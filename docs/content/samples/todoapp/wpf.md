+++
title = "WPF"
+++

You can find [our sample TodoApp for WPF](https://github.com/CommunityToolkit/Datasync/tree/main/samples/todoapp/TodoApp.WPF) on our GitHub repository.  All of our logic has been placed in the `Database/AppDbContext.cs` file:

{{< highlight lineNos="true" type="csharp" wrap="true" title="AppDbContext.cs" >}}
public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        HttpClientOptions clientOptions = new()
        {
            Endpoint = new Uri("https://YOURSITEHERE.azurewebsites.net/"),
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
{{< /highlight >}}

To enable offline synchronization:

* Switch from `DbContext` to `OfflineDbContext`.
* Define your `OnDatasyncInitialization()` method (don't forget to change the URL to the URL of your datasync server).
* Where appropriate, use `PushAsync()` and `PullAsync()` to communicate with the server.

We have placed a `SynchronizeAsync()` method on the database context, which is used in the view model for the single page we have.
