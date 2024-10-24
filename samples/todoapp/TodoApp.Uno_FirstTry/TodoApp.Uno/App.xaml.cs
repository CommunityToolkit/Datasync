using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.Uno.Database;
using TodoApp.Uno.ViewModels;
using TodoApp.Uno.Views;
using Uno.Resizetizer;

namespace TodoApp.Uno;
public partial class App : Application, IDisposable
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    private SqliteConnection dbConnection;

    public IServiceProvider Services { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {

        this.dbConnection = new SqliteConnection("Data Source=:memory:");
        this.dbConnection.Open();

        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // Register Json serializers (ISerializer and ISerializer)
                //.UseSerialization((context, services) => services
                //    .AddContentSerializer(context)
                //    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                //                .UseHttp((context, services) => services
                //                    // Register HttpClient
                //#if DEBUG
                //                    // DelegatingHandler will be automatically injected into Refit Client
                //                    .AddTransient<DelegatingHandler, DebugHttpHandler>()
                //#endif
                //                    .AddSingleton<IWeatherCache, WeatherCache>()
                //                    .AddRefitClient<IApiClient>(context))
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                })
            );

        // Create the IoC Services provider.
        Services = new ServiceCollection()
            .AddTransient<TodoListViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection))
            .BuildServiceProvider();

        // Initialize the database using the registered database initializer.
        InitializeDatabase();

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.EnableHotReload();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(TodoListPage));
        }
        // Ensure the current window is active
        MainWindow.Activate();
    }

    public static TService GetRequiredService<TService>()
        => ((App)App.Current).Services.GetRequiredService<TService>();

    private void InitializeDatabase()
    {
        using IServiceScope scope = Services.CreateScope();
        IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        initializer.Initialize();
    }

    #region IDisposable
    private bool hasDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.hasDisposed)
        {
            if (disposing)
            {
                this.dbConnection.Close();
            }

            this.hasDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
