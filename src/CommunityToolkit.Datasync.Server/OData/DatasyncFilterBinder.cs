// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.UriParser;
using Microsoft.OData;
using Microsoft.Spatial;
using System.Linq.Expressions;
using System.Reflection;

namespace CommunityToolkit.Datasync.Server.OData;

/// <summary>
/// Implements the filter binder for the additional geo-spatial functions for an OData query.
/// </summary>
internal class DatasyncFilterBinder : FilterBinder
{
    /// <summary>
    /// Convenience constant to get all the static methods of a class.
    /// </summary>
    internal const BindingFlags AllMethods = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// The name of the geo.distance function.
    /// </summary>
    internal const string GeoDistanceFunctionName = "geo.distance";

    /// <summary>
    /// The list of functions that we support, together with the <see cref="MethodInfo"/> for the implementation.
    /// </summary>
    internal static readonly Dictionary<string, MethodInfo> FunctionMethods = new()
    {
        { GeoDistanceFunctionName, typeof(GeospatialFunctions).GetMethod("GeoDistance", AllMethods, [ typeof(GeographyPoint), typeof(GeographyPoint) ])! }
    };

    /// <inheritdoc />
    public override Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode node, QueryBinderContext context)
        => node.Name switch
        {
            GeoDistanceFunctionName => BindGeoDistanceFunctionCallNode(node, context),
            _ => base.BindSingleValueFunctionCallNode(node, context)
        };

    /// <summary>
    /// The filter binding for the geo.distance function.
    /// </summary>
    /// <param name="node">The node that holds the parameters for the call.</param>
    /// <param name="context">The current query binder context.</param>
    /// <returns>An expression for the function result.</returns>
    public Expression BindGeoDistanceFunctionCallNode(SingleValueFunctionCallNode node, QueryBinderContext context)
    {
        Expression[] arguments = BindArguments(node.Parameters, context);
        return Expression.Call(FunctionMethods[GeoDistanceFunctionName], arguments[0], arguments[1]);
    }
}
