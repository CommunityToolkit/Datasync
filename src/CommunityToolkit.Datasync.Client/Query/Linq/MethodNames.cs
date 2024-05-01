// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// The list of method names we support as function calls, and their OData equivalent.
/// </summary>
internal static class MethodNames
{
    // Instance OData filter method names
    internal const string toLowerFilterMethod = "tolower";
    internal const string toUpperFilterMethod = "toupper";
    internal const string trimFilterMethod = "trim";
    internal const string startsWithFilterMethod = "startswith";
    internal const string endsWithFilterMethod = "endswith";
    internal const string indexOfFilterMethod = "indexof";
    internal const string containsFilterMethod = "contains";
    internal const string substringFilterMethod = "substring";

    // Static OData filter method names
    internal const string floorFilterMethod = "floor";
    internal const string ceilingFilterMethod = "ceiling";
    internal const string roundFilterMethod = "round";
    internal const string concatFilterMethod = "concat";
    internal const string inFilterMethod = "in";

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

    internal static bool TryGetValue(MemberInfoKey key, out string methodName, out bool isStatic)
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

        methodName = string.Empty;
        isStatic = false;
        return false;
    }
}
