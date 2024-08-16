// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using Microsoft.EntityFrameworkCore;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class OperationsQueueManager_Tests : BaseTest
{
    #region GetExistingOperationAsync
    [Fact]
    public async Task GetExistingOperationAsync_InvalidId_Throws()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager manager = context.QueueManager;
        ClientMovie movie = new() { Id = "###" };
        Func<Task> act = async () => _ = await manager.GetExistingOperationAsync(movie);
        await act.Should().ThrowAsync<DatasyncException>();
    }
    #endregion

    #region GetSynchronizableEntityTypes
    [Fact]
    public void GetSynchronizableEntityTypes_NoArg()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];

        sut.GetSynchronizableEntityTypes().Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Empty()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        List<Type> allowedTypes = [];
        List<Type> expectedTypes = [];

        sut.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_None()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        List<Type> allowedTypes = [typeof(Entity1), typeof(Entity4)];
        List<Type> expectedTypes = [];

        sut.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_Some()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        List<Type> allowedTypes = [typeof(Entity1), typeof(ClientMovie), typeof(Entity4)];
        List<Type> expectedTypes = [typeof(ClientMovie)];

        sut.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public void GetSynchronizableEntityTypes_All()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        List<Type> allowedTypes = [typeof(Entity3), typeof(ClientMovie)];
        List<Type> expectedTypes = [typeof(ClientMovie), typeof(Entity3)];

        sut.GetSynchronizableEntityTypes(allowedTypes).Should().BeEquivalentTo(expectedTypes);
    }
    #endregion

    #region InitializeEntityMap
    [Fact]
    public void InitializeDatasyncEntityMap_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;
        sut.InitializeEntityMap();

        Dictionary<string, Type> expected = new()
        {
            { typeof(ClientMovie).FullName, typeof(ClientMovie) },
            { typeof(Entity3).FullName, typeof(Entity3) }
        };

        sut.EntityMap.Should().NotBeNullOrEmpty().And.BeEquivalentTo(expected);
    }
    #endregion

    #region NullAsEmpty
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("foo", "foo")]
    public void NullAsEmpty_Works(string value, string expected)
    {
        OperationsQueueManager.NullAsEmpty(value).Should().Be(expected);
    }
    #endregion

    #region ToOperationKind
    [Fact]
    public void ToOperationKind_Invalid_Throws()
    {
        EntityState sut = EntityState.Detached;
        Action act = () => _ = OperationsQueueManager.ToOperationKind(sut);
        act.Should().Throw<InvalidOperationException>();
    }
    #endregion

    #region IDisposable
    [Fact]
    public void Dispose_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        sut.Dispose();
        sut.Dispose();
        sut._disposedValue.Should().BeTrue();

        Action act = () => sut.CheckDisposed();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CheckDisposed_Works()
    {
        TestDbContext context = CreateContext();
        OperationsQueueManager sut = context.QueueManager;

        Action act = () => sut.CheckDisposed();
        act.Should().NotThrow();
    }
    #endregion
}
