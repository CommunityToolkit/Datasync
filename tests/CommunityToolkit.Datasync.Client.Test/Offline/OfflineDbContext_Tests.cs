// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0305 // Simplify collection initialization

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests : BaseTest
{
    private readonly TestDbContext context = CreateContext();

    #region Ctor
    [Fact]
    public void Default_Ctor_CreatesInternalApi()
    {
        this.context._operationsQueueManager.Should().NotBeNull();
        this.context._operationsQueueManager._entityMap.Should().NotBeNull();
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
}
