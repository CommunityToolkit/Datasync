// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Offline.OperationsQueue;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.Data.Sqlite;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class OperationsQueueManager_Tests : BaseTest
{
    private readonly TestDbContext context = CreateContext();
    private OperationsQueueManager queueManager { get => this.context.QueueManager; }

    #region GetEntityMap
    [Fact]
    public void GetEntityMap_Works()
    {
        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };

        queueManager.GetEntityMap(this.context).Should().NotBeNullOrEmpty().And.BeEquivalentTo(expected);
    }
    #endregion

    #region GetExistingOperationAsync
    [Fact]
    public async Task GetExistingOperationAsync_InvalidId_Throws()
    {
        ClientMovie movie = new() { Id = "###" };
        Func<Task> act = async () => _ = await queueManager.GetExistingOperationAsync(context.Entry(movie));
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    #region GetSynchronizableEntityTypes
    [Fact]
    public void GetSynchronizableEntityTypes_NoArg()
    {
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];
        queueManager.GetSynchronizableEntityTypes().Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Empty()
    {
        List<Type> allowedTypes = [];
        List<Type> expectedTypes = [];
        queueManager.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_None()
    {
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity4)];
        List<Type> expectedTypes = [];
        queueManager.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Some()
    {
        List<Type> allowedTypes = [typeof(Entity1), typeof(ClientMovie), typeof(Entity4)];
        List<Type> expectedTypes = [typeof(ClientMovie)];
        queueManager.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_All()
    {
        List<Type> allowedTypes = [typeof(Entity3), typeof(ClientMovie)];
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];
        queueManager.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }
    #endregion

    #region PushAsync
    [Fact]
    public async Task PushAsync_BadPushOptions()
    {
        PushOptions options = new() { ParallelOperations = 0 };
        Type[] entityTypes = [typeof(ClientMovie)];

        Func<Task> act = async () => _ = await queueManager.PushAsync(entityTypes, options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PushAsync_NoSynchronizableEntities(int nItems)
    {
        PushOptions options = new();
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity2), typeof(Entity4)];
        Type[] entityTypes = allowedTypes.Take(nItems).ToArray();

        PushResult result = await queueManager.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedRequests.Count.Should().Be(0);
    }

    [Fact]
    public async Task PushAsync_NoOperations()
    {
        PushOptions options = new();
        Type[] entityTypes = [typeof(ClientMovie)];

        PushResult result = await queueManager.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedRequests.Count.Should().Be(0);
    }

    [Fact]
    public async Task PushAsync_Addition_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Created);
        
        PushResult results = await queueManager.PushAsync([ typeof(ClientMovie) ], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();

        ClientMovie actualMovie = this.context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);
    }

    [Fact]
    public async Task PushAsync_Addition_HttpError()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        this.context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(500);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }

    [Fact]
    public async Task PushAsync_Addition_Conflict()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Conflict);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(409);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }

    [Fact]
    public async Task PushAsync_Removal_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();
        this.context.Handler.AddResponse(HttpStatusCode.NoContent);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
        this.context.Movies.Find(clientMovie.Id).Should().BeNull();
    }

    [Fact]
    public async Task PushAsync_Removal_HttpError()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();
        this.context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(500);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }

    [Fact]
    public async Task PushAsync_Removal_Conflict()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Conflict);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(409);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }

    [Fact]
    public async Task PushAsync_Replacement_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        this.context.Update(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.OK);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_Replacement_HttpError()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        this.context.Update(clientMovie);
        this.context.SaveChanges();
        this.context.Handler.AddResponse(HttpStatusCode.InternalServerError);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(500);
        result.HasContent.Should().BeFalse();

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(500);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }

    [Fact]
    public async Task PushAsync_Replacement_Conflict()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        this.context.Update(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Conflict);

        PushResult results = await queueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedRequests.Should().HaveCount(1);
        ServiceResponse result = results.FailedRequests.First().Value;
        result.StatusCode.Should().Be(409);
        result.HasContent.Should().BeTrue();
        string content = new StreamReader(result.ContentStream).ReadToEnd();
        content.Should().Be(expectedJson);

        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation op = this.context.DatasyncOperationsQueue.Single();
        op.HttpStatusCode.Should().Be(409);
        op.LastAttempt.Should().NotBeNull().And.BeOnOrAfter(StartTime);
    }
    #endregion

    #region PushOperationAsync
    [Fact]
    public async Task PushOperationAsync_Throws_InvalidType()
    {
        DatasyncOperation operation = new()
        {
            Id = Guid.NewGuid().ToString(),
            Kind = OperationKind.Delete,
            State = OperationState.Pending,
            EntityType = typeof(Entity1).FullName,
            ItemId = "123",
            EntityVersion = string.Empty,
            Item = """{}""",
            Sequence = 0,
            Version = 0
        };
        Func<Task> act = async () => await queueManager.PushOperationAsync(operation);
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

        Action act1 = () => queueManager.ReplaceDatabaseValue(o1, o2);
        act1.Should().Throw<DatasyncException>();
    }
    #endregion

    #region AsNullableEmptyString()
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("foo", "foo")]
    public void NullAsEmpty_Works(string value, string expected)
    {
        value.AsNullableEmptyString().Should().Be(expected);
    }
    #endregion

    #region ToOperationKind
    [Fact]
    public void ToOperationKind_Invalid_Throws()
    {
        EntityState sut = EntityState.Detached;
        Action act = () => _ = sut.ToOperationKind();
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion

    #region LazyLoadingProxies Support
    [Fact]
    public async Task LLP_PushAsync_Addition_Works()
    {
        TestDbContext llpContext = CreateContext(x => x.UseLazyLoadingProxies());
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        llpContext.Movies.Add(clientMovie);
        llpContext.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        llpContext.Handler.AddResponseContent(expectedJson, HttpStatusCode.Created);

        PushResult results = await llpContext.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        llpContext.DatasyncOperationsQueue.Should().BeEmpty();

        ClientMovie actualMovie = llpContext.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);
    }

    [Fact]
    public async Task LLP_PushAsync_Removal_Works()
    {
        TestDbContext llpContext = CreateContext(x => x.UseLazyLoadingProxies());
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        llpContext.Movies.Add(clientMovie);
        llpContext.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        llpContext.Movies.Remove(clientMovie);
        llpContext.SaveChanges();
        llpContext.Handler.AddResponse(HttpStatusCode.NoContent);

        PushResult results = await llpContext.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        llpContext.DatasyncOperationsQueue.Should().BeEmpty();
        llpContext.Movies.Find(clientMovie.Id).Should().BeNull();
    }

    [Fact]
    public async Task LLP_PushAsync_Replacement_Works()
    {
        TestDbContext llpContext = CreateContext(x => x.UseLazyLoadingProxies());
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        llpContext.Movies.Add(clientMovie);
        llpContext.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Foo";
        llpContext.Update(clientMovie);
        llpContext.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        llpContext.Handler.AddResponseContent(expectedJson, HttpStatusCode.OK);

        PushResult results = await llpContext.QueueManager.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedRequests.Should().BeEmpty();

        llpContext.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public async Task LLP_ModifyAfterInsertInNewContext_NoPush_ShouldUpdateOperationsQueue()
    {
        await using SqliteConnection connection = CreateAndOpenConnection();
        string id = Guid.NewGuid().ToString("N");
        await using (TestDbContext llpContext = CreateContext(connection, x => x.UseLazyLoadingProxies()))
        {
            ClientMovie clientMovie = new(TestData.Movies.MovieList[0].Title) { Id = id };
            llpContext.Movies.Add(clientMovie);
            llpContext.SaveChanges();
        }

        await using TestDbContext newLlpContext = CreateContext(connection, x => x.UseLazyLoadingProxies());

        ClientMovie storedClientMovie = newLlpContext.Movies.First(m => m.Id == id);

        // ensure that it is a lazy loading proxy and not exactly a ClientMovie
        storedClientMovie.GetType().Should().NotBe(typeof(ClientMovie))
            .And.Subject.Namespace.Should().Be("Castle.Proxies");

        storedClientMovie.Title = TestData.Movies.MovieList[1].Title;
        newLlpContext.SaveChanges();

        newLlpContext.DatasyncOperationsQueue.Should().ContainSingle(op => op.ItemId == id)
            .Which.EntityType.Should().NotContain("Castle.Proxies");
    }
    #endregion
}
