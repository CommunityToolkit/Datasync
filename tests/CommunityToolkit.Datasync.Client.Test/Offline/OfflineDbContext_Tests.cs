// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0305 // Simplify collection initialization

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Net;
using System.Text;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests : BaseTest
{
    #region Ctor
    [Fact]
    public void Default_Ctor_CreateQueueManager()
    {
        TestDbContext context = new();
        context.QueueManager.Should().NotBeNull();
    }
    #endregion

    #region GetOfflineOptions
    [Fact]
    public void GetOfflineOptions_Null()
    {
        TestDbContext context = CreateContext();
        Action act = () => _ = context.GetOfflineOptions(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOfflineOptions_Works()
    {
        TestDbContext context = CreateContext();
        DatasyncOfflineOptions actual = context.GetOfflineOptions(typeof(ClientMovie));

        actual.HttpClient.Should().NotBeNull();
        actual.HttpClient.BaseAddress.ToString().Should().Be("https://test.zumo.net/");
        actual.Endpoint.ToString().Should().Be("/tables/movies");
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

        Func<Task> act = async () => _ = await context.PushAsync(entityTypes, options);
        await act.Should().ThrowAsync<DatasyncException>();
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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
        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N"), Version="1234" };
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        ServiceResponse result = await context.PushOperationAsync(op);
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

        Func<Task> act = () => context.PushOperationAsync(op);
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

        Action act1 = () => context.ReplaceDatabaseValue(o1, o2);
        act1.Should().Throw<DatasyncException>();
    }
    #endregion

    #region SaveChanges
    [Fact]
    public void SaveChanges_Addition_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_TwoAdds_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie firstMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(firstMovie);
        string firstMovieJson = DatasyncSerializer.Serialize(firstMovie);

        ClientMovie secondMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(secondMovie);
        string secondMovieJson = DatasyncSerializer.Serialize(secondMovie);

        context.Movies.Add(firstMovie);
        context.Movies.Add(secondMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(2);
        context.DatasyncOperationsQueue.Should().HaveCount(2);
        List<DatasyncOperation> operations = context.DatasyncOperationsQueue.ToList();

        DatasyncOperation operation1 = operations.Single(x => x.ItemId == firstMovie.Id);
        operation1.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation1.Id.Should().NotBeNullOrEmpty();
        operation1.Item.Should().Be(firstMovieJson);
        operation1.ItemId.Should().Be(firstMovie.Id);
        operation1.Kind.Should().Be(OperationKind.Add);
        operation1.State.Should().Be(OperationState.Pending);
        operation1.Sequence.Should().Be(0);
        operation1.Version.Should().Be(0);

        DatasyncOperation operation2 = operations.Single(x => x.ItemId == secondMovie.Id);
        operation2.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation2.Id.Should().NotBeNullOrEmpty();
        operation2.Item.Should().Be(secondMovieJson);
        operation2.ItemId.Should().Be(secondMovie.Id);
        operation2.Kind.Should().Be(OperationKind.Add);
        operation2.State.Should().Be(OperationState.Pending);
        operation2.Sequence.Should().Be(1);
        operation2.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_InvalidId_Throws()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = "###" };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        Action act = () =>
        {
            context.Movies.Add(clientMovie);
            context.SaveChanges();
        };

        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void SaveChanges_AddThenDelete_NoQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public void SaveChanges_AddThenReplace_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_Deletion_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_DeleteThenAdd_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_DeleteThenDelete_Throws()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);

        DatasyncOperation badOperation = new()
        {
            EntityType = typeof(ClientMovie).FullName,
            Id = Guid.NewGuid().ToString("N"),
            Item = serializedEntity,
            ItemId = clientMovie.Id,
            EntityVersion = string.Empty,
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            Sequence = 1,
            Version = 0
        };
        context.DatasyncOperationsQueue.Add(badOperation);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        Action act = () =>
        {
            context.Movies.Remove(clientMovie);
            context.SaveChanges();
        };

        DatasyncQueueException ex = act.Should().Throw<DatasyncQueueException>().Subject.Single();
        ex.OriginalOperation?.Id.Should().Be(badOperation.Id);
        ex.UpdatedOperation.Should().NotBe(badOperation).And.NotBeNull();
    }

    [Fact]
    public void SaveChanges_Replacement_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_ReplaceThenDelete_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        context.SaveChanges();

        context.Movies.Remove(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_ReplaceThenReplace_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        context.Movies.Update(clientMovie);
        context.SaveChanges();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        context.SaveChanges();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }
    #endregion

    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_Addition_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);

        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_TwoAdds_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie firstMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(firstMovie);
        string firstMovieJson = DatasyncSerializer.Serialize(firstMovie);

        ClientMovie secondMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(secondMovie);
        string secondMovieJson = DatasyncSerializer.Serialize(secondMovie);

        context.Movies.Add(firstMovie);
        context.Movies.Add(secondMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(2);
        context.DatasyncOperationsQueue.Should().HaveCount(2);
        List<DatasyncOperation> operations = context.DatasyncOperationsQueue.ToList();

        DatasyncOperation operation1 = operations.Single(x => x.ItemId == firstMovie.Id);
        operation1.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation1.Id.Should().NotBeNullOrEmpty();
        operation1.Item.Should().Be(firstMovieJson);
        operation1.ItemId.Should().Be(firstMovie.Id);
        operation1.Kind.Should().Be(OperationKind.Add);
        operation1.State.Should().Be(OperationState.Pending);
        operation1.Sequence.Should().Be(0);
        operation1.Version.Should().Be(0);

        DatasyncOperation operation2 = operations.Single(x => x.ItemId == secondMovie.Id);
        operation2.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation2.Id.Should().NotBeNullOrEmpty();
        operation2.Item.Should().Be(secondMovieJson);
        operation2.ItemId.Should().Be(secondMovie.Id);
        operation2.Kind.Should().Be(OperationKind.Add);
        operation2.State.Should().Be(OperationState.Pending);
        operation2.Sequence.Should().Be(1);
        operation2.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_InvalidId_Throws()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = "###" };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        Func<Task> act = async () =>
        {
            context.Movies.Add(clientMovie);
            await context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenDelete_NoQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Remove(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenReplace_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);

        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_Deletion_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenAdd_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        context.Movies.Remove(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenDelete_Throws()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Add(clientMovie);

        DatasyncOperation badOperation = new()
        {
            EntityType = typeof(ClientMovie).FullName,
            Id = Guid.NewGuid().ToString("N"),
            Item = serializedEntity,
            ItemId = clientMovie.Id,
            EntityVersion = string.Empty,
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            Sequence = 1,
            Version = 0
        };
        context.DatasyncOperationsQueue.Add(badOperation);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        Func<Task> act = async () =>
        {
            context.Movies.Remove(clientMovie);
            await context.SaveChangesAsync();
        };

        DatasyncQueueException ex = (await act.Should().ThrowAsync<DatasyncQueueException>()).Subject.Single();
        ex.OriginalOperation?.Id.Should().Be(badOperation.Id);
        ex.UpdatedOperation.Should().NotBe(badOperation).And.NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_Replacement_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_ReplaceThenDelete_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Remove(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(0);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ReplaceThenReplace_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        context.Movies.Add(clientMovie);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        context.Movies.Update(clientMovie);
        await context.SaveChangesAsync();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        context.Movies.Update(clientMovie);
        await context.SaveChangesAsync();

        context.Movies.Should().HaveCount(1);
        context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(0);
        operation.Version.Should().Be(1);
    }
    #endregion

    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext sut = CreateContext();
        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_bool_Works()
    {
        TestDbContext sut = CreateContext();
        sut.TestDispose(false); // Doesn't dispose the underlying thing
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        TestDbContext sut = CreateContext();
        Action act = () => sut.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion
}
