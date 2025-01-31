// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// A list of options for configuring the <see cref="TableController{TEntity}"/>.
/// </summary>
public class TableControllerOptions
{
    private int _pageSize = 100;
    private int _maxTop = MAX_TOP;

    /// <summary>
    /// The maximum page size that can be specified by the server.
    /// </summary>
    public const int MAX_PAGESIZE = 128000;

    /// <summary>
    /// The maximum value of $top that the client can request.
    /// </summary>
    public const int MAX_TOP = 128000;

    /// <summary>
    /// If <c>true</c>, then client-side evaluation of queries is disabled and clients
    /// will get a 500 Internal Server Error if they attempt to use a query that cannot
    /// be evaluated by the database.
    /// </summary>
    /// <remarks>
    /// This option is no longer used (since v9.0.0)
    /// </remarks>
    [Obsolete("Client-side evaluation is no longer supported.  This option will be removed in a future release.")]
    public bool DisableClientSideEvaluation { get; set; }

    /// <summary>
    /// If <c>true</c>, then items are marked as deleted instead of being removed from the database.
    /// By default, soft delete is turned off.
    /// </summary>
    public bool EnableSoftDelete { get; set; }

    /// <summary>
    /// The maximum page size for the results returned by a query operation.  This is the
    /// maximum value that the client can specify for the <c>$top</c> query option.
    /// </summary>
    public int MaxTop
    {
        get => this._maxTop;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(MaxTop));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MAX_TOP, nameof(MaxTop));
            this._maxTop = value;
        }
    }

    /// <summary>
    /// The default page size for the results returned by a query operation.
    /// </summary>
    public int PageSize
    {
        get => this._pageSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(PageSize));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MAX_PAGESIZE, nameof(PageSize));
            this._pageSize = value;
        }
    }

    /// <summary>
    /// The status code returned when the user is not authorized to perform an operation.
    /// </summary>
    public int UnauthorizedStatusCode { get; set; } = StatusCodes.Status401Unauthorized;
}
