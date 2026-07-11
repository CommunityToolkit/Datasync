using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.Uno.Database;
using TodoApp.Uno.ViewModels;
using TodoApp.Uno.Views;
using Uno.Resizetizer;

namespace TodoApp.Uno;
public partial class App : Application
{
    protected Window? MainWindow { get; private set; }
    public IHost? Host { get; private set; }
    private Lazy<SqliteConnection> dbConnection = new(() => CreateSqliteConnection());

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    private SqliteConnection DbConnection { get => dbConnection.Value; }

    private static SqliteConnection CreateSqliteConnection()
    {
        SqliteConnection conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        return conn;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppContext.SetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", true);
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
                    services.AddDbContext<AppDbContext>(options => options.UseSqlite(DbConnection));
                    services.AddScoped<IDbInitializer, DbContextInitializer>();
                    services.AddTransient<TodoListViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        InitializeDatabase();

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
