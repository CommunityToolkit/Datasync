// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TodoApp.MAUI.Infrastructure.CompiledModels;
using TodoApp.MAUI.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>((options) => { 
    options.UseSqlite("Data Source=TodoApp.db");
#if AoT
    options.UseModel(AppDbContextModel.Instance);
#endif
});
builder.Services.AddTransient<IDbInitializer, DbContextInitializer>();
builder.Services.AddHostedService<DatasyncService>();

using IHost host = builder.Build();

await host.RunAsync();

class DatasyncService : BackgroundService
{
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly AppDbContext dbContext;
    private readonly IDbInitializer dbInitializer;

    public DatasyncService(IHostApplicationLifetime hostApplicationLifetime, AppDbContext dbContext, IDbInitializer dbInitializer)
    {
        this.hostApplicationLifetime = hostApplicationLifetime;
        this.dbContext = dbContext;
        this.dbInitializer = dbInitializer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await dbInitializer.InitializeAsync(stoppingToken);

        var items = await dbContext.TodoItems.ToListAsync();

        foreach(var item in items)
        {
            Console.WriteLine($"{item.Title}\t Completed: {item.IsComplete}");
        }

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();

        hostApplicationLifetime.StopApplication();

    }
}

public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=TodoApp.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
