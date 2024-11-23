using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TodoApp.Uno.Database;
using TodoApp.Uno.ViewModels;
using TodoApp.Uno.Views;
using Uno.Resizetizer;

namespace TodoApp.Uno;
public partial class App : Application
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
    public IHost? Host { get; private set; }

    private SqliteConnection dbConnection;

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);

        this.dbConnection = new SqliteConnection("Data Source=:memory:");
        this.dbConnection.Open();

        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
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

                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                    services.AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection));
                    services.AddScoped<IDbInitializer, DbContextInitializer>();
                    services.AddTransient<TodoListViewModel>();

                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.EnableHotReload();
#endif
        MainWindow.SetWindowIcon();

        // We have to build here so that the services are initialized and we can call InitializeDatabase
        // before navigating. This enables us to load the data when the page is loaded
        Host = builder.Build();

        InitializeDatabase();

        // We still make this navigation call so we use the Uno Navigation extension, which automatically wires 
        // up view models with their page using the ViewMap and RouteMap below. 
        Host = await builder.NavigateAsync<Shell>();

    }

    private void InitializeDatabase()
    {
        IDbInitializer? initializer = Host?.Services?.GetRequiredService<IDbInitializer>();
        initializer?.Initialize();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<TodoListPage, TodoListViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("TodoList", View: views.FindByViewModel<TodoListViewModel>(), IsDefault: true)
                ]
            )
        );
    }
}
