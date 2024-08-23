// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline.Models;

namespace CommunityToolkit.Datasync.Client.Offline.Operations;

/// <summary>
/// The interface describing how to pull elements from the remote service
/// </summary>
internal interface IPullOperationManager
{
    /// <summary>
    /// Executes a set of pull requests.
    /// </summary>
    /// <param name="pullRequests">The list of pull requests to execute..</param>
    /// <param name="pullOptions">The pull options to use.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The results of the pull operation.</returns>
    Task<PullResult> ExecuteAsync(IEnumerable<PullRequest> pullRequests, PullOptions pullOptions, CancellationToken cancellationToken);

}
