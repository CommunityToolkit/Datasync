// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0305 // Simplify collection initialization

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Text.Json;
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

    #region SaveChanges
    [Fact]
    public void SaveChanges_Addition_AddsToQueue()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);

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
        string firstMovieJson = JsonSerializer.Serialize(firstMovie, DatasyncSerializer.JsonSerializerOptions);

        ClientMovie secondMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(secondMovie);
        string secondMovieJson = JsonSerializer.Serialize(secondMovie, DatasyncSerializer.JsonSerializerOptions);

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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);

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
        string firstMovieJson = JsonSerializer.Serialize(firstMovie, DatasyncSerializer.JsonSerializerOptions);

        ClientMovie secondMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(secondMovie);
        string secondMovieJson = JsonSerializer.Serialize(secondMovie, DatasyncSerializer.JsonSerializerOptions);

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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
        string serializedEntity = JsonSerializer.Serialize(clientMovie, DatasyncSerializer.JsonSerializerOptions);
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
