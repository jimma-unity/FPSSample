using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

public sealed class RuntimeContentDirectoryRegistration
{
    public const string ClientRegistryName = "BundledResources/Client";
    public const string ServerRegistryName = "BundledResources/Server";

    enum RuntimeRole
    {
        Unknown,
        Client,
        Server
    }

    readonly struct RegistrationHandle
    {
        public readonly string path;
        public readonly object handle;

        public RegistrationHandle(string path, object handle)
        {
            this.path = path;
            this.handle = handle;
        }
    }

    readonly List<RegistrationHandle> m_handles = new List<RegistrationHandle>();
    string m_owner;

    static readonly Type s_contentLoadManagerType = FindContentLoadManagerType();
    static readonly MethodInfo s_registerMethod = FindRegisterMethod();
    static readonly MethodInfo s_unregisterMethod = FindUnregisterMethod();
    static bool s_missingApiLogged;
    static bool s_quitHookRegistered;
    static int s_shutdownUnregisterInvoked;
    static readonly List<RuntimeContentDirectoryRegistration> s_activeRegistrations = new List<RuntimeContentDirectoryRegistration>();

    public int RegisteredCount => m_handles.Count;

    public IEnumerable<string> GetRegisteredPaths()
    {
        for (var i = 0; i < m_handles.Count; i++)
            yield return m_handles[i].path;
    }

    public void RegisterDefaultContentDirectories(string owner, string registryName = null)
    {
        m_owner = string.IsNullOrWhiteSpace(owner) ? "RuntimeContentDirectoryRegistration" : owner;
        EnsureQuitHookRegistered();
        TrackActiveRegistration(this);

        if (!HasApi())
        {
            LogMissingApi(owner);
            return;
        }

        foreach (var path in EnumerateDefaultContentDirectories(registryName))
            RegisterDirectory(path, owner);
    }

    public void UnregisterAll(string owner)
    {
        UntrackActiveRegistration(this);

        if (m_handles.Count == 0)
            return;

        if (!HasApi())
        {
            m_handles.Clear();
            return;
        }

        for (int i = m_handles.Count - 1; i >= 0; i--)
        {
            var registration = m_handles[i];
            try
            {
                s_unregisterMethod.Invoke(null, new[] { registration.handle });
                GameDebug.Log(owner + ": Unregistered content directory: " + registration.path);
            }
            catch (Exception ex)
            {
                GameDebug.LogWarning(owner + ": Failed to unregister content directory '" + registration.path + "': " + ex.Message);
            }
        }

        m_handles.Clear();
    }

    static void EnsureQuitHookRegistered()
    {
        if (s_quitHookRegistered)
            return;

        Application.quitting += OnApplicationQuitting;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        s_quitHookRegistered = true;
    }

    static void OnApplicationQuitting()
    {
        UnregisterAllActiveRegistrations("Application.quitting", true);
    }

    static void OnProcessExit(object sender, EventArgs args)
    {
        UnregisterAllActiveRegistrations("ProcessExit", true);
    }

    static void OnDomainUnload(object sender, EventArgs args)
    {
        UnregisterAllActiveRegistrations("DomainUnload", true);
    }

    static void UnregisterAllActiveRegistrations(string reason, bool appendReasonToOwner)
    {
        if (Interlocked.Exchange(ref s_shutdownUnregisterInvoked, 1) != 0)
            return;

        if (s_activeRegistrations.Count == 0)
            return;

        var registrations = s_activeRegistrations.ToArray();
        for (var i = 0; i < registrations.Length; i++)
        {
            var registration = registrations[i];
            if (registration == null)
                continue;

            var owner = string.IsNullOrWhiteSpace(registration.m_owner)
                ? "RuntimeContentDirectoryRegistration"
                : registration.m_owner;

            var ownerWithReason = appendReasonToOwner ? owner + " [" + reason + "]" : owner;
            registration.UnregisterAll(ownerWithReason);
        }
    }

    public static void ForceShutdownUnregister(string reason)
    {
        UnregisterAllActiveRegistrations(string.IsNullOrWhiteSpace(reason) ? "ForceShutdownUnregister" : reason, false);
    }

    static void TrackActiveRegistration(RuntimeContentDirectoryRegistration registration)
    {
        if (registration == null)
            return;

        if (!s_activeRegistrations.Contains(registration))
            s_activeRegistrations.Add(registration);
    }

    static void UntrackActiveRegistration(RuntimeContentDirectoryRegistration registration)
    {
        if (registration == null)
            return;

        s_activeRegistrations.Remove(registration);
    }

    void RegisterDirectory(string directoryPath, string owner)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return;

        if (!Directory.Exists(directoryPath))
            return;

        if (!File.Exists(Path.Combine(directoryPath, "BuildManifestHash.txt")))
            return;

        try
        {
            var normalized = Path.GetFullPath(directoryPath).Replace('\\', '/');
            var handle = s_registerMethod.Invoke(null, new object[] { normalized });
            m_handles.Add(new RegistrationHandle(normalized, handle));
            GameDebug.Log(owner + ": Registered content directory: " + normalized);
        }
        catch (Exception ex)
        {
            GameDebug.LogWarning(owner + ": Failed to register content directory '" + directoryPath + "': " + ex.Message);
        }
    }

    static bool HasApi()
    {
        return s_contentLoadManagerType != null && s_registerMethod != null && s_unregisterMethod != null;
    }

    static void LogMissingApi(string owner)
    {
        if (s_missingApiLogged)
            return;

        s_missingApiLogged = true;
        GameDebug.LogWarning(owner + ": ContentLoadManager register/unregister API not available; skipping runtime content directory registration.");
    }

    static IEnumerable<string> EnumerateDefaultContentDirectories(string registryName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var yieldReturnBuffer = new List<string>();

        void Add(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            if (seen.Add(fullPath))
                yieldReturnBuffer.Add(fullPath);
        }

        Add(Application.dataPath);

        var runtimeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        AddRoleVersionedContentDirectories(runtimeRoot, DetermineRole(registryName), yieldReturnBuffer, seen);

        if (Application.isEditor)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Add(Path.Combine(projectRoot, "Autobuild", "Autobuild_Data"));
        }

        var runtimeBundlePath = SimpleBundleManager.GetRuntimeBundlePath();
        if (!string.IsNullOrWhiteSpace(runtimeBundlePath))
        {
            var normalizedRuntimePath = runtimeBundlePath.Replace('\\', '/');
            var suffix = "/" + SimpleBundleManager.assetBundleFolder;
            if (normalizedRuntimePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(runtimeBundlePath);
                if (!string.IsNullOrEmpty(parent))
                    Add(parent);
            }
            else
            {
                Add(runtimeBundlePath);
            }
        }

        foreach (var path in yieldReturnBuffer)
            yield return path;
    }

    static void AddRoleVersionedContentDirectories(string runtimeRoot, RuntimeRole role, List<string> buffer, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(runtimeRoot))
            return;

        var contentRoot = Path.Combine(runtimeRoot, "Content");
        if (!Directory.Exists(contentRoot))
            return;

        var buildId = GetRuntimeBuildId();

        bool HasContentDirectoryManifest(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            var manifestPath = Path.Combine(path, "BuildManifestHash.txt");
            return File.Exists(manifestPath);
        }

        void AddIfUnique(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!HasContentDirectoryManifest(path))
                return;

            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            if (seen.Add(fullPath))
                buffer.Add(fullPath);
        }

        AddIfUnique(Path.Combine(contentRoot, "Base"));
        if (!string.IsNullOrEmpty(buildId))
            AddIfUnique(Path.Combine(contentRoot, "Base", buildId));

        if (role == RuntimeRole.Client)
        {
            AddIfUnique(Path.Combine(contentRoot, "Client"));
            if (!string.IsNullOrEmpty(buildId))
                AddIfUnique(Path.Combine(contentRoot, "Client", buildId));
        }
        else if (role == RuntimeRole.Server)
        {
            AddIfUnique(Path.Combine(contentRoot, "Server"));
            if (!string.IsNullOrEmpty(buildId))
                AddIfUnique(Path.Combine(contentRoot, "Server", buildId));
        }
    }

    static RuntimeRole DetermineRole(string registryName)
    {
        if (string.IsNullOrWhiteSpace(registryName))
            return RuntimeRole.Unknown;

        if (registryName.IndexOf("/Client", StringComparison.OrdinalIgnoreCase) >= 0)
            return RuntimeRole.Client;

        if (registryName.IndexOf("/Server", StringComparison.OrdinalIgnoreCase) >= 0)
            return RuntimeRole.Server;

        return RuntimeRole.Unknown;
    }

    static string GetRuntimeBuildId()
    {
        var buildId = Game.game != null ? Game.game.buildId : null;
        if (string.IsNullOrWhiteSpace(buildId))
            return null;

        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalid in invalidChars)
        {
            buildId = buildId.Replace(invalid, '_');
        }

        return buildId;
    }

    static Type FindContentLoadManagerType()
    {
        var type = Type.GetType("UnityEngine.Loading.ContentLoadManager, UnityEngine.CoreModule");
        if (type != null)
            return type;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType("UnityEngine.Loading.ContentLoadManager");
            if (type != null)
                return type;
        }

        return null;
    }

    static MethodInfo FindRegisterMethod()
    {
        if (s_contentLoadManagerType == null)
            return null;

        var methods = s_contentLoadManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != "RegisterContentDirectory")
                continue;

            var parameters = methods[i].GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                return methods[i];
        }

        return null;
    }

    static MethodInfo FindUnregisterMethod()
    {
        if (s_contentLoadManagerType == null || s_registerMethod == null)
            return null;

        var handleType = s_registerMethod.ReturnType;
        var methods = s_contentLoadManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != "UnregisterContentDirectory")
                continue;

            var parameters = methods[i].GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == handleType)
                return methods[i];
        }

        return null;
    }
}