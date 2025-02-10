// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.Abstractions.Json;
using CommunityToolkit.Datasync.Server.CosmosDb;
using CommunityToolkit.Datasync.Server.Swashbuckle;
using Microsoft.Azure.Cosmos;
using Sample.Datasync.Server.SingleContainer.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new ApplicationException("DefaultConnection is not set");

CosmosClient cosmosClient = new CosmosClient(connectionString,
    new CosmosClientOptions()
    {
        UseSystemTextJsonSerializerWithOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new DateTimeOffsetConverter(),
                new DateTimeConverter(),
                new TimeOnlyConverter(),
                new SpatialGeoJsonConverter()
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.Preserve
        }
    });

builder.Services.AddSingleton(cosmosClient);
builder.Services.AddSingleton<ICosmosTableOptions<TodoItem>>(new CosmosSharedTableOptions<TodoItem>("TodoDb", "TodoContainer"));
builder.Services.AddSingleton<ICosmosTableOptions<TodoList>>(new CosmosSharedTableOptions<TodoList>("TodoDb", "TodoContainer"));
builder.Services.AddSingleton(typeof(IRepository<>), typeof(CosmosTableRepository<>));
// Add services to the container.

builder.Services.AddDatasyncServices();

builder.Services.AddControllers();

_ = builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options => options.AddDatasyncControllers());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger().UseSwaggerUI();
    _ = app.UseDeveloperExceptionPage();

    Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("TodoDb");

    _ = await database.CreateContainerIfNotExistsAsync(new ContainerProperties("TodoContainer", "/entity")
    {
        IndexingPolicy = new()
        {
            CompositeIndexes =
            {
                new Collection<CompositePath>()
                {
                    new CompositePath() { Path = "/updatedAt", Order = CompositePathSortOrder.Ascending },
                    new CompositePath() { Path = "/id", Order = CompositePathSortOrder.Ascending }
                }
            }
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
