// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A set of internal extension methods for this library.
/// </summary>
internal static class InternalExtensions
{
    private static readonly Lazy<JsonSerializerOptions> _options = new(() => GetSerializerOptions());
    private const string IncludeDeletedParameterName = "__includedeleted";

    /// <summary>
    /// Applies an optional predicate to a query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="predicate">The optional predicate to add to the query.</param>
    /// <returns>An updated <see cref="IQueryable{T}"/> representing the new query.</returns>
    internal static IQueryable<T> ApplyDataView<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
        => predicate is null ? query : query.Where(predicate);

    /// <summary>
    /// Filters out the deleted entities unless the request includes an optional parameter to include them.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="request">The current <see cref="HttpRequest"/> being processed.</param>
    /// <param name="enableSoftDelete">A flag to indicate if soft-delete is enabled on the table being queried.</param>
    /// <returns>An updated <see cref="IQueryable{T}"/> representing the new query.</returns>
    internal static IQueryable<T> ApplyDeletedView<T>(this IQueryable<T> query, HttpRequest request, bool enableSoftDelete) where T : ITableData
        => !enableSoftDelete || request.ShouldIncludeDeletedEntities() ? query : query.Where(e => !e.Deleted);

    /// <summary>
    /// Applies the <c>$filter</c> OData query option to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="filterQueryOption">The filter query option to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the filtered data.</returns>
    internal static IQueryable<T> ApplyODataFilter<T>(this IQueryable<T> query, FilterQueryOption? filterQueryOption, ODataQuerySettings settings)
    {
        if (filterQueryOption is null)
        {
            return query;
        }

        return ((IQueryable<T>?)filterQueryOption.ApplyTo(query, settings)) ?? query;
    }

    /// <summary>
    /// Applies the <c>$orderBy</c> OData query option to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="orderingQueryOption">The ordering query option to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the ordered data.</returns>
    internal static IQueryable<T> ApplyODataOrderBy<T>(this IQueryable<T> query, OrderByQueryOption? orderingQueryOption, ODataQuerySettings settings) where T : ITableData
    {
        // Note that we ALWAYS do a sort so that the ordering is consistent across all queries.
        if (orderingQueryOption is null)
        {
            return query.OrderBy(e => e.Id);
        }

        IOrderedQueryable<T>? orderedQuery = orderingQueryOption.ApplyTo(query, settings);
        if (orderedQuery is null)
        {
            return query.OrderBy(e => e.Id);
        }

        return orderedQuery.ThenBy(x => x.Id);
    }

    /// <summary>
    /// Applies the <c>$skip</c> and <c>$top</c> OData query options to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="query">The current <see cref="IQueryable{T}"/> representing the query.</param>
    /// <param name="options">The query options to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> representing the paged data.</returns>
    internal static IQueryable<T> ApplyODataPaging<T>(this IQueryable<T> query, ODataQueryOptions<T> options, ODataQuerySettings settings)
    {
        int takeValue = Math.Min(options.Top?.Value ?? int.MaxValue, settings.PageSize ?? 100);
        int skipValue = Math.Max(options.Skip?.Value ?? 0, 0);
        return query.Skip(skipValue).Take(takeValue);
    }

    /// <summary>
    /// Applies the <c>$select</c> OData query option to the provided query.
    /// </summary>
    /// <typeparam name="T">The type of entity being queried.</typeparam>
    /// <param name="dataset">The datset to apply the <c>$select</c> option to.</param>
    /// <param name="queryOption">The <see cref="SelectExpandQueryOption"/> to apply.</param>
    /// <param name="settings">The query settings being used.</param>
    /// <returns>The resulting dataset after property selection.</returns>
    internal static IEnumerable<object> ApplyODataSelect<T>(this IList<T> dataset, SelectExpandQueryOption? queryOption, ODataQuerySettings settings)
    {
        if (dataset.Count == 0 || queryOption is null)
        {
            return dataset.Cast<object>().AsEnumerable();
        }

        return queryOption.ApplyTo(dataset.AsQueryable(), settings).Cast<object>().ToList().Select(x => ((ISelectExpandWrapper)x).ToDictionary());
    }

    /// <summary>
    /// Determines if the provided entity is in the view of the current user.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="accessControlProvider">The <see cref="IAccessControlProvider{TEntity}"/> that controls access to entities.</param>
    /// <param name="entity">The entity being checked.</param>
    /// <returns><c>true</c> if the entity is in view; <c>false</c> otherwise.</returns>
    internal static bool EntityIsInView<TEntity>(this IAccessControlProvider<TEntity> accessControlProvider, TEntity entity) where TEntity : ITableData
        => accessControlProvider.GetDataView()?.Compile().Invoke(entity) != false;

    /// <summary>
    /// Creates a new instance of <see cref="JsonSerializerOptions"/> with the default converters.
    /// </summary>
    /// <returns>A valid <see cref="JsonSerializerOptions"/> object.</returns>
    internal static JsonSerializerOptions GetSerializerOptions() => new(JsonSerializerDefaults.General)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new DateTimeOffsetConverter(),
            new DateTimeConverter(),
            new TimeOnlyConverter(),
            new SpatialGeoJsonConverter()
        },
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    /// <summary>
    /// Determines if the <paramref name="left"/> is after the <paramref name="right"/>, taking into account null values.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the <paramref name="left"/> is after the <paramref name="right"/>.</returns>
    internal static bool IsAfter(this DateTimeOffset left, DateTimeOffset? right)
        => !right.HasValue || left > right.Value;

    /// <summary>
    /// Determines if the <paramref name="left"/> is before (or the same as) the <paramref name="right"/>, taking into account null values.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the <paramref name="left"/> is before the <paramref name="right"/>.</returns>
    internal static bool IsBefore(this DateTimeOffset left, DateTimeOffset? right)
        => !right.HasValue || left <= right.Value;

    /// <summary>
    /// Determines if the entity tag in the <c>If-Match</c> or <c>If-None-Match</c> header matches the entity version.
    /// </summary>
    /// <param name="etag">The entity tag header value.</param>
    /// <param name="version">The version in the entity.</param>
    /// <returns><c>true</c> if the entity tag header value matches the version; <c>false</c> otherwise.</returns>
    internal static bool Matches(this EntityTagHeaderValue etag, byte[] version)
        => !etag.IsWeak && version.Length > 0 && (etag.Tag == "*" || etag.Tag.ToString().Trim('"').Equals(version.ToEntityTagValue()));

    /// <summary>
    /// Determines if the request has met the preconditions within the conditional headers, according to RFC 7232 section 5 and 6.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being checked.</typeparam>
    /// <param name="request">The current <see cref="HttpRequest"/> object that contains the request headers.</param>
    /// <param name="entity">The entity being checked.</param>
    /// <param name="version">On conclusion, the version that was requested.</param>
    /// <exception cref="HttpException">Thrown if the conditional request requirements are not met.</exception>
    internal static void ParseConditionalRequest<TEntity>(this HttpRequest request, TEntity entity, out byte[] version) where TEntity : ITableData
    {
        RequestHeaders headers = request.GetTypedHeaders();
        bool isFetch = request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase);

        if (headers.IfMatch.Count > 0 && !headers.IfMatch.Any(e => e.Matches(entity.Version)))
        {
            throw new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfMatch.Count == 0 && headers.IfUnmodifiedSince.HasValue && headers.IfUnmodifiedSince.Value.IsBefore(entity.UpdatedAt))
        {
            throw new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfNoneMatch.Count > 0 && headers.IfNoneMatch.Any(e => e.Matches(entity.Version)))
        {
            throw isFetch ? new HttpException(StatusCodes.Status304NotModified) : new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfNoneMatch.Count == 0 && headers.IfModifiedSince.HasValue && headers.IfModifiedSince.Value.IsAfter(entity.UpdatedAt))
        {
            throw isFetch ? new HttpException(StatusCodes.Status304NotModified) : new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        version = headers.IfMatch.SingleOrDefault()?.ToByteArray() ?? [];
    }

    /// <summary>
    /// Adds the required conditional headers to a header dictionary.
    /// </summary>
    /// <param name="headers">The current header dictionary.</param>
    /// <param name="entity">Tne entity to use for setting conditional header values.</param>
    internal static void SetConditionalHeaders(this IHeaderDictionary headers, ITableData entity)
    {
        _ = headers.Remove(HeaderNames.ETag);
        _ = headers.Remove(HeaderNames.LastModified);

        if (entity.Version.Length > 0)
        {
            headers.Append(HeaderNames.ETag, $"\"{entity.Version.ToEntityTagValue()}\"");
        }

        if (entity.UpdatedAt.HasValue && entity.UpdatedAt.Value != default)
        {
            headers.Append(HeaderNames.LastModified, entity.UpdatedAt.Value.ToString(DateTimeFormatInfo.InvariantInfo.RFC1123Pattern, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Determines if the client requested that the deleted items should be considered to
    /// be "in view".
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> being processed.</param>
    /// <returns><c>true</c> if deleted items should be considered "in view"; <c>false</c> otherwise.</returns>
    internal static bool ShouldIncludeDeletedEntities(this HttpRequest request)
    {
        if (request.Query.TryGetValue(IncludeDeletedParameterName, out StringValues deletedQueryParameter))
        {
            return deletedQueryParameter.Any(x => x!.Equals("true", StringComparison.InvariantCultureIgnoreCase));
        }

        return false;
    }

    /// <summary>
    /// Converts the provided ETag into a byte array.
    /// </summary>
    /// <param name="etag">The ETag to convert.</param>
    /// <returns>The ETag converted to a byte array.</returns>
    internal static byte[] ToByteArray(this EntityTagHeaderValue etag)
        => etag.Tag == "*" ? [] : Convert.FromBase64String(etag.Tag.ToString().Trim('"'));

    /// <summary>
    /// Converts a byte array to an entity tag value.
    /// </summary>
    /// <param name="version">The version to convert.</param>
    /// <returns>The version string.</returns>
    internal static string ToEntityTagValue(this byte[] version)
        => Convert.ToBase64String(version);

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <remarks>
    /// This is used in logging.  We capture all errors and exceptions and return a default string.
    /// </remarks>
    /// <param name="object">The object to be serialized.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The serialized object.</returns>
    internal static string ToJsonString(this object @object, JsonSerializerOptions? options = null)
    {
        try
        {
            if (@object is null)
            {
                return "null";
            }

            return JsonSerializer.Serialize(@object, options ?? _options.Value);
        }
        catch (Exception)
        {
            return "unserializable object";
        }
    }
}
