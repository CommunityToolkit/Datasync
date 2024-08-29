// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TodoApp.MAUI.Models;

/// <summary>
/// An interface to initialize a database.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Synchronously initialize the database.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Asynchronously initialize the database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when complete.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
