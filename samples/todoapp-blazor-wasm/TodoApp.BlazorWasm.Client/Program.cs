// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TodoApp.BlazorWasm.Client;
using TodoApp.BlazorWasm.Client.Services;
using TodoApp.BlazorWasm.Shared.Models;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for Datasync
builder.Services.AddHttpClient("DatasyncClient", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Register Datasync services
builder.Services.AddScoped<DatasyncServiceClient<TodoItemDto>>(sp =>
{
    IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    HttpClient httpClient = httpClientFactory.CreateClient("DatasyncClient");
    Uri tableEndpoint = new("/tables/todoitems", UriKind.Relative);
    return new DatasyncServiceClient<TodoItemDto>(tableEndpoint, httpClient);
});

builder.Services.AddScoped<ITodoService, TodoService>();

await builder.Build().RunAsync();
