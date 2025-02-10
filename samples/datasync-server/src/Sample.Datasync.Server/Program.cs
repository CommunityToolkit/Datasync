// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.NSwag;
using CommunityToolkit.Datasync.Server.Swashbuckle;
using Microsoft.EntityFrameworkCore;
using Sample.Datasync.Server.Db;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new ApplicationException("DefaultConnection is not set");

string? swaggerDriver = builder.Configuration["Swagger:Driver"];
bool nswagEnabled = swaggerDriver?.Equals("NSwag", StringComparison.InvariantCultureIgnoreCase) == true;
bool swashbuckleEnabled = swaggerDriver?.Equals("Swashbuckle", StringComparison.InvariantCultureIgnoreCase) == true;
bool openApiEnabled = swaggerDriver?.Equals("NET9", StringComparison.InvariantCultureIgnoreCase) == true;

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatasyncServices();
builder.Services.AddControllers();

if (nswagEnabled)
{
    _ = builder.Services.AddOpenApiDocument(options => options.AddDatasyncProcessor());
}

if (swashbuckleEnabled)
{
    _ = builder.Services.AddEndpointsApiExplorer();
    _ = builder.Services.AddSwaggerGen(options => options.AddDatasyncControllers());
}

if (openApiEnabled)
{
    _ = builder.Services.AddOpenApi();
}

WebApplication app = builder.Build();

// Initialize the database
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.InitializeDatabaseAsync();
}

app.UseHttpsRedirection();

if (nswagEnabled)
{
    _ = app.UseOpenApi().UseSwaggerUI();
}

if (swashbuckleEnabled)
{
    _ = app.UseSwagger().UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

if (openApiEnabled)
{
    _ = app.MapOpenApi(pattern: "swagger/{documentName}/swagger.json");
}

app.Run();
