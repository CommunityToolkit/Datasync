// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.OpenApiService.Extensions;
using CommunityToolkit.Datasync.OpenApiService.Models;
using CommunityToolkit.Datasync.Server;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

DbConnection DbConnection = SqliteFactory.CreateAndOpenConnection();
builder.Services.AddDbContext<ServiceDbContext>(
    options => options.UseSqlite(DbConnection).EnableDetailedErrors().EnableSensitiveDataLogging(),
    contextLifetime: ServiceLifetime.Transient,
    optionsLifetime: ServiceLifetime.Singleton);

builder.Services.AddControllers();
builder.Services.AddDatasyncServices();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ServiceDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
    context.InitializeDatabase();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi(pattern: "/openapi/{documentName}.json");

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;
