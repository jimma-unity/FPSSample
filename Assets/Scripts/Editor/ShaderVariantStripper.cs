#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class ShaderVariantStripper : IPreprocessShaders
{
  const string CollectionPath =
      "Assets/ScriptableRenderPipeline/HDRP_Assets/TrackedShaders.shadervariants";

  static readonly string[] NeverStripPrefixes =
  {
      "Firstperson_Projection",
      "Hidden/",
      "HDRP/Water",
      "HDRP/VFX",
      "HDRP/Unlit",
      "Planet",
      "TextMeshPro",
      "UI/"
  };

  // HDRP pipeline-injected keywords — excluded from the match key so that material-only
  // collection entries match compilation variants that carry them. These vary by camera
  // distance / light count (FPTL vs Clustered) or render pass (WRITE_NORMAL_BUFFER), which
  // is why the same model can fail at one LOD but not another. HDRP's built-in stripper
  // already handles stripping unused HDRP feature variants; we only match material features.
  static readonly HashSet<string> HDRPGlobalKeywords = new(StringComparer.Ordinal)
  {
      "USE_FPTL_LIGHTLIST", "USE_CLUSTERED_LIGHTLIST",
      "WRITE_NORMAL_BUFFER",
      "PUNCTUAL_SHADOW_LOW", "PUNCTUAL_SHADOW_MEDIUM", "PUNCTUAL_SHADOW_HIGH",
      "DIRECTIONAL_SHADOW_LOW", "DIRECTIONAL_SHADOW_MEDIUM", "DIRECTIONAL_SHADOW_HIGH",
      "AREA_SHADOW_LOW", "AREA_SHADOW_MEDIUM", "AREA_SHADOW_HIGH",
      "DECALS_OFF", "DECALS_3RT", "DECALS_4RT",
      "PROBE_VOLUMES_L1", "PROBE_VOLUMES_L2",
      "SCREEN_SPACE_SHADOWS_OFF",
      "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON",
      "USE_LEGACY_LIGHTMAPS", "LIGHTPROBE_SH",
      "PROCEDURAL_INSTANCING_ON",
      "STEREO_INSTANCING_ON", "STEREO_MULTIVIEW_ON", "UNITY_SINGLE_PASS_STEREO",
  };

  static readonly PassType[] RecordedPassTypes =
  {
      PassType.Normal,
      PassType.ShadowCaster,
      PassType.MotionVectors,
      PassType.ScriptableRenderPipeline,
      PassType.ScriptableRenderPipelineDefaultUnlit,
  };

  static readonly string[] InstancedKeywords =
  {
      "INSTANCING_ON",
      "DOTS_INSTANCING_ON",
  };

  static readonly Regex GuidRx =
      new Regex(@"guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);

  // Exact lookup: "shaderGUID|passType|matKw1 matKw2 ..." (HDRP globals stripped, sorted).
  // Subset lookup: "shaderGUID|passType" -> list of keyword sets from the collection.
  // A variant is kept if its keyword set is a subset of any collection entry for the same
  // shader+passType — this handles sub-pass variants (e.g. depth prepass uses fewer keywords
  // than the full forward pass) and SSR-enabled variants whose superset was recorded with
  // _DISABLE_SSR_TRANSPARENT during the playthrough.
  HashSet<string> _variantKeys;
  Dictionary<string, List<HashSet<string>>> _variantSets;
  readonly Dictionary<Shader, string> _guidCache = new();

  int _stripped, _kept;

  public int callbackOrder => 0;

  static bool ShouldSkipShader(Shader shader)
  {
      foreach (var prefix in NeverStripPrefixes)
          if (shader.name.StartsWith(prefix))
              return true;
      return false;
  }

  // Returns the material-feature keywords: strips HDRP pipeline globals, empty names,
  // and sorts. These are the only keywords we match on.
  static string[] MaterialKeywords(IEnumerable<string> names) =>
      names.Where(n => !string.IsNullOrEmpty(n) && !HDRPGlobalKeywords.Contains(n))
           .OrderBy(n => n, StringComparer.Ordinal)
           .ToArray();

  string GetGuid(Shader shader)
  {
      if (!_guidCache.TryGetValue(shader, out var guid))
          _guidCache[shader] = guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader));
      return guid;
  }

  // Parses the .shadervariants YAML text directly — avoids SerializedObject's inability to
  // navigate ShaderVariantCollection's native C++ struct layout reliably.
  // The YAML has blocks like:
  //   - first:
  //       shader: {fileID: X, guid: GUID, type: 2}
  //     second:
  //       variants:
  //       - keywords: KW1 KW2 KW3
  //           KW4 KW5          <- wrapped continuation lines
  //         passType: 13
  static (HashSet<string> keys, Dictionary<string, List<HashSet<string>>> sets) BuildLookup(string assetPath)
  {
      var result = new HashSet<string>(StringComparer.Ordinal);
      var sets   = new Dictionary<string, List<HashSet<string>>>(StringComparer.Ordinal);

      var projectRoot = Path.GetDirectoryName(Application.dataPath);
      var fullPath    = Path.Combine(projectRoot, assetPath);

      if (!File.Exists(fullPath))
      {
          Debug.LogError($"[ShaderVariantStripper] Collection not found at {fullPath}");
          return (result, sets);
      }

      string currentGuid        = null;
      bool   collectingKeywords = false;
      var    pendingKws         = new StringBuilder();
      int    shaderCount        = 0;

      foreach (var rawLine in File.ReadLines(fullPath))
      {
          var trimmed = rawLine.TrimStart();

          // Shader GUID — from "- first: {fileID: ..., guid: ..., type: ...}" lines.
          // The type is 3 for package/project shaders, 0 for built-in Unity shaders.
          if (trimmed.StartsWith("- first:") && trimmed.Contains("guid:"))
          {
              var m = GuidRx.Match(trimmed);
              if (m.Success)
              {
                  currentGuid = m.Groups[1].Value;
                  shaderCount++;
                  collectingKeywords = false;
                  pendingKws.Clear();
              }
              continue;
          }

          // Start of a variant's keyword list.
          if (trimmed.StartsWith("- keywords:"))
          {
              collectingKeywords = true;
              pendingKws.Clear();
              var rest = trimmed.Substring("- keywords:".Length).Trim();
              if (rest.Length > 0)
                  pendingKws.Append(rest);
              continue;
          }

          // passType closes the variant entry.
          if (trimmed.StartsWith("passType:") && collectingKeywords && currentGuid != null)
          {
              collectingKeywords = false;
              if (int.TryParse(trimmed.Substring("passType:".Length).Trim(), out int pt))
              {
                  var kws = MaterialKeywords(pendingKws.ToString()
                      .Split(' ', StringSplitOptions.RemoveEmptyEntries));
                  result.Add($"{currentGuid}|{pt}|{string.Join(" ", kws)}");
                  var setKey = $"{currentGuid}|{pt}";
                  if (!sets.TryGetValue(setKey, out var list))
                      sets[setKey] = list = new List<HashSet<string>>();
                  list.Add(new HashSet<string>(kws, StringComparer.Ordinal));
              }
              pendingKws.Clear();
              continue;
          }

          // Wrapped keyword continuation lines: indented further than passType, no colon.
          // Keyword names never contain ": " so the colon check is safe.
          if (collectingKeywords)
          {
              if (trimmed.Length == 0 || trimmed.Contains(": ") || trimmed.StartsWith("- "))
              {
                  collectingKeywords = false;
                  pendingKws.Clear();
              }
              else
              {
                  if (pendingKws.Length > 0) pendingKws.Append(' ');
                  pendingKws.Append(trimmed);
              }
          }
      }

      Debug.Log($"[ShaderVariantStripper] Lookup: {result.Count} material-keyword variants across {shaderCount} shaders.");
      return (result, sets);
  }

  public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
  {
      if (ShouldSkipShader(shader))
          return;

      if (_variantKeys == null)
          (_variantKeys, _variantSets) = BuildLookup(CollectionPath);

      var shaderGuid = GetGuid(shader);
      if (string.IsNullOrEmpty(shaderGuid))
          return;

      for (int i = data.Count - 1; i >= 0; i--)
      {
          var allKws = data[i].shaderKeywordSet.GetShaderKeywords();

          // Always keep the zero-keyword base variant.
          if (allKws.Length == 0)
              continue;

          var kws = MaterialKeywords(Array.ConvertAll(allKws, kw => kw.name));

          // No material keywords remain after filtering (e.g. WRITE_NORMAL_BUFFER-only
          // depth prepass). Let HDRP's own stripper handle these.
          if (kws.Length == 0)
              continue;

          var key = $"{shaderGuid}|{(int)snippet.passType}|{string.Join(" ", kws)}";
          if (_variantKeys.Contains(key))
          {
              _kept++;
              continue;
          }

          // Subset match: keep if the variant's keywords are a subset of any collection
          // entry for this shader+passType. Handles sub-pass variants (depth prepass uses
          // fewer keywords than forward pass) and SSR-enabled variants whose superset was
          // recorded with _DISABLE_SSR_TRANSPARENT during the collection playthrough.
          var setKey = $"{shaderGuid}|{(int)snippet.passType}";
          if (_variantSets.TryGetValue(setKey, out var sets))
          {
              var kwSet = new HashSet<string>(kws, StringComparer.Ordinal);
              foreach (var entry in sets)
              {
                  if (kwSet.IsSubsetOf(entry))
                  {
                      _kept++;
                      goto nextVariant;
                  }
              }
          }

          data.RemoveAt(i);
          _stripped++;
          continue;
          nextVariant:;
      }
  }

  [MenuItem("FPS Sample/Shaders/Rebuild Variant Collection From Materials")]
  static void RebuildCollection()
  {
      var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(CollectionPath);
      if (collection == null)
      {
          Debug.LogError($"[ShaderVariantStripper] Collection not found at {CollectionPath}");
          return;
      }

      var guids = AssetDatabase.FindAssets("t:Material");
      int added = 0;

      foreach (var guid in guids)
      {
          var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
          if (mat?.shader == null) continue;
          if (ShouldSkipShader(mat.shader)) continue;

          foreach (var passType in RecordedPassTypes)
          {
              if (TryAdd(collection, mat.shader, passType, mat.shaderKeywords))
                  added++;

              foreach (var instanceKw in InstancedKeywords)
              {
                  var keywords = mat.shaderKeywords.Append(instanceKw).ToArray();
                  if (TryAdd(collection, mat.shader, passType, keywords))
                      added++;
              }
          }
      }

      EditorUtility.SetDirty(collection);
      AssetDatabase.SaveAssets();
      Debug.Log($"[ShaderVariantStripper] Scanned {guids.Length} materials, " +
                $"added {added} new variants. " +
                $"Collection now has {collection.shaderCount} shaders / {collection.variantCount} variants.");
  }

  static bool TryAdd(ShaderVariantCollection collection, Shader shader,
                     PassType passType, string[] keywords)
  {
      try
      {
          return collection.Add(
              new ShaderVariantCollection.ShaderVariant(shader, passType, keywords));
      }
      catch (ArgumentException)
      {
          return false;
      }
  }
}
#endif
