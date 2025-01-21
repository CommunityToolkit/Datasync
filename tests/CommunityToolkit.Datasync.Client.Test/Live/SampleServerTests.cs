// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;

namespace CommunityToolkit.Datasync.Client.Test.Live;

[ExcludeFromCodeCoverage]
public class SampleServerTests
{
    private readonly bool liveTestsAreEnabled = Environment.GetEnvironmentVariable("DATASYNC_SERVICE_ENDPOINT") is not null;
    private readonly string serviceEndpoint = Environment.GetEnvironmentVariable("DATASYNC_SERVICE_ENDPOINT");

    [SkippableFact]
    public async Task Metadata_GetsSetByServer()
    {
        Skip.IfNot(this.liveTestsAreEnabled);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        HttpClient client = new();
        TodoItem source = new() { Title = "Test item" };
        HttpResponseMessage response = await client.PostAsJsonAsync($"{this.serviceEndpoint}/tables/TodoItem", source);

        response.Should().HaveHttpStatusCode(HttpStatusCode.Created);

        TodoItem result = await response.Content.ReadFromJsonAsync<TodoItem>();
        result.Id.Should().NotBeNullOrEmpty();
        result.UpdatedAt.Should().NotBeNull().And.BeAfter(now);
        result.Version.Should().NotBeNullOrEmpty();
        result.Deleted.Should().BeFalse();
        result.Title.Should().Be("Test item");
        result.IsComplete.Should().BeFalse();

        response.Headers.Location.Should().NotBeNull().And.BeEquivalentTo(new Uri($"{this.serviceEndpoint}/tables/TodoItem/{result.Id}"));
        response.Headers.ETag.ToString().Should().Be($"\"{result.Version}\"");
    }

    // This must match the TodoItem class in the server project.
    public class TodoItem
    {
        public string Id { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
        public string Title { get; set; }
        public bool IsComplete { get; set; }
    }
}
