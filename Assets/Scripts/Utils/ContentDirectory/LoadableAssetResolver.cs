using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class LoadableAssetResolver
{
    static bool s_warningLogged;

    public static bool TryResolve<T>(Loadable<T> loadable, out T asset) where T : Object
    {
        asset = null;
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
        var contentLoadManagerType = Type.GetType("UnityEngine.Loading.ContentLoadManager, UnityEngine.CoreModule");
        if (contentLoadManagerType == null)
            return false;

        var methodNames = new[] { "Load", "Resolve", "Get" };
        for (var i = 0; i < methodNames.Length; i++)
        {
            var method = contentLoadManagerType.GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Static, null, new[] { loadable.GetType() }, null);
            if (method == null)
                continue;

            var value = method.Invoke(null, new[] { loadable }) as Object;
            if (value != null)
            {
                asset = value;
                return true;
            }
        }

        return false;
    }
}