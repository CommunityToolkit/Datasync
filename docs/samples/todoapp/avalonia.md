# TodoApp client for Avalonia

!!! info
    The Avalonia sample has been kindly contributed to the community by [@timunie](https://github.com/timunie).

## Run the application first

The Avalonia sample uses an in-memory Sqlite store for storing its data.  To run the application locally:

* [Configure Visual Studio for Avalonia development](https://docs.avaloniaui.net/docs/get-started).
* Open `samples/todoapp/Samples.TodoApp.sln` in Visual Studio.
* In the Solution Explorer, expand the `TodoApp.Avalonia` folder, then right-click the `TodoApp.Avalonia.Desktop` project, then select **Set as Startup Project**.
* Select a target (in the top bar), then press F5 to run the application.

If you bump into issues at this point, ensure you can properly develop and run Avalonia applications for the desktop outside of the datasync service.

!!! info Tested platforms
    The TodoApp.Avalonia sample is known to work on Android and Desktop.  We have not tested on other platforms.

### Minimum OS versions and tooling

The mobile heads set the minimum OS version they run on (`SupportedOSPlatformVersion`) in their project files:

| Platform | Minimum version | Where it is set |
|----------|-----------------|-----------------|
| iOS | 18.0 | `TodoApp.Avalonia.iOS.csproj` and `MinimumOSVersion` in `Info.plist` (keep the two in step) |
| Android | 7.0 (API 24) | `TodoApp.Avalonia.Android.csproj` |

These match the MAUI and Uno samples.  Avalonia 12 tests iOS 26 (Tier 1) and iOS 18.x (Tier 2) and treats iOS 17 and earlier as Tier 3.  See the [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms) page for current details.

The minimum OS version is separate from the SDK you build with.  To build the mobile heads you need:

* **iOS**: a Mac with Xcode 26.6 and the `ios` workload (the iOS SDK 26.5 workload set, 10.0.112).  This is the combination used by the sample build in CI.
* **Android**: the `android` workload, JDK 21, and the Android SDK (installed with Android Studio or Visual Studio) including the Android 16 (API 36) SDK platform, which is the default target of `net10.0-android`.

## Deploy a datasync server to Azure

Before you begin adjusting the application for offline usage, you must [deploy a datasync service](./server.md).  Make a note of the URI of the service before continuing.

## Update the application for datasync operations

All the changes are isolated to the `Database/AppDbContext.cs` file in the `TodoApp.Avalonia` shared project.

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
