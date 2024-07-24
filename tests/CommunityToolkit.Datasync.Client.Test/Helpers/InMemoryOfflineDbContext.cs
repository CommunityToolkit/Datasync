// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Models;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Mocks;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

/// <summary>
/// Implements a basic <see cref="OfflineDbContext"/> that can be used
/// for test purposes.  It uses an in-memory store for data storage so
/// that all the tests can get access to relevant data.
/// </summary>
[ExcludeFromCodeCoverage]
public class InMemoryOfflineDbContext(DbContextOptions<InMemoryOfflineDbContext> options) : OfflineDbContext(options)
{
    private readonly MockDelegatingHandler _handler = new();

    public DbSet<ClientMovie> Movies => Set<ClientMovie>();

    // Expose the Datssync sets with different names
    public DbSet<SynchronizationSetMetadata> DatasyncMetadata => base.SynchronizationSetMetadata;

    // Expose the mock handler for our GetHttpClient()
    public MockDelegatingHandler HttpHandler { get => this._handler; }

    /// <summary>
    /// Selects a HttpClient for using to communicate with the remote service.
    /// </summary>
    /// <remarks>Returns a mocked service.</remarks>
    protected override HttpClient GetHttpClient()
        => new(this._handler) { BaseAddress = new Uri("http://localhost/") };

    /// <summary>
    /// Sets the relative URI to the endpoint for a specific type.
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    protected override Uri GetDatasyncUriForEntityType(Type entityType)
    {
        string entityName = entityType.Name.ToLowerInvariant();
        if (entityName.StartsWith("client"))
        {
            entityName = entityName[6..];
        }

        return new Uri($"/tables/{entityName}", UriKind.Relative);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._handler.Dispose();
            base.Dispose();
        }
    }

    public override void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
