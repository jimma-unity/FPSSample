using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

public static class FastShaderBuildMode
{
    const string Define = "FPS_FAST_SHADER_BUILD";
    const string MenuPath = "FPS Sample/BuildSystem/Fast Shader Build Mode";
    const string Win64MenuPath = "FPS Sample/BuildSystem/Win64/Fast Shader Build Mode";
    const string StrictDefine = "FPS_STRICT_SHADER_WHITELIST";
    const string StrictMenuPath = "FPS Sample/BuildSystem/Strict Shader Whitelist Mode";
    const string StrictWin64MenuPath = "FPS Sample/BuildSystem/Win64/Strict Shader Whitelist Mode";

    static BuildTargetGroup ActiveBuildTargetGroup
    {
        get
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (buildTargetGroup == BuildTargetGroup.Unknown)
                buildTargetGroup = BuildTargetGroup.Standalone;
            return buildTargetGroup;
        }
    }

    static List<string> GetDefines(BuildTargetGroup buildTargetGroup)
    {
        return ParseDefines(GetDefineSymbols(buildTargetGroup))
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => d.Length > 0)
            .Distinct()
            .ToList();
    }

    static void SetDefines(BuildTargetGroup buildTargetGroup, List<string> defines)
    {
        SetDefineSymbols(buildTargetGroup, string.Join(";", defines.Distinct()));
    }

    static string GetDefineSymbols(BuildTargetGroup buildTargetGroup)
    {
        var playerSettingsType = typeof(PlayerSettings);

        if (TryInvokeGetDefineSymbols(playerSettingsType, "GetScriptingDefineSymbolsForGroup", typeof(BuildTargetGroup), buildTargetGroup, out var legacySymbols))
            return legacySymbols;

        if (TryGetNamedBuildTargetArg(buildTargetGroup, out var namedBuildTargetType, out var namedBuildTarget))
        {
            if (TryInvokeGetDefineSymbols(playerSettingsType, "GetScriptingDefineSymbols", namedBuildTargetType, namedBuildTarget, out var modernSymbols))
                return modernSymbols;
        }

        Debug.LogWarning("Could not find a supported PlayerSettings define-symbol API in this Unity version.");
        return string.Empty;
    }

    static void SetDefineSymbols(BuildTargetGroup buildTargetGroup, string defines)
    {
        var playerSettingsType = typeof(PlayerSettings);

        var definesArray = ParseDefines(defines)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => d.Length > 0)
            .Distinct()
            .ToArray();

        if (TryInvokeSetDefineSymbols(playerSettingsType, "SetScriptingDefineSymbolsForGroup", typeof(BuildTargetGroup), buildTargetGroup, defines, definesArray))
            return;

        if (TryGetNamedBuildTargetArg(buildTargetGroup, out var namedBuildTargetType, out var namedBuildTarget))
        {
            if (TryInvokeSetDefineSymbols(playerSettingsType, "SetScriptingDefineSymbols", namedBuildTargetType, namedBuildTarget, defines, definesArray))
                return;
        }

        Debug.LogWarning("Could not set scripting define symbols: no supported PlayerSettings API was found.");
    }

    static string ParseDefines(string defines)
    {
        if (string.IsNullOrEmpty(defines))
            return string.Empty;

        return string.Join(";", defines
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => d.Length > 0)
            .Distinct());
    }

    static bool TryGetNamedBuildTargetArg(BuildTargetGroup buildTargetGroup, out Type namedBuildTargetType, out object namedBuildTarget)
    {
        namedBuildTargetType = Type.GetType("UnityEditor.Build.NamedBuildTarget, UnityEditor");
        namedBuildTarget = null;
        if (namedBuildTargetType == null)
            return false;

        var fromGroup = namedBuildTargetType.GetMethod(
            "FromBuildTargetGroup",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(BuildTargetGroup) },
            null);

        if (fromGroup == null)
            return false;

        namedBuildTarget = fromGroup.Invoke(null, new object[] { buildTargetGroup });
        return namedBuildTarget != null;
    }

    static bool TryInvokeGetDefineSymbols(Type playerSettingsType, string methodName, Type targetType, object targetArg, out string defines)
    {
        defines = null;
        var methods = playerSettingsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == methodName)
            .ToArray();

        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            var parameters = method.GetParameters();
            if (parameters.Length < 1 || parameters[0].ParameterType != targetType)
                continue;

            object[] args;
            if (parameters.Length == 1)
                args = new[] { targetArg };
            else if (parameters.Length == 2 && parameters[1].IsOut)
                args = new object[] { targetArg, null };
            else
                continue;

            try
            {
                var result = method.Invoke(null, args);
                if (TryConvertDefines(result, out defines))
                    return true;

                if (args.Length == 2 && TryConvertDefines(args[1], out defines))
                    return true;
            }
            catch
            {
            }
        }

        return false;
    }

    static bool TryInvokeSetDefineSymbols(Type playerSettingsType, string methodName, Type targetType, object targetArg, string defines, string[] definesArray)
    {
        var methods = playerSettingsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == methodName)
            .ToArray();

        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            var parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != targetType)
                continue;

            try
            {
                if (parameters[1].ParameterType == typeof(string))
                {
                    method.Invoke(null, new object[] { targetArg, defines });
                    return true;
                }

                if (parameters[1].ParameterType == typeof(string[]))
                {
                    method.Invoke(null, new object[] { targetArg, definesArray });
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    static bool TryConvertDefines(object value, out string defines)
    {
        if (value is string s)
        {
            defines = ParseDefines(s);
            return true;
        }

        if (value is string[] arr)
        {
            defines = ParseDefines(string.Join(";", arr));
            return true;
        }

        defines = null;
        return false;
    }

    [MenuItem(MenuPath)]
    public static void Toggle()
    {
        var buildTargetGroup = ActiveBuildTargetGroup;
        var defines = GetDefines(buildTargetGroup);

        if (defines.Contains(Define))
            defines.RemoveAll(d => d == Define);
        else
        {
            defines.Add(Define);
            defines.RemoveAll(d => d == StrictDefine);
        }

        SetDefines(buildTargetGroup, defines);

        defines = GetDefines(buildTargetGroup);

        Debug.Log("Fast Shader Build Mode: " + (defines.Contains(Define) ? "ON" : "OFF") + " for " + buildTargetGroup);
    }

    [MenuItem(Win64MenuPath)]
    static void ToggleWin64()
    {
        Toggle();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        var defines = GetDefines(ActiveBuildTargetGroup);

        Menu.SetChecked(MenuPath, defines.Contains(Define));
        return true;
    }

    [MenuItem(Win64MenuPath, true)]
    static bool ToggleWin64Validate()
    {
        var defines = GetDefines(ActiveBuildTargetGroup);
        Menu.SetChecked(Win64MenuPath, defines.Contains(Define));
        return true;
    }

    [MenuItem(StrictMenuPath)]
    static void ToggleStrict()
    {
        var buildTargetGroup = ActiveBuildTargetGroup;
        var defines = GetDefines(buildTargetGroup);

        if (defines.Contains(StrictDefine))
            defines.RemoveAll(d => d == StrictDefine);
        else
        {
            defines.Add(StrictDefine);
            defines.RemoveAll(d => d == Define);
        }

        SetDefines(buildTargetGroup, defines);

        defines = GetDefines(buildTargetGroup);
        Debug.Log("Strict Shader Whitelist Mode: " + (defines.Contains(StrictDefine) ? "ON" : "OFF") + " for " + buildTargetGroup);
    }

    [MenuItem(StrictWin64MenuPath)]
    static void ToggleStrictWin64()
    {
        ToggleStrict();
    }

    [MenuItem(StrictMenuPath, true)]
    static bool ToggleStrictValidate()
    {
        var defines = GetDefines(ActiveBuildTargetGroup);
        Menu.SetChecked(StrictMenuPath, defines.Contains(StrictDefine));
        return true;
    }

    [MenuItem(StrictWin64MenuPath, true)]
    static bool ToggleStrictWin64Validate()
    {
        var defines = GetDefines(ActiveBuildTargetGroup);
        Menu.SetChecked(StrictWin64MenuPath, defines.Contains(StrictDefine));
        return true;
    }
}

public class FastShaderVariantStripper : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
#if FPS_STRICT_SHADER_WHITELIST
        StrictShaderWhitelist.Filter(shader, snippet, data);
#elif FPS_FAST_SHADER_BUILD
        if (data == null || data.Count <= 1)
            return;

        for (var i = data.Count - 1; i >= 1; --i)
            data.RemoveAt(i);
#endif
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
#if FPS_FAST_SHADER_BUILD || FPS_STRICT_SHADER_WHITELIST
        if (data == null || data.Count <= 1)
            return;

        for (var i = data.Count - 1; i >= 1; --i)
            data.RemoveAt(i);
#endif
    }
}

static class StrictShaderWhitelist
{
    class TraceVariant
    {
        public int ShaderType;
        public int SubShaderIndex;
        public HashSet<string> Keywords;
    }

    static readonly Dictionary<string, List<TraceVariant>> TraceMap = new Dictionary<string, List<TraceVariant>>();
    static bool loaded;
    static bool missingLogPrinted;

    static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        var tracePath = "Assets/shadertrace.txt";
        if (!File.Exists(tracePath))
        {
            if (!missingLogPrinted)
            {
                missingLogPrinted = true;
                Debug.LogWarning("Strict Shader Whitelist Mode is ON but Assets/shadertrace.txt was not found. All unknown shader variants will be stripped.");
            }
            return;
        }

        var lines = File.ReadAllLines(tracePath);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("SAS:"))
                continue;

            var fields = line.Substring(4).Split(',');
            if (fields.Length < 6)
                continue;

            if (!int.TryParse(fields[1], out var subShaderIndex))
                continue;
            if (!int.TryParse(fields[3], out var shaderType))
                continue;

            var keywordField = fields[5].Trim();
            var keywords = keywordField.TrimEnd(';')
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .ToHashSet();

            var shaderName = fields[0].Trim();
            if (!TraceMap.TryGetValue(shaderName, out var variants))
            {
                variants = new List<TraceVariant>();
                TraceMap[shaderName] = variants;
            }

            variants.Add(new TraceVariant
            {
                ShaderType = shaderType,
                SubShaderIndex = subShaderIndex,
                Keywords = keywords,
            });
        }

        Debug.Log("Strict Shader Whitelist loaded traces for " + TraceMap.Count + " shaders.");
    }

    public static void Filter(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (data == null || data.Count == 0)
            return;

        EnsureLoaded();

        if (!TraceMap.TryGetValue(shader.name, out var allowedVariants))
            allowedVariants = null;

        var i = 0;
        while (i < data.Count)
        {
            if (IsAllowed(snippet, data[i], allowedVariants))
                i++;
            else
                data.RemoveAt(i);
        }
    }

    static bool IsAllowed(ShaderSnippetData snippet, ShaderCompilerData inputVariant, List<TraceVariant> allowedVariants)
    {
        if (allowedVariants == null || allowedVariants.Count == 0)
            return false;

        var inputKeywords = inputVariant.shaderKeywordSet.GetShaderKeywords();

        for (var i = 0; i < allowedVariants.Count; i++)
        {
            var validVariant = allowedVariants[i];
            if (validVariant.ShaderType != (int)snippet.shaderType)
                continue;
            if (validVariant.SubShaderIndex != (int)snippet.pass.SubshaderIndex)
                continue;
            if (inputKeywords.Length != validVariant.Keywords.Count)
                continue;

            var allMatch = true;
            for (var n = 0; n < inputKeywords.Length; n++)
            {
                if (!validVariant.Keywords.Contains(inputKeywords[n].name))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
                return true;
        }

        return false;
    }
}