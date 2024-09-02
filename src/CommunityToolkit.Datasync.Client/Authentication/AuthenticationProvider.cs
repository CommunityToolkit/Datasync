// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Datasync.Client.Authentication;

/// <summary>
/// Definition of an authentication provider, which is a specific type of delegating
/// handler that handles authentication updates.
/// </summary>
public abstract class AuthenticationProvider : DelegatingHandler
{
    /// <summary>
    /// The display name for the currently logged in user.  This may be null.
    /// </summary>
    public string? DisplayName { get; protected set; }

    /// <summary>
    /// If true, the user is logged in (and the UserId is available).
    /// </summary>
    public bool IsLoggedIn { get; protected set; }

    /// <summary>
    /// The User ID for this user.
    /// </summary>
    public string? UserId { get; protected set; }

    /// <summary>
    /// Initiate a login request out of band of the pipeline.  This can be used to
    /// initiate the login process via a button.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An async task that resolves when the login is complete.</returns>
    public abstract Task LoginAsync(CancellationToken cancellationToken = default);
}
