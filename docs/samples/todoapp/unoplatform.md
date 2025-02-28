# TodoApp client for Uno Platform

!!! info
    The Uno Platform sample has been kindly contributed to the community by [@trydalch](https://github.com/trydalch).

## Run the application first

The Uno Platform sample uses an in-memory Sqlite store for storing its data.  To run the application locally:

* [Configure Visual Studio for Uno Platform development](https://platform.uno/visual-studio/).
* Open `samples/todoapp/TodoApp.Uno/TodoApp.Uno.sln` in Visual Studio.
* Select a target (in the top bar), then press F5 to run the application.
    * For Windows, select **TodoApp.Uno (WinAppSDK Packaged)**.
    * For other platforms, select the correct emulator or simulator from the target selection drop-down.

If you bump into issues at this point, ensure you can properly develop and run Uno Platform applications for the desktop outside of the datasync service.

!!! info Tested platforms
    The TodoApp.Uno sample is known to work on Android and Desktop.  We have not tested on other platforms.

## Deploy a datasync server to Azure

Before you begin adjusting the application for offline usage, you must [deploy a datasync service](./server.md).  Make a note of the URI of the service before continuing.

## Update the application for datasync operations

All the changes are isolated to the `Database/AppDbContext.cs` file in the `TodoApp.Uno` project.

1. Change the definition of the class so that it inherits from `OfflineDbContext`:

        public class AppDbContext(DbContextOptions<AppDbContext> options) : OfflineDbContext(options)
        {
          // Rest of the class
        }

2. Add the `OnDatasyncInitialization()` method:

        protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
        {
            HttpClientOptions clientOptions = new()
            {
                Endpoint = new Uri("https://YOURSITEHERE.azurewebsites.net/"),
                HttpPipeline = [new LoggingHandler()]
            };
            _ = optionsBuilder.UseHttpClientOptions(clientOptions);
        }

   Replace the Endpoint with the URI of your datasync service.

3. Update the `SynchronizeAsync()` method.

   The `SynchronizeAsync()` method is used by the application to synchronize data to and from the datasync service.  It is called primarily from the `MainViewModel` which drives the UI interactions for the main list.

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

You can now re-run your application. Watch the console logs to show the interactions with the datasync service.  Press the refresh button to synchronize data with the cloud.  When you restart the application, your changes will automatically populate the database again.

!!! tip
    The first synchronization can take a while because of the cold-start of the service.  Watch the debug output to see the synchronization happening.

Obviously, you will want to do much more in a "real world" application, including proper error handling, authentication, and using a Sqlite file instead of an in-memory database.  This example shows off the minimum required to add datasync services to an application.
