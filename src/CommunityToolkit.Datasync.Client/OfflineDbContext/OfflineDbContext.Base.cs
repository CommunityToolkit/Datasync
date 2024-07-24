// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Client;

/// <summary>
/// An <see cref="OfflineDbContext"/> instance represents a combination of the Unit of Work
/// and Repository patterns such that it can be used to query a local database, just like a
/// <see cref="DbContext"/> instance.  It is further extended to provide data synchronization
/// capabilities between the on-client database and a remote service.
/// </summary>
/// <remarks>
/// Each <see cref="DbSet{TEntity}"/> should be independent and not contain foreign relationships
/// so that the data synchronization process can push and pull tables independently.
/// </remarks>
public abstract partial class OfflineDbContext : DbContext
{
    /// <summary>
    /// A default <see cref="HttpClient"/> that requires the user to specify absolute URIs when asked.
    /// </summary>
    private readonly Lazy<HttpClient> _defaultHttpClient = new(() => new HttpClient());

    /// <summary>
    /// A default <see cref="JsonSerializerOptions"/> implementation that matches the default service serializer options.
    /// </summary>
    private readonly Lazy<JsonSerializerOptions> _defaultJsonSerializerOptions = new(() => new DatasyncServiceOptions().JsonSerializerOptions);

    /// <summary>
    /// Initializes a new instance of the DbContext class. The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will be called to configure the database(and other options) to be used for this context.
    /// </summary>
    public OfflineDbContext() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DbContext class using the specified options.The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/>
    /// method will still be called to allow further configuration of the options.
    /// </summary>
    /// <param name="options">The options for this context</param>
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Retrieves the absolute or relative URI for the datasync service endpoint handling the entity type.
    /// </summary>
    /// <remarks>
    /// If a relative URI is returned, then it is combined with the <see cref="HttpClient.BaseAddress"/> of the
    /// <see cref="HttpClient"/> returned by <see cref="GetHttpClient"/>.  By default, the URI is constructed
    /// with a format of <c>/tables/{entityType}s</c>.  For example, if the <paramref name="entityType"/> is
    /// <c>Movie</c>, then the URI will be the relative Uri <c>/tables/movies</c>.
    /// </remarks>
    /// <param name="entityType">The entity type to process.</param>
    /// <returns>The absolute or relative URI for the entity type</returns>
    protected virtual Uri GetDatasyncUriForEntityType(Type entityType)
        => new($"/tables/{entityType.Name.ToLowerInvariant()}s", UriKind.Relative);

    /// <summary>
    /// A method used by synchronization methods to get a <see cref="HttpClient"/> that will be used
    /// to communicate with the remote service.
    /// </summary>
    /// <remarks>
    /// The default implementation creates a standard <see cref="HttpClient"/> and uses the same client for
    /// all communications for the lifetime of the context.  It assumes that <see cref="GetDatasyncUriForEntityType(Type)"/>
    /// returns absolute URIs.
    /// </remarks>
    /// <returns>A <see cref="HttpClient"/> that is configured for communicating with the remote service.</returns>
    protected virtual HttpClient GetHttpClient() => this._defaultHttpClient.Value;

    /// <summary>
    /// Retrieves the options used for serializing the deserializing the content going to or coming from a
    /// datasync service.  You should never have to change this.
    /// </summary>
    /// <returns>The <see cref="JsonSerializerOptions"/> to use for JSON serialization and deserialization.</returns>
    protected virtual JsonSerializerOptions GetJsonSerializerOptions() => this._defaultJsonSerializerOptions.Value;
}
