// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
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
        set { Ensure.That(value, nameof(MaxTop)).IsInRange(1, MAX_TOP); this._maxTop = value; }
    }

    /// <summary>
    /// The default page size for the results returned by a query operation.
    /// </summary>
    public int PageSize
    {
        get => this._pageSize;
        set { Ensure.That(value, nameof(MaxTop)).IsInRange(1, MAX_PAGESIZE); this._pageSize = value; }
    }

    /// <summary>
    /// The status code returned when the user is not authorized to perform an operation.
    /// </summary>
    public int UnauthorizedStatusCode { get; set; } = StatusCodes.Status401Unauthorized;
}
