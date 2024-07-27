// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace CommunityToolkit.Datasync.Client.Query;

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
            {
                typeof(sbyte),
                new[] { Tshort, Tint, Tlong, Tfloat, Tdouble, Tdecimal, Tnint }
            },
            {
                typeof(byte),
                new[] { Tshort, Tushort, Tint, Tuint, Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnint, Tnuint }
            },
            {
                typeof(short),
                new[] { Tint, Tlong, Tfloat, Tdouble, Tdecimal, Tnint }
            },
            {
                typeof(ushort),
                new[] { Tint, Tuint, Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnint, Tnuint }
            },
            {
                typeof(int),
                new[] { Tlong, Tfloat, Tdouble, Tdecimal, Tnint }
            },
            {
                typeof(uint),
                new[] { Tlong, Tulong, Tfloat, Tdouble, Tdecimal, Tnuint }
            },
            {
                typeof(long),
                new[] { Tfloat, Tdouble, Tdecimal }
            },
            {
                typeof(ulong),
                new[] { Tfloat, Tdouble, Tdecimal }
            },
            {
                typeof(float),
                new[] { Tdouble }
            },
            {
                typeof(nint),
                new[] { Tlong, Tfloat, Tdouble, Tdecimal }
            },
            {
                typeof(nuint),
                new[] { Tulong, Tfloat, Tdouble, Tdecimal }
            }
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

        if (uFrom == uTo || uFrom.GetTypeInfo().IsEnum)
        {
            return true;
        }

        if (_table.Value.TryGetValue(uFrom, out Type[]? conversions))
        {
            return Array.IndexOf(conversions, uTo) >= 0;
        }

        return false;
    }
}
