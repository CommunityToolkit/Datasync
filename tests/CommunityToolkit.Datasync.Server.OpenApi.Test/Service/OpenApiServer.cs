// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Datasync.Server.OpenApi.Test.Service;

[ExcludeFromCodeCoverage]
internal static class OpenApiServer
{
    internal static TestServer CreateTestServer()
    {
        IWebHostBuilder builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseContentRoot(AppContext.BaseDirectory)
            .UseStartup<ServiceStartup>();
        TestServer server = new(builder);

        using IServiceScope scope = server.Services.CreateScope();
        ServiceDbContext context = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        context.InitializeDatabase();

        return server;
    }
}

[ExcludeFromCodeCoverage]
internal class ServiceStartup
{
    public ServiceStartup(IConfiguration configuration)
    {
        Configuration = configuration;
        DbConnection = new SqliteConnection("Data Source=:memory:");
        DbConnection.Open();
    }

    public IConfiguration Configuration { get; }
    public SqliteConnection DbConnection { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<ServiceDbContext>(
            options => options.UseSqlite(DbConnection).EnableDetailedErrors().EnableSensitiveDataLogging(),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDatasyncServices();
        services.AddControllers();
        services.AddOpenApi(options => options.AddDatasyncTransformers());
    }

    public static void Configure(IApplicationBuilder builder)
    {
        builder.UseRouting();
        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapOpenApi();
        });
    }
}
