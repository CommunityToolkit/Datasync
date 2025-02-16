using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
