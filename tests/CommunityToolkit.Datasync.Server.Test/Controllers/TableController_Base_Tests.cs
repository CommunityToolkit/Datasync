// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Models;
using CommunityToolkit.Datasync.Server.Private;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OData.ModelBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CommunityToolkit.Datasync.Server.Test.Controllers;

[ExcludeFromCodeCoverage]
public class TableController_Base_Tests : BaseTest
{
    #region Constructors
    [Fact]
    public void Ctor_Empty_Works()
    {
        TableController<TableData> controller = new();

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeOfType<AccessControlProvider<TableData>>().And.NotBeNull();
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().NotBeNull();
        controller.Repository.Should().BeOfType<Repository<TableData>>().And.NotBeNull();
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        TableController<TableData> controller = new(repository);

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeOfType<AccessControlProvider<TableData>>().And.NotBeNull();
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().NotBeNull();
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_AccessProvider_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        TableController<TableData> controller = new(repository, provider);

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeSameAs(provider);
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().NotBeNull();
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_Options_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        TableControllerOptions options = new();
        TableController<TableData> controller = new(repository, options);

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeOfType<AccessControlProvider<TableData>>().And.NotBeNull();
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().BeSameAs(options);
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_EdmModel_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        ODataConventionModelBuilder modelBuilder = new();
        modelBuilder.EnableLowerCamelCase();
        modelBuilder.AddEntityType(typeof(TableData));
        TableController<TableData> controller = new(repository, modelBuilder.GetEdmModel());

        controller.AccessControlProvider.Should().BeOfType<AccessControlProvider<TableData>>().And.NotBeNull();
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().NotBeNull();
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_AccessProvider_EdmModel_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        ODataConventionModelBuilder modelBuilder = new();
        modelBuilder.EnableLowerCamelCase();
        modelBuilder.AddEntityType(typeof(TableData));
        TableController<TableData> controller = new(repository, provider, modelBuilder.GetEdmModel());

        controller.AccessControlProvider.Should().BeSameAs(provider);
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().NotBeNull();
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_EdmModel_Throws_WhenEntityNotFound()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        ODataConventionModelBuilder modelBuilder = new();
        modelBuilder.EnableLowerCamelCase();
        modelBuilder.AddEntityType(typeof(InMemoryMovie));
        Action act = () => _ = new TableController<TableData>(repository, modelBuilder.GetEdmModel());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ctor_Repository_AccessProvider_Options_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        TableControllerOptions options = new();
        TableController<TableData> controller = new(repository, provider, options);

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeSameAs(provider);
        controller.Logger.Should().BeOfType<NullLogger>().And.NotBeNull();
        controller.Options.Should().BeSameAs(options);
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }

    [Fact]
    public void Ctor_Repository_AccessProvider_Options_Logger_Works()
    {
        IRepository<TableData> repository = FakeRepository<TableData>();
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        TableControllerOptions options = new();
        ILogger logger = Substitute.For<ILogger>();
        TableController<TableData> controller = new(repository, provider, options) { Logger = logger };

        controller.Should().NotBeNull();
        controller.AccessControlProvider.Should().BeSameAs(provider);
        controller.Logger.Should().BeSameAs(logger);
        controller.Options.Should().BeSameAs(options);
        controller.Repository.Should().BeSameAs(repository);
        controller.EdmModel.Should().NotBeNull();
    }
    #endregion

    #region AuthorizeRequestAsync
    [Fact]
    public async Task AuthorizeRequestAsync_ThrowsFromAccessControlProvider()
    {
        IAccessControlProvider<TableData> provider = Substitute.For<IAccessControlProvider<TableData>>();
        provider.IsAuthorizedAsync(Arg.Any<TableOperation>(), Arg.Any<TableData>(), Arg.Any<CancellationToken>()).ThrowsAsync(new ApplicationException());
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, provider);

        Func<Task> act = async () => await controller.__AuthorizeRequestAsync(TableOperation.Create, new TableData(), CancellationToken.None);

        await act.Should().ThrowAsync<ApplicationException>();
    }

    [Fact]
    public async Task AuthorizeRequestAsync_ThrowsIfNotAuthorized()
    {
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, false);
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, provider);

        Func<Task> act = async () => await controller.__AuthorizeRequestAsync(TableOperation.Create, new TableData(), CancellationToken.None);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(401);
    }

    [Fact]
    public async Task AuthorizeRequestAsync_ThrowsIfNotAuthorized_ViaOptions()
    {
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, false);
        IRepository<TableData> repository = FakeRepository<TableData>();
        TableControllerOptions options = new() { UnauthorizedStatusCode = 403 };
        ExposedTableController<TableData> controller = new(repository, provider, options);

        Func<Task> act = async () => await controller.__AuthorizeRequestAsync(TableOperation.Create, new TableData(), CancellationToken.None);

        (await act.Should().ThrowAsync<HttpException>()).WithStatusCode(403);
    }

    [Fact]
    public async Task AuthorizeRequestAsync_AllowsIfAuthorized()
    {
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(TableOperation.Create, true);
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, provider);

        Func<Task> act = async () => await controller.__AuthorizeRequestAsync(TableOperation.Create, new TableData(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
    #endregion

    #region PostCommitHookAsync
    [Theory]
    [InlineData(TableOperation.Create)]
    [InlineData(TableOperation.Delete)]
    [InlineData(TableOperation.Update)]
    public async Task PostCommitHookAsync_NoRepositoryUpdated(TableOperation op)
    {
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(op, true);
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, provider);
        TableData entity = new() { Id = "1" };

        Func<Task> act = async () => await controller.__PostCommitHookAsync(op, entity, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await provider.ReceivedWithAnyArgs(1).PostCommitHookAsync(default, default, default);
    }

    [Theory]
    [InlineData(TableOperation.Create)]
    [InlineData(TableOperation.Delete)]
    [InlineData(TableOperation.Update)]
    public async Task PostCommitHookAsync_FiresRepositoryUpdated(TableOperation op)
    {
        IAccessControlProvider<TableData> provider = FakeAccessControlProvider<TableData>(op, true);
        IRepository<TableData> repository = FakeRepository<TableData>();
        ExposedTableController<TableData> controller = new(repository, provider);
        TableData entity = new() { Id = "1" };
        List<RepositoryUpdatedEventArgs> firedEvents = [];
        controller.RepositoryUpdated += (_, e) => firedEvents.Add(e);

        Func<Task> act = async () => await controller.__PostCommitHookAsync(op, entity, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await provider.ReceivedWithAnyArgs(1).PostCommitHookAsync(default, default, default);
        firedEvents.Should().ContainSingle();
        firedEvents[0].Operation.Should().Be(op);
        firedEvents[0].Entity.Should().BeEquivalentTo(entity);
        firedEvents[0].EntityName.Should().Be("TableData");
        firedEvents[0].Timestamp.Should().BeAfter(this.StartTime).And.BeBefore(DateTimeOffset.UtcNow);
    }
    #endregion

    #region CreateNextLink
    [Theory]
    [InlineData(0, 0, "")]
    [InlineData(5, 0, "$skip=5")]
    [InlineData(0, 5, "$top=5")]
    [InlineData(40, 2, "$skip=40&$top=2")]
    public void CreateNextLink_Works(int skip, int top, string expected)
    {
        const string queryString = null;
        string result = TableController<TableData>.CreateNextLink(queryString, skip, top);
        result.Should().Be(expected);
    }
    #endregion
}
