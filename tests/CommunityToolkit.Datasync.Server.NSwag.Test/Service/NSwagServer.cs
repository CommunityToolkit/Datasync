// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Datasync.Server.NSwag.Test.Service;

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
        services.AddOpenApiDocument(options => options.AddDatasyncProcessor());
    }

    public static void Configure(IApplicationBuilder builder)
    {
        builder.UseOpenApi();
        builder.UseRouting();
        builder.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
