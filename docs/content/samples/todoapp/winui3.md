+++
title = "WinUI3"
+++

## Run the application first

The WinUI3 sample uses an in-memory Sqlite store for storing its data.  To run the application locally:

* [Configure Visual Studio for WinUI3 development](https://learn.microsoft.com/windows/apps/get-started/start-here).
* Open `samples/todoapp/Samples.TodoApp.sln` in Visual Studio.
* In the Solution Explorer, right-click the `TodoApp.WinUI3` project, then select **Set as Startup Project**.
* Select a target (in the top bar), then press F5 to run the application.

If you bump into issues at this point, ensure you can properly develop and run WinUI3 applications outside of the datasync service.

## Deploy a datasync server to Azure

Before you begin adjusting the application for offline usage, you must [deploy a datasync service](../server.md).  Make a note of the URI of the service before continuing.

## Update the application for datasync operations

All the changes are isolated to the `Database/AppDbContext.cs` file.

1. Change the definition of the class so that it inherits from `OfflineDbContext`:

   ```csharp
   public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
   {
     // Rest of the class
   }
   ```

2. Add the `OnDatasyncInitialization()` method:

   ```csharp
   protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
   {
       HttpClientOptions clientOptions = new()
       {
           Endpoint = new Uri("https://YOURSITEHERE.azurewebsites.net/"),
           HttpPipeline = [new LoggingHandler()]
       };
       _ = optionsBuilder.UseHttpClientOptions(clientOptions);
   }
   ```

   Replace the Endpoint with the URI of your datasync service.

3. Update the `SynchronizeAsync()` method.

   The `SynchronizeAsync()` method is used by the application to synchronize data to and from the datasync service.  It is called primarily from the `MainViewModel` which drives the UI interactions for the main list.

   ```csharp
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
    ```

You can now re-run your application. Watch the console logs to show the interactions with the datasync service.  Press the refresh button to synchronize data with the cloud.  When you restart the application, your changes will automatically populate the database again.

Obviously, you will want to do much more in a "real world" application, including proper error handling, authentication, and using a Sqlite file instead of an in-memory database.  This example shows off the minimum required to add datasync services to an application.
