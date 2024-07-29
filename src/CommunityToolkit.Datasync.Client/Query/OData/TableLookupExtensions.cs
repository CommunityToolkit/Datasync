// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq;
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
    StringArray,
    Date,
    TimeOfDay,
    DateTime,
    DateTimeOffset,
    Guid
}

/// <summary>
/// A set of conversion methods to support OData conversion.
/// </summary>
internal static class TableLookupExtensions
{
    private static readonly Dictionary<long, ConstantType> ConstantTypeLookupTable = new()
    {
        { (long)typeof(bool).TypeHandle.Value, ConstantType.Boolean },
        { (long)typeof(byte).TypeHandle.Value, ConstantType.Byte },
        { (long)typeof(char).TypeHandle.Value, ConstantType.Character },
        { (long)typeof(decimal).TypeHandle.Value, ConstantType.Decimal },
        { (long)typeof(double).TypeHandle.Value, ConstantType.Double },
        { (long)typeof(float).TypeHandle.Value, ConstantType.Float },
        { (long)typeof(int).TypeHandle.Value, ConstantType.Int },
        { (long)typeof(long).TypeHandle.Value, ConstantType.Long },
        { (long)typeof(short).TypeHandle.Value, ConstantType.Short },
        { (long)typeof(sbyte).TypeHandle.Value, ConstantType.SignedByte },
        { (long)typeof(uint).TypeHandle.Value, ConstantType.UnsignedInt },
        { (long)typeof(ulong).TypeHandle.Value, ConstantType.UnsignedLong },
        { (long)typeof(ushort).TypeHandle.Value, ConstantType.UnsignedShort },
        { (long)typeof(DateOnly).TypeHandle.Value, ConstantType.Date },
        { (long)typeof(TimeOnly).TypeHandle.Value, ConstantType.TimeOfDay },
        { (long)typeof(DateTime).TypeHandle.Value, ConstantType.DateTime },
        { (long)typeof(DateTimeOffset).TypeHandle.Value, ConstantType.DateTimeOffset },
        { (long)typeof(Guid).TypeHandle.Value, ConstantType.Guid }
    };

    /// <summary>
    /// The format of the date/time transition (in universal time).
    /// </summary>
    private const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ";

    /// <summary>
    /// The format for the DateOnly type
    /// </summary>
    private const string DateOnlyFormat = "yyyy-MM-dd";

    /// <summary>
    /// The format for the TimeOnly type
    /// </summary>
    private const string TimeOnlyFormat = "hh:mm:ss";

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

        long handle = (long)value.GetType().TypeHandle.Value;
        return (ConstantTypeLookupTable.TryGetValue(handle, out ConstantType constantType)) ? constantType : ConstantType.Unknown;
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
        string formattedString = string.Empty;
        switch (value.GetConstantType())
        {
            case ConstantType.Null:
                return "null";
            case ConstantType.Boolean:
                return ((bool)value).ToString().ToLower();
            case ConstantType.Byte:
                return $"{value:X2}";
            case ConstantType.Character:
                formattedString = (char)value == '\'' ? "''" : ((char)value).ToString();
                return $"'{formattedString}'";
            case ConstantType.Decimal:
                formattedString = ((decimal)value).ToString("G", CultureInfo.InvariantCulture);
                return $"{formattedString}M";
            case ConstantType.Double:
                formattedString = ((double)value).ToString("G", CultureInfo.InvariantCulture);
                return (formattedString.Contains('E') || formattedString.Contains('.')) ? formattedString : $"{formattedString}.0";
            case ConstantType.Float:
                formattedString = ((float)value).ToString("G", CultureInfo.InvariantCulture);
                return $"{formattedString}f";
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
                IEnumerable<string> stringArray = ((IEnumerable<string>)value).Select(x => $"'{x}'");
                return $"({string.Join(",", stringArray)})";
            case ConstantType.Date:
                formattedString = ((DateOnly)value).ToString(DateOnlyFormat);
                return $"cast({formattedString},Edm.Date)";
            case ConstantType.TimeOfDay:
                formattedString = ((TimeOnly)value).ToString(TimeOnlyFormat);
                return $"cast({formattedString},Edm.TimeOfDay)";
            case ConstantType.DateTime:
                formattedString = new DateTimeOffset(((DateTime)value).ToUniversalTime()).ToString(DateTimeFormat);
                return $"cast({formattedString},Edm.DateTimeOffset)";
            case ConstantType.DateTimeOffset:
                formattedString = ((DateTimeOffset)value).ToUniversalTime().ToString(DateTimeFormat);
                return $"cast({formattedString},Edm.DateTimeOffset)";
            case ConstantType.Guid:
                formattedString = string.Format("{0:D}", (Guid)value);
                return $"cast({formattedString},Edm.Guid)";
            default:
                return $"'{value.ToString()?.Replace("'", "''")}'";
        }
    }
}
