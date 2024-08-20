// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_InternalApi_Tests : BaseTest
{
    #region GetEntityMap
    [Fact]
    public void GetEntityMap_Works()
    {
        TestDbContext context = CreateContext();

        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };

        context._internalApi.GetEntityMap(context).Should().NotBeNullOrEmpty().And.BeEquivalentTo(expected);
    }
    #endregion

    #region GetExistingOperationAsync
    [Fact]
    public async Task GetExistingOperationAsync_InvalidId_Throws()
    {
        TestDbContext context = CreateContext();
        ClientMovie movie = new() { Id = "###" };
        Func<Task> act = async () => _ = await context._internalApi.GetExistingOperationAsync(movie);
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    #region GetSynchronizableEntityTypes
    [Fact]
    public void GetSynchronizableEntityTypes_NoArg()
    {
        TestDbContext context = CreateContext();
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];

        context._internalApi.GetSynchronizableEntityTypes().Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Empty()
    {
        TestDbContext context = CreateContext();
        List<Type> allowedTypes = [];
        List<Type> expectedTypes = [];

        context._internalApi.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_None()
    {
        TestDbContext context = CreateContext();
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity4)];
        List<Type> expectedTypes = [];

        context._internalApi.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Some()
    {
        TestDbContext context = CreateContext();
        List<Type> allowedTypes = [typeof(Entity1), typeof(ClientMovie), typeof(Entity4)];
        List<Type> expectedTypes = [typeof(ClientMovie)];

        context._internalApi.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_All()
    {
        TestDbContext context = CreateContext();
        List<Type> allowedTypes = [typeof(Entity3), typeof(ClientMovie)];
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];

        context._internalApi.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }
    #endregion

    #region PullAsync
    [Fact]
    public async Task PullAsync_BadPullOptions()
    {
        TestDbContext context = CreateContext();
        PullOptions options = new() { ParallelOperations = 0 };
        Type[] entityTypes = [typeof(ClientMovie)];

        Func<Task> act = async () => await context._internalApi.PullAsync(entityTypes, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PullAsync_NoSynchronizableEntities(int nItems)
    {
        TestDbContext context = CreateContext();
        PullOptions options = new();
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity2), typeof(Entity4)];
        Type[] entityTypes = allowedTypes.Take(nItems).ToArray();

        Func<Task> act = async () => await context._internalApi.PullAsync(entityTypes, options);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PullAsync_PendingOperations()
    {
        TestDbContext context = CreateContext();
        PullOptions options = new();
        Type[] entityTypes = [typeof(ClientMovie)];
        string itemJson = """{"id":"123"}""";
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Add,
            State = OperationState.Pending,
            EntityType = typeof(ClientMovie).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = itemJson,
            Sequence = 0,
            Version = 0
        };
        context.DatasyncOperationsQueue.Add(op);
        context.SaveChanges();

        Func<Task> act = async () => await context._internalApi.PullAsync(entityTypes, options);
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    #region PushAsync
    [Fact]
    public async Task PushAsync_BadPushOptions()
    {
        TestDbContext context = CreateContext();
        PushOptions options = new() { ParallelOperations = 0 };
        Type[] entityTypes = [typeof(ClientMovie)];

        Func<Task> act = async () => _ = await context.PushAsync(entityTypes, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PushAsync_NoSynchronizableEntities(int nItems)
    {
        TestDbContext context = CreateContext();
        PushOptions options = new();
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity2), typeof(Entity4)];
        Type[] entityTypes = allowedTypes.Take(nItems).ToArray();

        PushOperationResult result = await context.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedOperations.Count.Should().Be(0);
    }

    [Fact]
    public async Task PushAsync_NoOperations()
    {
        TestDbContext context = CreateContext();
        PushOptions options = new();
        Type[] entityTypes = [typeof(ClientMovie)];

        PushOperationResult result = await context.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedOperations.Count.Should().Be(0);
    }

    [Fact]
    public async void PushAsync_Addition_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        context.DatasyncOperationsQueue.Should().BeEmpty();

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);
    }

    [Fact]
    public async Task PushAsync_Addition_HttpError()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }

    [Fact]
    public async Task PushAsync_Addition_Conflict()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }

    [Fact]
    public async Task PushAsync_Removal_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Handler.AddResponse(HttpStatusCode.NoContent);

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        context.DatasyncOperationsQueue.Should().BeEmpty();
        context.Movies.Find(clientMovie.Id).Should().BeNull();
    }

    [Fact]
    public async Task PushAsync_Removal_HttpError()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }

    [Fact]
    public async Task PushAsync_Removal_Conflict()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }

    [Fact]
    public async Task PushAsync_Replacement_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        context.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_Replacement_HttpError()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }

    [Fact]
    public async Task PushAsync_Replacment_Conflict()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.PushAsync();
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        context.DatasyncOperationsQueue.Should().HaveCount(1);
    }
    #endregion

    #region PushOperationAsync
    [Fact]
    public async void PushOperationAsync_Addition_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().BeNull();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/");
        (await request.Content.ReadAsStringAsync()).Should().Be(clientMovieJson);

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().BeNull();
    }

    [Fact]
    public async Task PushOperationAsync_Addition_HttpError()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(500);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_Addition_Conflict()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.ToString().Should().Be("https://test.zumo.net/tables/movies/");
        (await request.Content.ReadAsStringAsync()).Should().Be(clientMovieJson);

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.Should().BeEquivalentTo(clientMovie);

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(409);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_Removal_Works()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        context.Handler.AddResponse(HttpStatusCode.NoContent);

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().BeNull();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().NotHaveHeader("If-Match");

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().BeNull();
    }

    [Fact]
    public async Task PushOperationAsync_Removal_Works_WithVersion()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N"), Version = "1234" };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        context.Handler.AddResponse(HttpStatusCode.NoContent);

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().BeNull();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().HaveHeader("If-Match", "\"1234\"");

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().BeNull();
    }

    [Fact]
    public async Task PushOperationAsync_Removal_HttpError()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().NotHaveHeader("If-Match");

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(500);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_Removal_Conflict()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });
        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);
        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().NotHaveHeader("If-Match");

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(409);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_Replacement_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().BeNull();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().NotHaveHeader("If-Match");
        (await request.Content.ReadAsStringAsync()).Should().Be(clientMovieJson);

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().BeNull();
    }

    [Fact]
    public async Task PushOperationAsync_Replacement_Works_WithVersion()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N"), Version = "1234" };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().BeNull();

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        request.Should().HaveHeader("If-Match", "\"1234\"");
        (await request.Content.ReadAsStringAsync()).Should().Be(clientMovieJson);

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().BeNull();
    }

    [Fact]
    public async Task PushOperationAsync_Replacement_HttpError()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(500);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_Replacment_Conflict()
    {
        DateTimeOffset StartTime = DateTimeOffset.UtcNow;
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        context.Update(clientMovie);
        context.SaveChanges();
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);

        DatasyncOperation op = context.DatasyncOperationsQueue.Single(x => x.ItemId == clientMovie.Id);

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        ServiceResponse result = await context._internalApi.PushOperationAsync(op);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        HttpRequestMessage request = context.Handler.Requests.SingleOrDefault();
        request.Should().NotBeNull();
        request.Method.Should().Be(HttpMethod.Put);
        request.RequestUri.ToString().Should().Be($"https://test.zumo.net/tables/movies/{clientMovie.Id}");
        (await request.Content.ReadAsStringAsync()).Should().Be(clientMovieJson);

        ClientMovie actualMovie = context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.Should().BeEquivalentTo(clientMovie);

        DatasyncOperation afterOp = context.DatasyncOperationsQueue.SingleOrDefault(x => x.ItemId == clientMovie.Id);
        afterOp.Should().NotBeNull();
        afterOp.EntityType.Should().Be(op.EntityType);
        afterOp.EntityVersion.Should().Be(op.EntityVersion);
        afterOp.HttpStatusCode.Should().Be(409);
        afterOp.Id.Should().Be(op.Id);
        afterOp.LastAttempt.Should().BeAfter(StartTime).And.BeBefore(DateTimeOffset.UtcNow);
        afterOp.State.Should().Be(OperationState.Failed);
    }

    [Fact]
    public async Task PushOperationAsync_InvalidEntityType_Throws()
    {
        TestDbContext context = CreateContext();
        DatasyncOperation op = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Replace,
            State = OperationState.Pending,
            EntityType = typeof(Entity1).FullName,      // any entity that is not synchronizable
            ItemId = "123",
            EntityVersion = "abcdefg",
            Item = string.Empty,
            Sequence = 0,
            Version = 0
        };

        Func<Task> act = () => context._internalApi.PushOperationAsync(op);
        await act.Should().ThrowAsync<DatasyncException>();
    }

    #endregion

    #region ReplaceDatabaseValues
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ReplaceDatabaseValues_EdgeCases(bool o1IsNull, bool o2IsNull)
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };

        object o1 = o1IsNull ? null : clientMovie;
        object o2 = o2IsNull ? null : clientMovie;

        Action act1 = () => context._internalApi.ReplaceDatabaseValue(o1, o2);
        act1.Should().Throw<DatasyncException>();
    }
    #endregion

    #region ToOperationKind
    [Fact]
    public void ToOperationKind_Invalid_Throws()
    {
        EntityState sut = EntityState.Detached;
        Action act = () => _ = OfflineDbContext.SyncContext.ToOperationKind(sut);
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion

}
