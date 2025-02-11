// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#undef OFFLINE_SYNC_ENABLED

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using TodoApp.WPF.Services;

namespace TodoApp.WPF.Database;

#if OFFLINE_SYNC_ENABLED
public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
#else
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
#endif
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

#if OFFLINE_SYNC_ENABLED
    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        HttpClientOptions clientOptions = new()
        {
            Endpoint = new Uri("https://YOUR_SITE_HERE.azurewebsites.net/"),
            HttpPipeline = [new LoggingHandler()]
        };
        _ = optionsBuilder.UseHttpClientOptions(clientOptions);
    }
#endif

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
#if OFFLINE_SYNC_ENABLED
        PushResult pushResult = await this.PushAsync(cancellationToken);
        if (!pushResult.IsSuccessful)
        {
            throw new ApplicationException($"Push failed: {pushResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        }

        PullResult pullResult = await this.PullAsync(cancellationToken);
        if (!pullResult.IsSuccessful)
        {
            throw new ApplicationException($"Pull failed: {pullResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        }
#endif
    }
}

/// <summary>
/// Use this class to initialize the database.  In this sample, we just create
/// the database using <see cref="DatabaseFacade.EnsureCreated"/>.  However, you
/// may want to use migrations.
/// </summary>
/// <param name="context">The context for the database.</param>
public class DbContextInitializer(AppDbContext context) : IDbInitializer
{
    /// <inheritdoc />
    public void Initialize()
    {
        _ = context.Database.EnsureCreated();
        // Task.Run(async () => await context.SynchronizeAsync());
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => context.Database.EnsureCreatedAsync(cancellationToken);
}
