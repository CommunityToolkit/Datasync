// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using Microsoft.EntityFrameworkCore;
using Sample.Datasync.Server.Db;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new ApplicationException("DefaultConnection is not set");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatasyncServices();
builder.Services.AddControllers();

WebApplication app = builder.Build();

// Initialize the database
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.InitializeDatabaseAsync();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
