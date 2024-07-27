// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// The list of method names we support as function calls, and their OData equivalent.
/// </summary>
internal static class MethodNames
{
    // Instance OData filter method names
    private const string toLowerFilterMethod = "tolower";
    private const string toUpperFilterMethod = "toupper";
    private const string trimFilterMethod = "trim";
    private const string startsWithFilterMethod = "startswith";
    private const string endsWithFilterMethod = "endswith";
    private const string indexOfFilterMethod = "indexof";
    private const string containsFilterMethod = "contains";
    private const string substringFilterMethod = "substring";

    // Static OData filter method names
    private const string floorFilterMethod = "floor";
    private const string ceilingFilterMethod = "ceiling";
    private const string roundFilterMethod = "round";
    private const string concatFilterMethod = "concat";

    private static readonly Lazy<Dictionary<MemberInfoKey, string>> _instanceMethods = new(() => new()
        {
            { new MemberInfoKey(typeof(string), "ToLower", true, true), toLowerFilterMethod },
            { new MemberInfoKey(typeof(string), "ToLowerInvariant", true, true), toLowerFilterMethod },
            { new MemberInfoKey(typeof(string), "ToUpper", true, true), toUpperFilterMethod },
            { new MemberInfoKey(typeof(string), "ToUpperInvariant", true, true), toUpperFilterMethod },
            { new MemberInfoKey(typeof(string), "Trim", true, true), trimFilterMethod },
            { new MemberInfoKey(typeof(string), "StartsWith", true, true, typeof(string)), startsWithFilterMethod },
            { new MemberInfoKey(typeof(string), "EndsWith", true, true, typeof(string)), endsWithFilterMethod },
            { new MemberInfoKey(typeof(string), "IndexOf", true, true, typeof(string)), indexOfFilterMethod },
            { new MemberInfoKey(typeof(string), "IndexOf", true, true, typeof(char)), indexOfFilterMethod },
            { new MemberInfoKey(typeof(string), "Contains", true, true, typeof(string)), containsFilterMethod },
            { new MemberInfoKey(typeof(string), "Substring", true, true, typeof(int)), substringFilterMethod },
            { new MemberInfoKey(typeof(string), "Substring", true, true, typeof(int), typeof(int)), substringFilterMethod },
        });

    private static readonly Lazy<Dictionary<MemberInfoKey, string>> _staticMethods = new(() => new()
        {
            { new MemberInfoKey(typeof(Math), "Floor", true, false, typeof(double)), floorFilterMethod },
            { new MemberInfoKey(typeof(Math), "Ceiling", true, false, typeof(double)), ceilingFilterMethod },
            { new MemberInfoKey(typeof(Math), "Round", true, false, typeof(double)), roundFilterMethod },
            { new MemberInfoKey(typeof(string), "Concat", true, false, typeof(string), typeof(string)), concatFilterMethod },
            { new MemberInfoKey(typeof(decimal), "Floor", true, false, typeof(decimal)), floorFilterMethod },
            { new MemberInfoKey(typeof(decimal), "Ceiling", true, false, typeof(decimal)), ceilingFilterMethod },
            { new MemberInfoKey(typeof(decimal), "Round", true, false, typeof(decimal)), roundFilterMethod },
            { new MemberInfoKey(typeof(Math), "Ceiling", true, false, typeof(decimal)), ceilingFilterMethod },
            { new MemberInfoKey(typeof(Math), "Floor", true, false, typeof(decimal)), floorFilterMethod },
            { new MemberInfoKey(typeof(Math), "Round", true, false, typeof(decimal)), roundFilterMethod }
        });

    internal static bool TryGetValue(MemberInfoKey key, [NotNullWhen(true)] out string? methodName, out bool isStatic)
    {
        if (_instanceMethods.Value.TryGetValue(key, out methodName))
        {
            isStatic = false;
            return true;
        }

        if (_staticMethods.Value.TryGetValue(key, out methodName))
        {
            isStatic = true;
            return true;
        }

        methodName = "";
        isStatic = false;
        return false;
    }
}
