// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using CommunityToolkit.Datasync.Server.InMemory;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestService.AccessControlProviders;
using Microsoft.OData.ModelBuilder;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

InMemoryRepository<InMemoryMovie> inMemoryMovieRepository = new(TestData.Movies.OfType<InMemoryMovie>());
builder.Services.AddSingleton<IRepository<InMemoryMovie>>(inMemoryMovieRepository);

InMemoryRepository<InMemoryKitchenSink> inMemoryKitchenSinkRepository = new();
builder.Services.AddSingleton<IRepository<InMemoryKitchenSink>>(inMemoryKitchenSinkRepository);

ODataConventionModelBuilder modelBuilder = new();
modelBuilder.EnableLowerCamelCase();
modelBuilder.AddEntityType(typeof(InMemoryMovie));
modelBuilder.AddEntityType(typeof(InMemoryKitchenSink));
builder.Services.AddDatasyncServices(modelBuilder.GetEdmModel());

builder.Services.AddSingleton<IAccessControlProvider<InMemoryMovie>>(new MovieAccessControlProvider<InMemoryMovie>());

builder.Services.AddOpenApi();

builder.Services.AddControllers();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;