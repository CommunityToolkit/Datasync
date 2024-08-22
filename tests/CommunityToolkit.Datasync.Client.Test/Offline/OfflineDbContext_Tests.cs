// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0305 // Simplify collection initialization

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class OfflineDbContext_Tests : BaseTest
{
    private readonly TestDbContext context = CreateContext();

    #region Ctor
    [Fact]
    public void Default_Ctor_CreatesInternalApi()
    {
        this.context.QueueManager.Should().NotBeNull();
        this.context.DeltaTokenStore.Should().NotBeNull();
    }

    [Fact]
    public void Base_Ctor_CreatesInternalApi()
    {
        TestDbContext context = new();
        context.QueueManager.Should().NotBeNull();
        context.DeltaTokenStore.Should().NotBeNull();
    }
    #endregion

    #region PushAsync
    [Fact]
    public async Task PushAsync_BadPushOptions()
    {
        PushOptions options = new() { ParallelOperations = 0 };
        Type[] entityTypes = [typeof(ClientMovie)];

        Func<Task> act = async () => _ = await this.context.PushAsync(entityTypes, options);
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

        PushResult result = await this.context.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedOperations.Count.Should().Be(0);
    }

    [Fact]
    public async Task PushAsync_NoOperations()
    {
        PushOptions options = new();
        Type[] entityTypes = [typeof(ClientMovie)];

        PushResult result = await this.context.PushAsync(entityTypes, options);
        result.CompletedOperations.Should().Be(0);
        result.FailedOperations.Count.Should().Be(0);
    }

    [Fact]
    public async Task DbSet_PushAsync_NoOperations()
    {
        PushOptions options = new();

        PushResult result = await this.context.Movies.PushAsync(options);
        result.CompletedOperations.Should().Be(0);
        result.FailedOperations.Count.Should().Be(0);
    }

    [Fact]
    public async void PushAsync_Addition_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Created);

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();

        ClientMovie actualMovie = this.context.Movies.SingleOrDefault(x => x.Id == clientMovie.Id);
        actualMovie.UpdatedAt!.Should().BeCloseTo((DateTimeOffset)responseMovie.UpdatedAt, TimeSpan.FromMicroseconds(1000));
        actualMovie.Version.Should().Be(responseMovie.Version);
    }

    [Fact]
    public async void DbSet_PushAsync_Addition_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        ClientMovie responseMovie = new(TestData.Movies.BlackPanther) { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        this.context.Handler.AddResponseContent(expectedJson, HttpStatusCode.Created);

        PushResult results = await this.context.Movies.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
        this.context.Movies.Find(clientMovie.Id).Should().BeNull();
    }

    [Fact]
    public async Task DbSet_PushAsync_Removal_Works()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();
        this.context.Handler.AddResponse(HttpStatusCode.NoContent);

        PushResult results = await this.context.Movies.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public async Task DbSet_PushAsync_Replacement_Works()
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

        PushResult results = await this.context.Movies.PushAsync();
        results.IsSuccessful.Should().BeTrue();
        results.CompletedOperations.Should().Be(1);
        results.FailedOperations.Should().BeEmpty();

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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

        PushResult results = await this.context.PushAsync([typeof(ClientMovie)], new PushOptions());
        results.IsSuccessful.Should().BeFalse();
        results.CompletedOperations.Should().Be(0);
        results.FailedOperations.Should().HaveCount(1);
        ServiceResponse result = results.FailedOperations.First().Value;
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

    #region SaveChanges
    [Fact]
    public void SaveChanges_Addition_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);

        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);

        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_TwoAdds_AddsToQueue()
    {
        ClientMovie firstMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string firstMovieJson = DatasyncSerializer.Serialize(firstMovie);

        ClientMovie secondMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string secondMovieJson = DatasyncSerializer.Serialize(secondMovie);

        this.context.Movies.Add(firstMovie);
        this.context.Movies.Add(secondMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(2);
        this.context.DatasyncOperationsQueue.Should().HaveCount(2);
        List<DatasyncOperation> operations = this.context.DatasyncOperationsQueue.ToList();

        DatasyncOperation operation1 = operations.Single(x => x.ItemId == firstMovie.Id);
        operation1.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation1.Id.Should().NotBeNullOrEmpty();
        operation1.Item.Should().Be(firstMovieJson);
        operation1.ItemId.Should().Be(firstMovie.Id);
        operation1.Kind.Should().Be(OperationKind.Add);
        operation1.State.Should().Be(OperationState.Pending);
        operation1.Sequence.Should().Be(1);
        operation1.Version.Should().Be(0);

        DatasyncOperation operation2 = operations.Single(x => x.ItemId == secondMovie.Id);
        operation2.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation2.Id.Should().NotBeNullOrEmpty();
        operation2.Item.Should().Be(secondMovieJson);
        operation2.ItemId.Should().Be(secondMovie.Id);
        operation2.Kind.Should().Be(OperationKind.Add);
        operation2.State.Should().Be(OperationState.Pending);
        operation2.Sequence.Should().Be(2);
        operation2.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_InvalidId_Throws()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = "###" };

        Action act = () =>
        {
            this.context.Movies.Add(clientMovie);
            this.context.SaveChanges();
        };

        act.Should().Throw<DatasyncException>();
    }

    [Fact]
    public void SaveChanges_AddThenDelete_NoQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public void SaveChanges_AddThenReplace_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);

        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_Deletion_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);

        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_DeleteThenAdd_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_DeleteThenDelete_Throws()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);

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
        this.context.DatasyncOperationsQueue.Add(badOperation);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        Action act = () =>
        {
            this.context.Movies.Remove(clientMovie);
            this.context.SaveChanges();
        };

        DatasyncQueueException ex = act.Should().Throw<DatasyncQueueException>().Subject.Single();
        ex.OriginalOperation?.Id.Should().Be(badOperation.Id);
        ex.UpdatedOperation.Should().NotBe(badOperation).And.NotBeNull();
    }

    [Fact]
    public void SaveChanges_Replacement_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public void SaveChanges_ReplaceThenDelete_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Remove(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_ReplaceThenReplace_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        this.context.SaveChanges(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        this.context.Movies.Update(clientMovie);
        this.context.SaveChanges();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        this.context.SaveChanges();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }
    #endregion

    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_Addition_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);

        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_TwoAdds_AddsToQueue()
    {
        ClientMovie firstMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string firstMovieJson = DatasyncSerializer.Serialize(firstMovie);

        ClientMovie secondMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string secondMovieJson = DatasyncSerializer.Serialize(secondMovie);

        this.context.Movies.Add(firstMovie);
        this.context.Movies.Add(secondMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(2);
        this.context.DatasyncOperationsQueue.Should().HaveCount(2);
        List<DatasyncOperation> operations = this.context.DatasyncOperationsQueue.ToList();

        DatasyncOperation operation1 = operations.Single(x => x.ItemId == firstMovie.Id);
        operation1.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation1.Id.Should().NotBeNullOrEmpty();
        operation1.Item.Should().Be(firstMovieJson);
        operation1.ItemId.Should().Be(firstMovie.Id);
        operation1.Kind.Should().Be(OperationKind.Add);
        operation1.State.Should().Be(OperationState.Pending);
        operation1.Sequence.Should().Be(1);
        operation1.Version.Should().Be(0);

        DatasyncOperation operation2 = operations.Single(x => x.ItemId == secondMovie.Id);
        operation2.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation2.Id.Should().NotBeNullOrEmpty();
        operation2.Item.Should().Be(secondMovieJson);
        operation2.ItemId.Should().Be(secondMovie.Id);
        operation2.Kind.Should().Be(OperationKind.Add);
        operation2.State.Should().Be(OperationState.Pending);
        operation2.Sequence.Should().Be(2);
        operation2.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_InvalidId_Throws()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = "###" };

        Func<Task> act = async () =>
        {
            this.context.Movies.Add(clientMovie);
            await this.context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<DatasyncException>();
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenDelete_NoQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Remove(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenReplace_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Add);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_Deletion_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenAdd_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        this.context.Movies.Remove(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenDelete_Throws()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Add(clientMovie);

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

        this.context.DatasyncOperationsQueue.Add(badOperation);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        Func<Task> act = async () =>
        {
            this.context.Movies.Remove(clientMovie);
            await this.context.SaveChangesAsync();
        };

        DatasyncQueueException ex = (await act.Should().ThrowAsync<DatasyncQueueException>()).Subject.Single();
        ex.OriginalOperation?.Id.Should().Be(badOperation.Id);
        ex.UpdatedOperation.Should().NotBe(badOperation).And.NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_Replacement_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_ReplaceThenDelete_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Remove(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(0);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Delete);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ReplaceThenReplace_AddsToQueue()
    {
        ClientMovie clientMovie = new(TestData.Movies.BlackPanther) { Id = Guid.NewGuid().ToString("N") };
        this.context.Movies.Add(clientMovie);
        await this.context.SaveChangesAsync(acceptAllChangesOnSuccess: true, addToQueue: false);

        clientMovie.Title = "Replaced Title";
        this.context.Movies.Update(clientMovie);
        await this.context.SaveChangesAsync();

        clientMovie.Title = "Foo";
        string serializedEntity = DatasyncSerializer.Serialize(clientMovie);
        this.context.Movies.Update(clientMovie);
        await this.context.SaveChangesAsync();

        this.context.Movies.Should().HaveCount(1);
        this.context.DatasyncOperationsQueue.Should().HaveCount(1);
        DatasyncOperation operation = this.context.DatasyncOperationsQueue.SingleOrDefault();
        operation.EntityType.Should().Be(typeof(ClientMovie).FullName);
        operation.Id.Should().NotBeNullOrEmpty();
        operation.Item.Should().Be(serializedEntity);
        operation.ItemId.Should().Be(clientMovie.Id);
        operation.Kind.Should().Be(OperationKind.Replace);
        operation.State.Should().Be(OperationState.Pending);
        operation.Sequence.Should().Be(1);
        operation.Version.Should().Be(1);
    }
    #endregion

    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        this.context.Dispose();
        this.context.Dispose();
        this.context._disposedValue.Should().BeTrue();

        Action act = () => this.context.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_bool_Works()
    {
        this.context.TestDispose(false); // Doesn't dispose the underlying thing
        this.context._disposedValue.Should().BeTrue();

        Action act = () => this.context.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        Action act = () => this.context.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion

    #region DbSet<T>.PushAsync
    [Fact]
    public async Task DbSet_PushAsync_Throws_OnNonOfflineDbContext()
    {
        NotOfflineDbContext context = NotOfflineDbContext.CreateContext();
        Func<Task> act = async () => await context.Movies.PushAsync();
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    public class NotOfflineDbContext : DbContext
    {
        public NotOfflineDbContext() : base()
        {
        }

        public NotOfflineDbContext(DbContextOptions<NotOfflineDbContext> options) : base(options)
        {
        }

        internal SqliteConnection Connection { get; set; }

        public DbSet<ClientMovie> Movies => Set<ClientMovie>();

        public static NotOfflineDbContext CreateContext()
        {
            SqliteConnection connection = new("Data Source=:memory:");
            connection.Open();
            DbContextOptionsBuilder<NotOfflineDbContext> optionsBuilder = new DbContextOptionsBuilder<NotOfflineDbContext>()
                .UseSqlite(connection);
            NotOfflineDbContext context = new(optionsBuilder.Options) { Connection = connection };
            context.Database.EnsureCreated();
            return context;
        }
    }
}
