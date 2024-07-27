// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;

namespace CommunityToolkit.Datasync.Client.Query;

/// <summary>
/// Translates an expression tree into an OData expression.
/// </summary>
internal class ODataExpressionVisitor : QueryNodeVisitor<QueryNode>
{
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
    /// Translates an expression tree of <see cref="QueryNode"/> elements
    /// to an OData expression.
    /// </summary>
    /// <param name="filter">The top <see cref="QueryNode"/> representing the entire expression.</param>
    /// <returns>An OData string.</returns>
    public static string ToODataString(QueryNode filter)
    {
        if (filter == null)
        {
            return string.Empty;
        }

        ODataExpressionVisitor visitor = new();
        _ = filter.Accept(visitor);
        return visitor.Expression.ToString();
    }

    /// <summary>
    /// You cannot instantiate this - access the visitor through the static methods.
    /// </summary>
    protected ODataExpressionVisitor()
    {
    }

    /// <summary>
    /// The OData expression.
    /// </summary>
    public StringBuilder Expression { get; } = new();

    #region QueryNodeVisitor<QueryNode>
    /// <summary>
    /// Visit a <see cref="BinaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(BinaryOperatorNode node)
    {
        _ = Expression.Append('(');
        Accept(node, node.LeftOperand!);
        _ = Expression.Append(' ').Append(node.OperatorKind.ToODataString()).Append(' ');
        Accept(node, node.RightOperand!);
        _ = Expression.Append(')');
        return node;
    }

    /// <summary>
    /// Visit a <see cref="ConstantNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(ConstantNode node)
    {
        _ = Expression.Append(ToODataString(node));
        return node;
    }

    /// <summary>
    /// Visit a <see cref="ConvertNode"/>
    /// </summary>
    /// <remarks>
    /// This should never happen, but it's added for compatibility with the interface
    /// </remarks>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(ConvertNode node)
    {
        throw new NotSupportedException("ConvertNode is not supported on the ODataExpressionVisitor");
    }

    /// <summary>
    /// Visit a <see cref="FunctionCallNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(FunctionCallNode node)
    {
        // Special case: string[].Contains(string) is represented as "string in ( string, string, ...)" in OData
        if (node.Name == "in")
        {
            Accept(node, node.Arguments[1]);
            _ = Expression.Append(" in ");
            Accept(node, node.Arguments[0]);
            return node;
        }

        bool appendSeparator = false;
        _ = Expression.Append(node.Name).Append('(');
        foreach (QueryNode arg in node.Arguments)
        {
            if (appendSeparator)
            {
                _ = Expression.Append(',');
            }

            Accept(node, arg);
            appendSeparator = true;
        }

        _ = Expression.Append(')');
        return node;
    }

    /// <summary>
    /// Visit a <see cref="MemberAccessNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(MemberAccessNode node)
    {
        _ = Expression.Append(node.MemberName);
        return node;
    }

    /// <summary>
    /// Visit a <see cref="UnaryOperatorNode"/>
    /// </summary>
    /// <param name="node">The node to visit</param>
    /// <returns>The visited node</returns>
    internal override QueryNode Visit(UnaryOperatorNode node)
    {
        switch (node.OperatorKind)
        {
            case UnaryOperatorKind.Not:
                _ = Expression.Append("not(");
                Accept(node, node.Operand!);
                _ = Expression.Append(')');
                break;
            default:
                throw new NotSupportedException($"'{node.OperatorKind}' is not supported in a table query");
        }

        return node;
    }
    #endregion

    /// <summary>
    /// Accept a visitor to a node, with error checking
    /// </summary>
    /// <param name="parent">The parent node</param>
    /// <param name="node">The node to visit</param>
    protected void Accept(QueryNode parent, QueryNode node)
    {
        if (node == null)
        {
            throw new ArgumentException($"Parent {parent.Kind} is not complete.", nameof(node));
        }
        else
        {
            _ = node.Accept(this);
        }
    }

    /// <summary>
    /// Converts an object into the supported constant type.
    /// </summary>
    /// <param name="value">The reference value</param>
    /// <returns>The <see cref="ConstantType"/></returns>
    internal static ConstantType GetConstantType(object value)
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
        return ConstantTypeLookupTable.TryGetValue(handle, out ConstantType constantType) ? constantType: ConstantType.Unknown;
    }

    /// <summary>
    /// Converts a constant to the value of the OData representation.
    /// </summary>
    /// <param name="node">The <see cref="ConstantNode"/> to convert.</param>
    /// <returns>The OData representation of the constant node value.</returns>
    internal static string ToODataString(ConstantNode node)
    {
        object value = node.Value;
        string formattedString = string.Empty;
        switch (GetConstantType(value))
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
