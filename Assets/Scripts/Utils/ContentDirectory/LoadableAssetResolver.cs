using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class LoadableAssetResolver
{
    static bool s_warningLogged;
    static bool s_rootAssetWarningLogged;
    static readonly Type s_contentLoadManagerType = FindContentLoadManagerType();
    static readonly MethodInfo s_getRootAssetsGenericNoArgsMethod = FindGetRootAssetsGenericNoArgsMethod();

    public static bool TryGetRootAssets<T>(out T[] assets) where T : Object
    {
        assets = null;

        if (s_contentLoadManagerType == null || s_getRootAssetsGenericNoArgsMethod == null)
            return false;

        MethodInfo closedMethod;
        try
        {
            closedMethod = s_getRootAssetsGenericNoArgsMethod.MakeGenericMethod(typeof(T));
        }
        catch
        {
            return false;
        }

        try
        {
            var result = closedMethod.Invoke(null, null);
            if (result is T[] typedAssets && typedAssets.Length > 0)
            {
                assets = typedAssets;
                return true;
            }

            if (result is Array array && array.Length > 0)
            {
                var converted = new List<T>(array.Length);
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array.GetValue(i) as T;
                    if (item != null)
                        converted.Add(item);
                }

                if (converted.Count > 0)
                {
                    assets = converted.ToArray();
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            if (!s_rootAssetWarningLogged)
            {
                s_rootAssetWarningLogged = true;
                GameDebug.LogWarning("LoadableAssetResolver: failed to query ContentLoadManager root assets: " + ex.Message);
            }
        }

        return false;
    }

    public static bool IsLikelyInvalidLoadableObject(object loadable)
    {
        if (loadable == null)
            return true;

        if (TryReadLoadableGuid(loadable, out var guid))
            return string.IsNullOrWhiteSpace(guid);

        var type = loadable.GetType();
        if (!type.IsValueType)
            return false;

        try
        {
            var defaultValue = Activator.CreateInstance(type);
            return loadable.Equals(defaultValue);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryResolve<T>(Loadable<T> loadable, out T asset) where T : Object
    {
        asset = null;
        if (EqualityComparer<Loadable<T>>.Default.Equals(loadable, default))
            return false;

        if (!TryResolveObject(loadable, out var resolved))
            return false;

        asset = resolved as T;
        return asset != null;
    }

    public static bool TryResolveObject(object loadable, out Object asset)
    {
        asset = null;
        if (loadable == null)
            return false;

        if (IsLikelyInvalidLoadableObject(loadable))
            return false;

        if (TryInvokeInstanceGetter(loadable, out asset))
            return asset != null;

        if (TryInvokeContentLoadManager(loadable, out asset))
            return asset != null;

        if (!s_warningLogged)
        {
            s_warningLogged = true;
            GameDebug.LogWarning("LoadableAssetResolver: Unable to resolve Loadable<T> via reflection.");
        }

        return false;
    }

    static bool TryReadLoadableGuid(object loadable, out string guid)
    {
        guid = null;
        if (loadable == null)
            return false;

        if (TryReadGuidMember(loadable, out guid))
            return true;

        if (TryGetMemberValue(loadable, "m_LoadableRef", out var loadableRef) && loadableRef != null)
            return TryReadGuidMember(loadableRef, out guid);

        return false;
    }

    static bool TryReadGuidMember(object target, out string guid)
    {
        guid = null;
        if (target == null)
            return false;

        if (!TryGetMemberValue(target, "guid", out var value) &&
            !TryGetMemberValue(target, "Guid", out value) &&
            !TryGetMemberValue(target, "m_Guid", out value) &&
            !TryGetMemberValue(target, "m_guid", out value))
            return false;

        guid = value as string;
        if (guid == null && value != null)
            guid = Convert.ToString(value, CultureInfo.InvariantCulture);

        return true;
    }

    static bool TryGetMemberValue(object target, string memberName, out object value)
    {
        value = null;
        if (target == null)
            return false;

        var type = target.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var field = type.GetField(memberName, flags);
        if (field != null)
        {
            value = field.GetValue(target);
            return true;
        }

        var property = type.GetProperty(memberName, flags);
        if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
            return false;

        value = property.GetValue(target, null);
        return true;
    }

    static bool TryInvokeContentLoadManagerMethod(MethodInfo method, Type loadableType, object loadable, out Object asset)
    {
        asset = null;
        if (method == null)
            return false;

        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            return false;

        var parameterType = parameters[0].ParameterType;
        var isByRef = parameterType.IsByRef;
        if (isByRef)
            parameterType = parameterType.GetElementType();

        if (parameterType == null || !parameterType.IsAssignableFrom(loadableType))
            return false;

        var args = new[] { loadable };
        var result = method.Invoke(null, args) as Object;
        if (result != null)
        {
            asset = result;
            return true;
        }

        if (isByRef)
        {
            var byRefResult = args[0] as Object;
            if (byRefResult != null)
            {
                asset = byRefResult;
                return true;
            }
        }

        return false;
    }

    static bool TryInvokeInstanceGetter(object loadable, out Object asset)
    {
        asset = null;
        var type = loadable.GetType();

        var propertyNames = new[] { "Asset", "Value", "Result", "Object" };
        for (var i = 0; i < propertyNames.Length; i++)
        {
            var property = type.GetProperty(propertyNames[i], BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.GetIndexParameters().Length > 0)
                continue;

            var value = property.GetValue(loadable, null) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        var methodNames = new[] { "Load", "Resolve", "Get" };
        for (var i = 0; i < methodNames.Length; i++)
        {
            var method = type.GetMethod(methodNames[i], BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (method == null)
                continue;

            var value = method.Invoke(loadable, null) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        return false;
    }

    static bool TryInvokeContentLoadManager(object loadable, out Object asset)
    {
        asset = null;
        if (s_contentLoadManagerType == null)
            return false;

        var loadableType = loadable.GetType();
        var methods = s_contentLoadManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        var methodNames = new[] { "Load", "Resolve", "Get" };
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            var isKnownMethodName = false;
            for (var j = 0; j < methodNames.Length; j++)
            {
                if (string.Equals(method.Name, methodNames[j], StringComparison.OrdinalIgnoreCase))
                {
                    isKnownMethodName = true;
                    break;
                }
            }

            if (!isKnownMethodName)
                continue;

            if (!method.IsGenericMethodDefinition)
            {
                if (TryInvokeContentLoadManagerMethod(method, loadableType, loadable, out asset))
                    return true;

                continue;
            }

            if (!loadableType.IsGenericType)
                continue;

            var genericArgs = loadableType.GetGenericArguments();
            if (genericArgs.Length != method.GetGenericArguments().Length)
                continue;

            MethodInfo closedMethod;
            try
            {
                closedMethod = method.MakeGenericMethod(genericArgs);
            }
            catch
            {
                continue;
            }

            if (TryInvokeContentLoadManagerMethod(closedMethod, loadableType, loadable, out asset))
                return true;
        }

        return false;
    }

    static Type FindContentLoadManagerType()
    {
        var type = Type.GetType("UnityEngine.Loading.ContentLoadManager, UnityEngine.ContentLoadModule");
        if (type != null)
            return type;

        type = Type.GetType("UnityEngine.Loading.ContentLoadManager, UnityEngine.CoreModule");
        if (type != null)
            return type;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType("UnityEngine.Loading.ContentLoadManager");
            if (type != null)
                return type;
        }

        return null;
    }

    static MethodInfo FindGetRootAssetsGenericNoArgsMethod()
    {
        if (s_contentLoadManagerType == null)
            return null;

        var methods = s_contentLoadManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (!string.Equals(method.Name, "GetRootAssets", StringComparison.Ordinal))
                continue;

            if (!method.IsGenericMethodDefinition)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return method;
        }

        return null;
    }
}