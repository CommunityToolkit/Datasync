// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Reflection and LINQ requires a lot of null manipulation, so we opt for
// a generalized "nullable" option here to allow us to do that.
#nullable disable

using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Query.Linq;

/// <summary>
/// A list of implicit numeric type conversions.
/// </summary>
public static class ImplicitConversions
{
    private static readonly Type Tdecimal = typeof(decimal);
    private static readonly Type Tdouble = typeof(double);
    private static readonly Type Tfloat = typeof(float);
    private static readonly Type Tint = typeof(int);
    private static readonly Type Tlong = typeof(long);
    private static readonly Type Tnint = typeof(nint);
    private static readonly Type Tnuint = typeof(nuint);
    private static readonly Type Tshort = typeof(short);
    private static readonly Type Tuint = typeof(uint);
    private static readonly Type Tulong = typeof(ulong);
    private static readonly Type Tushort = typeof(ushort);

    /// <summary>
    /// The table of implicit numeric conversions from <see href="https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/numeric-conversions"/>
    /// </summary>
    private static readonly Lazy<Dictionary<Type, Type[]>> _table = new(() => new()
        {
            { typeof(sbyte), new[] { Tshort, Tint, Tlong, Tfloat, Tdouble, Tdecimal, Tnint } },
            { typeof(byte), new[] { Tshort, Tushort, Tint, Tuint, Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnint, Tnuint } },
            { typeof(short), new[] { Tint, Tlong, Tfloat, Tdouble, Tdecimal, Tnint } },
            { typeof(ushort), new[] { Tint, Tuint, Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnint, Tnuint } },
            { typeof(int), new[] { Tlong, Tfloat, Tdouble, Tdecimal, Tnint } },
            { typeof(uint), new[] { Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnuint } },
            { typeof(long), new[] { Tfloat, Tdouble, Tdecimal } },
            { typeof(ulong), new[] { Tfloat, Tdouble, Tdecimal } },
            { typeof(float), new[] { Tdouble } },
            { typeof(nint), new[] { Tlong, Tfloat, Tdouble, Tdecimal } },
            { typeof(nuint), new[] { Tulong, Tfloat, Tdouble, Tdecimal } }
        });

    /// <summary>
    /// Given a <see cref="Nullable{T}"/>, find out the underlying type.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>The underlying type</returns>
    internal static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    /// Determines if the type conversion being considered is "implicit" according to
    /// .NET rules.
    /// </summary>
    /// <param name="from">The source type</param>
    /// <param name="to">The converted type</param>
    /// <returns>True if we can convert the types implicitly</returns>
    public static bool IsImplicitConversion(Type from, Type to)
    {
        Type uFrom = Unwrap(from), uTo = Unwrap(to);

        if (uFrom == uTo)
        {
            return true;
        }

        if (uFrom.GetTypeInfo().IsEnum)
        {
            return true;
        }

        if (_table.Value.TryGetValue(uFrom, out Type[] conversions))
        {
            return Array.IndexOf(conversions, uTo) >= 0;
        }

        return false;
    }
}

/// <summary>
/// The list of instance properties and the OData method name that supports them.
/// </summary>
internal static class InstanceProperties
{
    internal static readonly Lazy<Dictionary<MemberInfoKey, string>> _table = new(() => new()
        {
            { new MemberInfoKey(typeof(DateOnly), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateOnly), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateOnly), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(TimeOnly), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(TimeOnly), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(TimeOnly), "Second", false, true), "second" },
            { new MemberInfoKey(typeof(string), "Length", false, true), "length" },
            { new MemberInfoKey(typeof(DateTime), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateTime), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateTime), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(DateTime), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(DateTime), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(DateTime), "Second", false, true), "second" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Day", false, true), "day" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Month", false, true), "month" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Year", false, true), "year" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Hour", false, true), "hour" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Minute", false, true), "minute" },
            { new MemberInfoKey(typeof(DateTimeOffset), "Second", false, true), "second" }
        });

    /// <summary>
    /// Gets the method name from the key, or null if it doesn't exist.
    /// </summary>
    /// <param name="key">The <see cref="MemberInfoKey"/></param>
    /// <returns>The method name</returns>
    internal static string GetMethodName(MemberInfoKey key)
        => _table.Value.TryGetValue(key, out string methodName) ? methodName : null;
}

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

        methodName = "";
        isStatic = false;
        return false;
    }
}
