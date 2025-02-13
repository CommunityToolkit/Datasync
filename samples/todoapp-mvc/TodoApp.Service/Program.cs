using CommunityToolkit.Datasync.Server;
using Microsoft.EntityFrameworkCore;
using TodoApp.Service.Database;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new ApplicationException("DefaultConnection is not found in the configuration");

builder.Services.AddDbContext<TodoContext>(options => 
{
    options.UseSqlServer(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
        options.EnableThreadSafetyChecks();
    }
});

builder.Services.AddDatasyncServices();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
    await context.InitializeDatabaseAsync();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();