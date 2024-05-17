// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContext context;

    // This is the expected JSON content for the Black Panther movie if it has an ID of 1234.
    private const string expectedJsonContent = "{\"bestPictureWinner\":true,\"duration\":134,\"rating\":\"PG13\",\"releaseDate\":\"2018-02-16\",\"title\":\"Black Panther\",\"year\":2018,\"id\":\"1234\",\"updatedAt\":\"0001-01-01T00:00:00.000Z\",\"version\":\"\",\"deleted\":false}";

    // This is the expected Entity Name value.
    private const string expectedEntityName = "CommunityToolkit.Datasync.Common.Test.Models.OfflineMovie";

    #region Test Setup
    public OfflineDbContext_Tests()
    {
        this.connection = new SqliteConnection("DataSource=:memory:");
        this.connection.Open();

        this.context = new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(this.connection).Options);
        this.context.Database.EnsureCreated();
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : OfflineDbContext(options)
    {
        public DbSet<OfflineMovie> Movies { get; set; }
        public DbSet<T_IdAndTitle> Titles { get; set; }
    }
    #endregion

    #region Test Teardown
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.context?.Dispose();
            this.connection?.Dispose();
        }
    }
    #endregion

    #region SaveChanges
    [Fact]
    public void SaveChanges_AddsOfflineEntityToQueue_Create()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Add);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().BeNullOrEmpty();
        queuedItem.ReplacementJsonEntityData.Should().Be(expectedJsonContent);
    }

    [Fact]
    public void SaveChanges_AddsOfflineEntityToQueue_Update()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Update);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
    }

    [Fact]
    public void SaveChanges_AddsOfflineEntityToQueue_Delete()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        this.context.Movies.Remove(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Delete);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().BeNullOrEmpty();
    }

    [Fact]
    public void SaveChanges_AddThenUpdate_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Add);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().BeNullOrEmpty();
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
    }

    [Fact]
    public void SaveChanges_AddThenDelete_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        this.context.Movies.Remove(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public void SaveChanges_AddThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Add,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = string.Empty,
            ReplacementJsonEntityData = expectedJsonContent
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        this.context.SaveChanges();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Action act = () =>
        {
            this.context.Movies.Add(movie);
            this.context.SaveChanges();
        };

        act.Should().Throw<OperationsQueueConflictException>();
    }

    [Fact]
    public void SaveChanges_DeleteThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Delete,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = expectedJsonContent,
            ReplacementJsonEntityData = string.Empty
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        this.context.SaveChanges();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Action act = () =>
        {
            this.context.Movies.Add(movie);
            this.context.SaveChanges();
        };

        act.Should().Throw<OperationsQueueConflictException>();
    }

    [Fact]
    public void SaveChanges_DeleteThenDelete_Throws()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        OfflineQueueEntity queueEntity = this.context.DatasyncOperationsQueue.Single();
        queueEntity.ChangeType = EntityChangeType.Delete;
        this.context.DatasyncOperationsQueue.Update(queueEntity);
        this.context.SaveChanges();

        Action act = () =>
        {
            OfflineMovie movie = this.context.Movies.Find("1234");
            this.context.Movies.Remove(movie);
            this.context.SaveChanges();
        };

        act.Should().Throw<OperationsQueueConflictException>();
    }

    [Fact]
    public void SaveChanges_DeleteThenUpdate_Throws()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        OfflineQueueEntity queueEntity = this.context.DatasyncOperationsQueue.Single();
        queueEntity.ChangeType = EntityChangeType.Delete;
        this.context.DatasyncOperationsQueue.Update(queueEntity);
        this.context.SaveChanges();

        Action act = () =>
        {
            OfflineMovie movie = this.context.Movies.Find("1234");
            movie.Title = "Black Panther Changed";
            this.context.Movies.Update(movie);
            this.context.SaveChanges();
        };

        act.Should().Throw<OperationsQueueConflictException>();
    }

    [Fact]
    public void SaveChanges_UpdateThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Update,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = "{}",
            ReplacementJsonEntityData = expectedJsonContent
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        this.context.SaveChanges();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Action act = () =>
        {
            this.context.Movies.Add(movie);
            this.context.SaveChanges();
        };

        act.Should().Throw<OperationsQueueConflictException>();
    }

    [Fact]
    public void SaveChanges_UpdateThenDelete_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        this.context.SaveChanges();

        this.context.Movies.Remove(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Delete);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
    }

    [Fact]
    public void SaveChanges_UpdateThenUpdate_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        this.context.SaveChanges();

        entity.Duration = 170;
        this.context.Update(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Update);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"duration\":170");
    }

    [Fact]
    public void SaveChanges_DoesNotAddNonOfflineEntityToQueue_Add()
    {
        T_IdAndTitle entity = new() { Id = "1234", Title = "Black Panther" };

        this.context.Add(entity);
        this.context.SaveChanges();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }
    #endregion

    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_AddsOfflineEntityToQueue_Create()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Add);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().BeNullOrEmpty();
        queuedItem.ReplacementJsonEntityData.Should().Be(expectedJsonContent);
    }

    [Fact]
    public async Task SaveChangesAsync_AddsOfflineEntityToQueue_Update()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Update);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
    }

    [Fact]
    public async Task SaveChangesAsync_AddsOfflineEntityToQueue_Delete()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        this.context.Movies.Remove(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Delete);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenUpdate_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Add);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().BeNullOrEmpty();
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenDelete_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        this.context.Movies.Remove(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_AddThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Add,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = string.Empty,
            ReplacementJsonEntityData = expectedJsonContent
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        await this.context.SaveChangesAsync();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Func<Task> act = async () =>
        {
            this.context.Movies.Add(movie);
            await this.context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<OperationsQueueConflictException>();
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Delete,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = expectedJsonContent,
            ReplacementJsonEntityData = string.Empty
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        await this.context.SaveChangesAsync();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Func<Task> act = async () =>
        {
            this.context.Movies.Add(movie);
            await this.context.SaveChangesAsync();
        };

        OperationsQueueConflictException e = (await act.Should().ThrowAsync<OperationsQueueConflictException>()).Subject.SingleOrDefault();
        e.Should().NotBeNull();
        e.QueuedOperation?.Id.Should().Be(existingQueueEntity.Id);
        e.ConflictingEntity.Should().BeOfType<OfflineMovie>();
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenDelete_Throws()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        OfflineQueueEntity queueEntity = this.context.DatasyncOperationsQueue.Single();
        queueEntity.ChangeType = EntityChangeType.Delete;
        this.context.DatasyncOperationsQueue.Update(queueEntity);
        await this.context.SaveChangesAsync();

        Func<Task> act = async () =>
        {
            OfflineMovie movie = this.context.Movies.Find("1234");
            this.context.Movies.Remove(movie);
            await this.context.SaveChangesAsync();
        };

        OperationsQueueConflictException e = (await act.Should().ThrowAsync<OperationsQueueConflictException>()).Subject.SingleOrDefault();
        e.Should().NotBeNull();
        e.QueuedOperation?.Id.Should().Be(queueEntity.Id);
        e.ConflictingEntity.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteThenUpdate_Throws()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        OfflineQueueEntity queueEntity = this.context.DatasyncOperationsQueue.Single();
        queueEntity.ChangeType = EntityChangeType.Delete;
        this.context.DatasyncOperationsQueue.Update(queueEntity);
        await this.context.SaveChangesAsync();

        Func<Task> act = async () =>
        {
            OfflineMovie movie = this.context.Movies.Find("1234");
            movie.Title = "Black Panther Changed";
            this.context.Movies.Update(movie);
            await this.context.SaveChangesAsync();
        };

        OperationsQueueConflictException e = (await act.Should().ThrowAsync<OperationsQueueConflictException>()).Subject.SingleOrDefault();
        e.Should().NotBeNull();
        e.QueuedOperation?.Id.Should().Be(queueEntity.Id);
        e.ConflictingEntity.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_UpdateThenAdd_Throws()
    {
        OfflineQueueEntity existingQueueEntity = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ChangeType = EntityChangeType.Update,
            EntityName = expectedEntityName,
            EntityId = "1234",
            OriginalJsonEntityEntity = "{}",
            ReplacementJsonEntityData = expectedJsonContent
        };
        this.context.DatasyncOperationsQueue.Add(existingQueueEntity);
        await this.context.SaveChangesAsync();

        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        Func<Task> act = async () =>
        {
            this.context.Movies.Add(movie);
            await this.context.SaveChangesAsync();
        };

        OperationsQueueConflictException e = (await act.Should().ThrowAsync<OperationsQueueConflictException>()).Subject.SingleOrDefault();
        e.Should().NotBeNull();
        e.QueuedOperation?.Id.Should().Be(existingQueueEntity.Id);
        e.ConflictingEntity.Should().BeOfType<OfflineMovie>();
    }

    [Fact]
    public async Task SaveChangesAsync_UpdateThenDelete_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        await this.context.SaveChangesAsync();

        this.context.Movies.Remove(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Delete);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
    }

    [Fact]
    public async Task SaveChangesAsync_UpdateThenUpdate_UpdatesQueue()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.RemoveRange(this.context.DatasyncOperationsQueue);
        await this.context.SaveChangesAsync();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        entity.Title = "Black Panther Changed";
        this.context.Update(entity);
        await this.context.SaveChangesAsync();

        entity.Duration = 170;
        this.context.Update(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().ContainSingle();

        OfflineQueueEntity queuedItem = this.context.DatasyncOperationsQueue.Single();
        queuedItem.Should().NotBeNull();
        queuedItem.Id.Should().NotBeNullOrWhiteSpace();
        queuedItem.ChangeType.Should().Be(EntityChangeType.Update);
        queuedItem.EntityId.Should().Be(movie.Id);
        queuedItem.EntityName.Should().Be(expectedEntityName);
        queuedItem.OriginalJsonEntityEntity.Should().Be(expectedJsonContent);
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"title\":\"Black Panther Changed\"");
        queuedItem.ReplacementJsonEntityData.Should().Contain("\"duration\":170");
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotAddNonOfflineEntityToQueue_Add()
    {
        T_IdAndTitle entity = new() { Id = "1234", Title = "Black Panther" };

        this.context.Add(entity);
        await this.context.SaveChangesAsync();

        this.context.DatasyncOperationsQueue.Should().BeEmpty();
    }
    #endregion

    #region Corner Cases
    [Fact]
    public void StoreChangesInOperationsQueue_ThrowsOnInvalidChangeType()
    {
        OfflineMovie movie = new(Movies.BlackPanther) { Id = "1234" };

        this.context.Movies.Add(movie);
        this.context.SaveChanges();

        OfflineMovie entity = this.context.Movies.Find(movie.Id);
        EntityEntry sut = this.context.Entry(entity);
        sut.State = EntityState.Unchanged;

        Action act = () => this.context.StoreChangeInOperationsQueue(sut);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddOperationToQueue_UnknownChangeType_Throws()
    {
        OperationsQueueManager sut = new(this.context);
        Action act = () => sut.AddQueueEntity(EntityChangeType.Unknown, expectedEntityName, "1234", null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" an invalid id")]
    public void ValidateEntityId_NoId_Throws(string idValue)
    {
        OperationsQueueManager manager = this.context.OperationsQueueManager as OperationsQueueManager;
        Action act = () => _ = OperationsQueueManager.ValidateEntityId(idValue);
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion
}
