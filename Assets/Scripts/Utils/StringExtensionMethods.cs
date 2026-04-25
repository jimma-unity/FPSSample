using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Convenience functions for strings
public static class StringExtensionMethods
{
    public static string AfterLast(this string str, string sub)
    {
        var idx = str.LastIndexOf(sub);
        return idx < 0 ? "" : str.Substring(idx + sub.Length);
    }

    public static string BeforeLast(this string str, string sub)
    {
        var idx = str.LastIndexOf(sub);
        return idx < 0 ? "" : str.Substring(0, idx);
    }

    public static string AfterFirst(this string str, string sub)
    {
        var idx = str.IndexOf(sub);
        return idx < 0 ? "" : str.Substring(idx + sub.Length);
    }

    public static string BeforeFirst(this string str, string sub)
    {
        var idx = str.IndexOf(sub);
        return idx < 0 ? "" : str.Substring(0, idx);
    }

    public static int PrefixMatch(this string str, string prefix)
    {
        int l = 0, slen = str.Length, plen = prefix.Length;
        while(l<slen && l<plen)
        {
            if (str[l] != prefix[l])
                break;
            l++;
        }
        return l;
    }

    // FNV-1a 32-bit — deterministic across .NET versions, unlike string.GetHashCode().
    // Use this instead of GetHashCode() whenever the hash is serialized or compared
    // across Editor/Player sessions (e.g. skeleton bone name lookup).
    public static int GetStableHashCode(this string str)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in str)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return (int)hash;
        }
    }
}
