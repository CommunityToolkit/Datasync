// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.TestCommon.Models;

namespace CommunityToolkit.Datasync.TestService.AccessControlProviders;

[ExcludeFromCodeCoverage]
public class MovieAccessControlProvider<T> : AccessControlProvider<T> where T : class, IMovie, ITableData
{
    /// <summary>
    /// The last entity that was authorized.
    /// </summary>
    public object LastEntity { get; private set; } = null;

    /// <summary>
    /// Determines if the entity can be authorized.
    /// </summary>
    public bool CanBeAuthorized { get; set; } = true;

    /// <inheritdoc />
    public override ValueTask<bool> IsAuthorizedAsync(TableOperation operation, T entity, CancellationToken cancellationToken = default)
    {
        LastEntity = entity;
        return ValueTask.FromResult(CanBeAuthorized);
    }
}
