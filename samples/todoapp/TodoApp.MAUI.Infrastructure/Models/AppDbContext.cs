// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Infrastructure.Services;

namespace TodoApp.MAUI.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        HttpClientOptions clientOptions = new()
        {
            Endpoint = new Uri("https://********-****.****.devtunnels.ms/"),
            HttpPipeline = [new LoggingHandler()]
        };
        _ = optionsBuilder.UseHttpClientOptions(clientOptions);
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
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
    }
}

/// <summary>
/// Use this class to initialize the database.  In this sample, we just create
/// the database. However, you may want to use migrations.
/// </summary>
/// <param name="context">The context for the database.</param>
public class DbContextInitializer(AppDbContext context) : IDbInitializer
{
    /// <inheritdoc />
    public void Initialize()
    {
        //_ = context.Database.EnsureCreated();
        context.Database.Migrate();
        Task.Run(async () => await context.SynchronizeAsync());
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        //await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.SynchronizeAsync(cancellationToken);

    }
}