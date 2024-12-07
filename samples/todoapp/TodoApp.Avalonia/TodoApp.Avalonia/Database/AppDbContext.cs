// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TodoApp.Avalonia.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    //protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    //{
    //    HttpClientOptions clientOptions = new()
    //    {
    //        Endpoint = new Uri("https://YOURSITEHERE.azurewebsites.net/"),
    //        HttpPipeline = [new LoggingHandler()]
    //    };
    //    _ = optionsBuilder.UseHttpClientOptions(clientOptions);
    //}

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        // PushResult pushResult = await this.PushAsync(cancellationToken);
        // if (!pushResult.IsSuccessful)
        // {
        //     throw new ApplicationException($"Push failed: {pushResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        // }
        //
        // PullResult pullResult = await this.PullAsync(cancellationToken);
        // if (!pullResult.IsSuccessful)
        // {
        //     throw new ApplicationException($"Pull failed: {pullResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        // }
    }

    /// <summary>
    /// Adds some sample data to the database
    /// </summary>
    internal async Task AddSampleDataAsync(CancellationToken cancellationToken = default)
    {
        await TodoItems.AddRangeAsync(
            new TodoItem() { Id = Guid.NewGuid().ToString("N"), Content = """Say "Hello" to DataSync and Avalonia""" , IsComplete = true }, 
            new TodoItem() { Id = Guid.NewGuid().ToString("N"), Content = "Learn DataSync", IsComplete = false }, 
            new TodoItem() { Id = Guid.NewGuid().ToString("N"), Content = "Learn Avalonia", IsComplete = false });
        
        await SaveChangesAsync(cancellationToken);
    }
}