// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;
using System.Globalization;

namespace CommunityToolkit.Datasync.Client.Query.OData;

/// <summary>
/// The list of supported types for constants.
/// </summary>
internal enum ConstantType
{
    Unknown,
    Null,
    Boolean,
    Byte,
    Character,
    Decimal,
    Double,
    Float,
    Int,
    Long,
    Short,
    SignedByte,
    UnsignedInt,
    UnsignedLong,
    UnsignedShort,
    StringArray
}

/// <summary>
/// A set of extension methods that provide table lookup capabilities.
/// </summary>
internal static class TableLookupExtensions
{
    private static readonly Dictionary<long, ConstantType> ConstantTypeLookupTable = new()
        {
            { typeof(bool).TypeHandle.Value, ConstantType.Boolean },
            { typeof(byte).TypeHandle.Value, ConstantType.Byte },
            { typeof(char).TypeHandle.Value, ConstantType.Character },
            { typeof(decimal).TypeHandle.Value, ConstantType.Decimal },
            { typeof(double).TypeHandle.Value, ConstantType.Double },
            { typeof(float).TypeHandle.Value, ConstantType.Float },
            { typeof(int).TypeHandle.Value, ConstantType.Int },
            { typeof(long).TypeHandle.Value, ConstantType.Long },
            { typeof(short).TypeHandle.Value, ConstantType.Short },
            { typeof(sbyte).TypeHandle.Value, ConstantType.SignedByte },
            { typeof(uint).TypeHandle.Value, ConstantType.UnsignedInt },
            { typeof(ulong).TypeHandle.Value, ConstantType.UnsignedLong },
            { typeof(ushort).TypeHandle.Value, ConstantType.UnsignedShort }
        };

    /// <summary>
    /// Converts an object into the supported constant type.
    /// </summary>
    /// <param name="value">The reference value</param>
    /// <returns>The <see cref="ConstantType"/></returns>
    internal static ConstantType GetConstantType(this object value)
    {
        if (value == null)
        {
            return ConstantType.Null;
        }

        // Special case of string arrays since they are handled for "in" clauses.
        if (value is IEnumerable<string>)
        {
            return ConstantType.StringArray;
        }

        long handle = value.GetType().TypeHandle.Value;
        if (ConstantTypeLookupTable.TryGetValue(handle, out ConstantType @type))
        {
            return @type;
        }
        else
        {
            return ConstantType.Unknown;
        }
    }

    /// <summary>
    /// Converts the <see cref="BinaryOperatorKind"/> to an OData operator.
    /// </summary>
    internal static string ToODataString(this BinaryOperatorKind kind) => kind switch
    {
        BinaryOperatorKind.Or => "or",
        BinaryOperatorKind.And => "and",
        BinaryOperatorKind.Equal => "eq",
        BinaryOperatorKind.NotEqual => "ne",
        BinaryOperatorKind.GreaterThan => "gt",
        BinaryOperatorKind.GreaterThanOrEqual => "ge",
        BinaryOperatorKind.LessThan => "lt",
        BinaryOperatorKind.LessThanOrEqual => "le",
        BinaryOperatorKind.Add => "add",
        BinaryOperatorKind.Subtract => "sub",
        BinaryOperatorKind.Multiply => "mul",
        BinaryOperatorKind.Divide => "div",
        BinaryOperatorKind.Modulo => "mod",
        _ => throw new NotSupportedException($"'{kind}' is not supported in a 'Where' table query expression.")
    };

    /// <summary>
    /// Converts a constant to the value of the OData representation.
    /// </summary>
    /// <param name="node">The <see cref="ConstantNode"/> to convert.</param>
    /// <returns>The OData representation of the constant node value.</returns>
    internal static string ToODataString(this ConstantNode node)
    {
        object value = node.Value;
        switch (value.GetConstantType())
        {
            case ConstantType.Null:
                return "null";
            case ConstantType.Boolean:
                return ((bool)value).ToString().ToLower();
            case ConstantType.Byte:
                return $"{value:X2}";
            case ConstantType.Character:
                string ch = (char)value == '\'' ? "''" : ((char)value).ToString();
                return $"'{ch}'";
            case ConstantType.Decimal:
                string m = ((decimal)value).ToString("G", CultureInfo.InvariantCulture);
                return $"{m}M";
            case ConstantType.Double:
                string d = ((double)value).ToString("G", CultureInfo.InvariantCulture);
                return (d.Contains('E') || d.Contains('.')) ? d : $"{d}.0";
            case ConstantType.Float:
                string f = ((float)value).ToString("G", CultureInfo.InvariantCulture);
                return $"{f}f";
            case ConstantType.Int:
            case ConstantType.Short:
            case ConstantType.UnsignedShort:
            case ConstantType.SignedByte:
                return $"{value}";
            case ConstantType.Long:
            case ConstantType.UnsignedInt:
            case ConstantType.UnsignedLong:
                return $"{value}L";
            case ConstantType.StringArray:
                IEnumerable<string> stringArray = (value as IEnumerable<string>).Select(x => $"'{x}'");
                return $"({string.Join(",", stringArray)})";
            default:
                return EdmTypeSupport.ToODataString(value) ?? $"'{value.ToString().Replace("'", "''")}'";
        }
    }
}
