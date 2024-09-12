// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        //PushResult pushResult = await this.PushAsync(cancellationToken);
        //if (!pushResult.IsSuccessful)
        //{
        //    throw new ApplicationException($"Push failed: {pushResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        //}

        //PullResult pullResult = await this.PullAsync(cancellationToken);
        //if (!pullResult.IsSuccessful)
        //{
        //    throw new ApplicationException($"Pull failed: {pullResult.FailedRequests.FirstOrDefault().Value.ReasonPhrase}");
        //}
    }
}