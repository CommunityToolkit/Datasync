// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A set of extension methods to help working with a <see cref="HttpRequest"/> object.
/// </summary>
internal static class HttpRequestExtensions
{
    /// <summary>
    /// Creates the NextLink Uri for the next page in a paging request.
    /// </summary>
    /// <param name="request">The source of the original request.</param>
    /// <param name="skip">The skip value.</param>
    /// <param name="top">The top value.</param>
    /// <returns>A URI representing the next page.</returns>
    internal static string CreateNextLink(this HttpRequest request, int skip = 0, int top = 0)
    {
        UriBuilder builder = new(request.GetDisplayUrl());
        List<string> query = string.IsNullOrEmpty(builder.Query) 
            ? new() 
            : [.. builder.Query.TrimStart('?').Split('&').Where(q => !q.StartsWith("$skip=") && !q.StartsWith("$top="))];
        
        if (skip > 0) 
        {
            query.Add($"$skip={skip}");
        }

        if (top > 0)
        {
            query.Add($"$top={top}");
        }

        return string.Join('&', query).TrimStart('&');
    }

    /// <summary>
    /// Determines if one date is after another date.
    /// </summary>
    /// <remarks>
    /// This is used in checking the If-Modified-Since and If-Unmodified-Since headers for date/time comparisons with nulls.
    /// </remarks>
    /// <param name="left">The date to use as a source.</param>
    /// <param name="right">The optional date to use as a comparison.</param>
    /// <returns><c>true</c> if the left date is after the right date or the right date is null.</returns>
    internal static bool IsAfter(this DateTimeOffset left, DateTimeOffset? right)
        => !right.HasValue || left > right.Value;

    /// <summary>
    /// Determines if one date is before another date.
    /// </summary>
    /// <remarks>
    /// This is used in checking the If-Modified-Since and If-Unmodified-Since headers for date/time comparisons with nulls.
    /// </remarks>
    /// <param name="left">The date to use as a source.</param>
    /// <param name="right">The optional date to use as a comparison.</param>
    /// <returns><c>true</c> if the left date is before the right date or the right date is null.</returns>
    internal static bool IsBefore(this DateTimeOffset left, DateTimeOffset? right)
        => !right.HasValue || left <= right.Value;

    /// <summary>
    /// Determines if the provided <see cref="EntityTagHeaderValue"/> is equivalent to the provided version.
    /// </summary>
    /// <param name="etag">The <see cref="EntityTagHeaderValue"/> to check.</param>
    /// <param name="version">The expected version.</param>
    /// <returns><c>true</c> if the <paramref name="etag"/> matches the <paramref name="version"/>.</returns>
    internal static bool Matches(this EntityTagHeaderValue etag, byte[] version)
        => !etag.IsWeak && version.Length > 0 && (etag.Tag == "*" || etag.Tag.ToString().Trim('"').Equals(version.ToEntityTagValue()));

    /// <summary>
    /// Determines if the request has met the preconditions within the conditional headers according to RFC 7232 sections 5 and 6.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being checked.</typeparam>
    /// <param name="request">The current <see cref="HttpRequest"/> object that contains the request headers.</param>
    /// <param name="entity">The entity being checked.</param>
    /// <param name="version">On conclusion, the version of the entity that was requested.</param>
    /// <exception cref="HttpException">Thrown if the request does not meet the preconditions within the conditional headers.</exception>
    internal static void ParseConditionalRequest<TEntity>(this HttpRequestError request, TEntity entity, out byte[] version) where TEntity : ITableData
    {
        var headers = request.GetTypedHeaders();
        bool isFetch = request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase);
        
        if (headers.IfMatch.Count > 0 && !headers.IfMatch.Any(e => e.Matches(entity.Version))) 
        {
            throw new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfMatch.Count == 0 && headers.IfUnmodifiedSince.HasValue && headers.IfUnmodifiedSince.Value.IsBefore(entity.UpdatedAt))
        {
            throw new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfNoneMatch.Count > 0 && headers.IfNoneMatch.Any(entity => e.Matches(entity.Version)))
        {
            throw isFetch
                ? new HttpException(StatusCodes.Status304NotModified)
                : new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        if (headers.IfNoneMatch.Count == 0 && headers.IfModifiedSince.HasValue && headers.IfModifiedSince.Value.IsAfter(entity.UpdatedAt))
        {
            throw isFetch
                ? new HttpException(StatusCodes.Status304NotModified)
                : new HttpException(StatusCodes.Status412PreconditionFailed) { Payload = entity };
        }

        version = headers.IfMatch.Count > 0 ? headers.IfMatch.Single().ToByteArray() : [];
    }

    /// <summary>
    /// Determines if the client requested that the deleted items in the table should be considered "in view".
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> being processed.</param>
    /// <returns><c>true</c> if deleted items should be considered "in view".</returns>
    internal static bool ShouldIncludeDeletedItems(this HttpRequest request)
    {
        if (request.Query.TryGetValue("__includedeleted", out HeaderStringValues deletedQueryParameter))
        {
            return deletedQueryParameter.Any(x => x!.Equals("true", StringComparison.InvariantCultureIgnoreCase));
        }
        return false;
    }

    /// <summary>
    /// Converts an ETag header value into a byte array.
    /// </summary>
    /// <param name="etag">The Entity Tag header to convert.</param>
    /// <returns>The byte array representing the entity tag.</returns>
    internal static byte[] ToByteArray(this EntityTagHeaderValue etag)
        => etag.Tag == "*" ? [] : Convert.FromBase64String(etag.Tag.ToString().Trim('"'));
}
