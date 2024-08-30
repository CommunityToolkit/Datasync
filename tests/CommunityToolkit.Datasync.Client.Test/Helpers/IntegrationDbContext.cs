// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Helpers;

[ExcludeFromCodeCoverage]
public class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : OfflineDbContext(options)
{
    public DbSet<ClientMovie> Movies => Set<ClientMovie>();

    public ServiceApplicationFactory Factory { get; set; }

    public SqliteConnection Connection { get; set; }

    public string Filename { get; set; }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseHttpClient(Factory.CreateClient());
        optionsBuilder.Entity<ClientMovie>(cfg =>
        {
            cfg.ClientName = "movies";
            cfg.Endpoint = new Uri($"/{Factory.MovieEndpoint}", UriKind.Relative);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Close();
        }

        base.Dispose(disposing);
    }
}
