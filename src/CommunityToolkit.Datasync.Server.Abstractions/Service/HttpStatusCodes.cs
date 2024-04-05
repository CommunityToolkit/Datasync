// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A set of symbolic names for HTTP status codes.
/// </summary>
/// <remarks>
/// We use this to avoid a dependency on Microsoft.AspNetCore.Http.
/// </remarks>
public static class HttpStatusCodes
{
    /// <summary>
    /// A symbolic name for the HTTP Status Code 400 Bad Request.
    /// </summary>
    public const int Status40BadRequest = 400;

    /// <summary>
    /// A symbolic name for the HTTP Status Code 404 Not Found.
    /// </summary>
    public const int Status404NotFound = 404;

    /// <summary>
    /// A symbolic name for the HTTP Status Code 409 Conflict.
    /// </summary>
    public const int Status409Conflict = 409;

    /// <summary>
    /// A symbolic name for the HTTP Status Code 412 Precondition Failed.
    /// </summary>
    public const int Status412PreconditionFailed = 412;
}
