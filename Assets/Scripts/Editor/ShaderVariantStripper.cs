#if false
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
#if !UNITY_6000_5_OR_NEWER
using UnityEngine.Experimental.Rendering; // GraphicsStateCollection pre-6000.0.5
#endif

/// <summary>
/// Shader variant stripper driven by ShaderVariantCollection (SVC) and
/// GraphicsStateCollection (GSC).
///
/// The core insight is that HDRP pipeline globals (USE_FPTL_LIGHTLIST,
/// WRITE_NORMAL_BUFFER, SHADOWS_SHADOWMASK, etc.) are declared with
/// #pragma multi_compile (no _local suffix) and therefore show up as
/// GLOBAL ShaderKeywords. Material feature keywords (_NORMALMAP, _MASKMAP,
/// etc.) are declared with #pragma shader_feature_local and are LOCAL.
/// ShaderKeyword.IsKeywordLocal() distinguishes them. By filtering the
/// runtime variant's keywords to local-only before matching, both sides of
/// the comparison are on equal footing with the material-recorded SVC without
/// needing a hardcoded exclusion list.
///
/// GSC records LocalKeyword[] by design, so it is naturally local-only.
///
/// A variant is kept when its local keywords:
///   1. Exactly match a collection entry, OR
///   2. Are a subset of a collection entry (handles per-stage compilation
///      where multi_compile_fragment keywords present in the SVC entry are
///      absent from a vertex-stage runtime variant).
///
/// A shader/pass unknown to both collections is left untouched.
/// </summary>
class ShaderVariantStripper : IPreprocessShaders
{
    const string SvcPath =
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
        "UI/",
    };

    static readonly PassType[] RecordedPassTypes =
    {
        PassType.Normal,
        PassType.ShadowCaster,
        PassType.MotionVectors,
        PassType.ScriptableRenderPipeline,
        PassType.ScriptableRenderPipelineDefaultUnlit,
    };

    static readonly string[] InstancedKeywords = { "INSTANCING_ON", "DOTS_INSTANCING_ON" };

    // -------------------------------------------------------------------------
    // Per-pass variant collection

    class VariantSet
    {
        // Joined "kw1 kw2 kw3" strings for O(1) exact lookup.
        public readonly HashSet<string> ExactKeys = new(StringComparer.Ordinal);
        // Full keyword sets retained for subset matching.
        public readonly List<HashSet<string>> Entries = new();

        public void Add(string[] sortedLocalKws)
        {
            if (ExactKeys.Add(string.Join(" ", sortedLocalKws)))
                Entries.Add(new HashSet<string>(sortedLocalKws, StringComparer.Ordinal));
        }
    }

    // SVC: (shader, passType) → material-recorded variants.
    Dictionary<(Shader, int), VariantSet> _svc;

    // GSC: (shader, subshaderIndex, passIndex) → runtime-recorded variants.
    Dictionary<(Shader, uint, uint), VariantSet> _gsc;

    int _stripped, _kept;

    // Run after HDRP's built-in stripper so it removes disabled-feature variants
    // first, leaving a smaller set for our collection-based pass.
    public int callbackOrder => int.MaxValue;

    // -------------------------------------------------------------------------

    static bool ShouldSkipShader(Shader shader)
    {
        foreach (var prefix in NeverStripPrefixes)
            if (shader.name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        return false;
    }

    // Extracts the local keyword names from a compiled variant's keyword set,
    // sorted for canonical comparison. Global keywords (pipeline-injected:
    // USE_FPTL_LIGHTLIST, SHADOWS_SHADOWMASK, etc.) are excluded because they
    // are absent from material-recorded SVC entries by construction.
    static string[] LocalKeywords(ShaderKeyword[] allKws)
    {
        var result = new List<string>(allKws.Length);
        foreach (var kw in allKws)
            if (!string.IsNullOrEmpty(kw.name) && ShaderKeyword.IsKeywordLocal(kw))
                result.Add(kw.name);
        result.Sort(StringComparer.Ordinal);
        return result.ToArray();
    }

    // Normalises a keyword string list (from SVC or material) for storage:
    // removes empty entries and sorts. No global/local filtering here because
    // material.shaderKeywords only ever contains local keywords.
    static string[] SortedKeywords(IEnumerable<string> names) =>
        names.Where(n => !string.IsNullOrEmpty(n))
             .OrderBy(n => n, StringComparer.Ordinal)
             .ToArray();

    // -------------------------------------------------------------------------
    // Collection loading

    void BuildCollections()
    {
        _svc = new Dictionary<(Shader, int), VariantSet>();
        _gsc = new Dictionary<(Shader, uint, uint), VariantSet>();
        LoadSvc();
        LoadGsc();
    }

    void LoadSvc()
    {
        var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(SvcPath);
        if (collection == null)
        {
            Debug.LogError($"[ShaderVariantStripper] SVC not found at {SvcPath}");
            return;
        }

        var so          = new SerializedObject(collection);
        var shadersProp = so.FindProperty("m_Shaders");
        if (shadersProp == null || !shadersProp.isArray) return;

        for (int i = 0; i < shadersProp.arraySize; i++)
        {
            var elem   = shadersProp.GetArrayElementAtIndex(i);
            var shader = elem.FindPropertyRelative("first").objectReferenceValue as Shader;
            if (shader == null || ShouldSkipShader(shader)) continue;

            var variantsProp = elem.FindPropertyRelative("second.variants");
            if (variantsProp == null || !variantsProp.isArray) continue;

            for (int j = 0; j < variantsProp.arraySize; j++)
            {
                var vp       = variantsProp.GetArrayElementAtIndex(j);
                var kwStr    = vp.FindPropertyRelative("keywords").stringValue ?? "";
                var passType = vp.FindPropertyRelative("passType").intValue;

                var kws = SortedKeywords(kwStr.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                var key = (shader, passType);
                if (!_svc.TryGetValue(key, out var set))
                    _svc[key] = set = new VariantSet();
                set.Add(kws);
            }
        }

        int totalVariants = _svc.Values.Sum(v => v.Entries.Count);
        int totalShaders  = _svc.Keys.Select(k => k.Item1).Distinct().Count();
        Debug.Log($"[ShaderVariantStripper] SVC: {totalVariants} variants across {totalShaders} shaders.");
    }

    void LoadGsc()
    {
        var assetGuids = AssetDatabase.FindAssets("t:GraphicsStateCollection");
        var buf        = new List<GraphicsStateCollection.ShaderVariant>();
        int total      = 0;

        foreach (var assetGuid in assetGuids)
        {
            var gsc = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(
                          AssetDatabase.GUIDToAssetPath(assetGuid));
            if (gsc == null) continue;

            buf.Clear();
            gsc.GetVariants(buf);

            foreach (var v in buf)
            {
                if (v.shader == null || ShouldSkipShader(v.shader)) continue;

                // GSC records LocalKeyword[] — already local-only by definition.
                var kws = SortedKeywords(v.keywords.Select(k => k.name));
                var key = (v.shader, v.passId.SubshaderIndex, v.passId.PassIndex);
                if (!_gsc.TryGetValue(key, out var set))
                    _gsc[key] = set = new VariantSet();
                set.Add(kws);
                total++;
            }
        }

        Debug.Log($"[ShaderVariantStripper] GSC: {total} variants from {assetGuids.Length} collection(s).");
    }

    // -------------------------------------------------------------------------
    // IPreprocessShaders

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (ShouldSkipShader(shader)) return;
        if (_svc == null) BuildCollections();

        _svc.TryGetValue((shader, (int)snippet.passType), out var svcSet);
        _gsc.TryGetValue((shader, snippet.pass.SubshaderIndex, snippet.pass.PassIndex), out var gscSet);

        // Unknown to both collections — leave entirely untouched so untracked
        // shaders are never silently stripped.
        if (svcSet == null && gscSet == null) return;

        for (int i = data.Count - 1; i >= 0; i--)
        {
            var allKws = data[i].shaderKeywordSet.GetShaderKeywords();

            // Always keep the zero-keyword base variant.
            if (allKws.Length == 0) continue;

            // Reduce to local keywords. If nothing remains, every keyword in this
            // variant is a pipeline global — HDRP's own stripper handles those.
            var kws = LocalKeywords(allKws);
            if (kws.Length == 0) continue;

            if (IsKept(svcSet, kws) || IsKept(gscSet, kws))
            {
                _kept++;
                continue;
            }

            data.RemoveAt(i);
            _stripped++;
        }
    }

    static bool IsKept(VariantSet set, string[] kws)
    {
        if (set == null) return false;

        // Exact match: O(1) hash lookup.
        if (set.ExactKeys.Contains(string.Join(" ", kws)))
            return true;

        // Subset match: variant's local keywords ⊆ a collection entry.
        // This handles per-stage compilation: a vertex-stage variant will not
        // carry multi_compile_fragment keywords that appear in the SVC entry
        // (which was recorded with all stages active on the material). The
        // smaller runtime set is still a subset of the full entry.
        var kwSet = new HashSet<string>(kws, StringComparer.Ordinal);
        foreach (var entry in set.Entries)
            if (kwSet.IsSubsetOf(entry))
                return true;

        return false;
    }

    // -------------------------------------------------------------------------
    // SVC rebuild from project materials

    [MenuItem("FPS Sample/Shaders/Rebuild Variant Collection From Materials")]
    static void RebuildCollection()
    {
        var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(SvcPath);
        if (collection == null)
        {
            Debug.LogError($"[ShaderVariantStripper] Collection not found at {SvcPath}");
            return;
        }

        int added = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat?.shader == null || ShouldSkipShader(mat.shader)) continue;

            foreach (var passType in RecordedPassTypes)
            {
                if (TryAddVariant(collection, mat.shader, passType, mat.shaderKeywords))
                    added++;
                foreach (var kw in InstancedKeywords)
                    if (TryAddVariant(collection, mat.shader, passType, mat.shaderKeywords.Append(kw).ToArray()))
                        added++;
            }
        }

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ShaderVariantStripper] Scanned all materials, added {added} new variants. " +
                  $"Collection now has {collection.shaderCount} shaders / {collection.variantCount} variants.");
    }

    static bool TryAddVariant(ShaderVariantCollection collection, Shader shader,
                               PassType passType, string[] keywords)
    {
        try   { return collection.Add(new ShaderVariantCollection.ShaderVariant(shader, passType, keywords)); }
        catch (ArgumentException) { return false; }
    }
}
#endif
