// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Query.Linq.Nodes;

namespace CommunityToolkit.Datasync.Client.Query.OData;

/// <summary>
/// A set of methods for converting to/from supported types.
/// </summary>
internal static class EdmTypeSupport
{
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
    /// A list of the known Edm types we have a mechanism to convert.
    /// </summary>
    private enum EdmType
    {
        Date,
        TimeOfDay,
        DateTime,
        DateTimeOffset,
        Guid
    };

    /// <summary>
    /// The type lookup table to get the type over to the
    /// </summary>
    private static readonly Dictionary<long, EdmType> TypeLookupTable = new()
        {
            { typeof(DateOnly).TypeHandle.Value, EdmType.Date },
            { typeof(TimeOnly).TypeHandle.Value, EdmType.TimeOfDay },
            { typeof(DateTime).TypeHandle.Value, EdmType.DateTime },
            { typeof(DateTimeOffset).TypeHandle.Value, EdmType.DateTimeOffset },
            { typeof(Guid).TypeHandle.Value, EdmType.Guid }
        };

    /// <summary>
    /// A lookup table to convert from the string EdmType to the enum EdmType.
    /// </summary>
    private static readonly Dictionary<string, EdmType> EdmLookupTable = new()
        {
            { "Edm.Date", EdmType.Date },
            { "Edm.TimeOfDay", EdmType.TimeOfDay },
            { "Edm.DateTime", EdmType.DateTime },
            { "Edm.DateTimeOffset", EdmType.DateTimeOffset },
            { "Edm.Guid", EdmType.Guid }
        };

    /// <summary>
    /// Converts the given value to the Edm OData form, or returns null
    /// if the value is not of a known type.
    /// </summary>
    /// <param name="value">The value to serialize</param>
    /// <returns>The oData representation of the value</returns>
    public static string ToODataString(object value)
    {
        long handle = value.GetType().TypeHandle.Value;
        if (!TypeLookupTable.TryGetValue(handle, out EdmType type))
        {
            return null;
        }

        // Because we have the type lookup table, we can safely ignore the warning here.
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        return type switch
        {
            EdmType.Date => $"cast({((DateOnly)value).ToString(DateOnlyFormat)},Edm.Date)",
            EdmType.TimeOfDay => $"cast({((TimeOnly)value).ToString(TimeOnlyFormat)},Edm.TimeOfDay)",
            EdmType.DateTime => $"cast({new DateTimeOffset(((DateTime)value).ToUniversalTime()).ToString(DateTimeFormat)},Edm.DateTimeOffset)",
            EdmType.DateTimeOffset => $"cast({((DateTimeOffset)value).ToUniversalTime().ToString(DateTimeFormat)},Edm.DateTimeOffset)",
            EdmType.Guid => $"cast({string.Format("{0:D}", (Guid)value)},Edm.Guid)"
        };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
    }

    /// <summary>
    /// Converts an OData string back into the QueryNode.  This is used during OData filter parsing.
    /// </summary>
    /// <param name="literal">The value of the literal</param>
    /// <param name="typestr">The type string.</param>
    /// <returns>The <see cref="QueryNode"/> for the cast.</returns>
    public static QueryNode ToQueryNode(string literal, string typestr)
    {
        if (!EdmLookupTable.TryGetValue(typestr, out EdmType type))
        {
            throw new InvalidOperationException($"Edm Type '{typestr}' is not valid.");
        }

        // Because we have the type lookup table, we can safely ignore the warning here.
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        return type switch
        {
            EdmType.Date => new ConstantNode(DateOnly.ParseExact(literal, DateOnlyFormat)),
            EdmType.TimeOfDay => new ConstantNode(TimeOnly.ParseExact(literal, TimeOnlyFormat)),
            EdmType.DateTime => new ConstantNode(DateTime.Parse(literal)),
            EdmType.DateTimeOffset => new ConstantNode(DateTimeOffset.Parse(literal)),
            EdmType.Guid => new ConstantNode(Guid.Parse(literal)),
        };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
    }
}
