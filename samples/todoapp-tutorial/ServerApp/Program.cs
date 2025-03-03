using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.InMemory;
using ServerApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRepository<TodoItem>, InMemoryRepository<TodoItem>>();
builder.Services.AddDatasyncServices();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
