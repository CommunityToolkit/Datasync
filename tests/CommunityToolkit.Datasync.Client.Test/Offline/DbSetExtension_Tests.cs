// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Offline;
using CommunityToolkit.Datasync.Client.Serialization;
using CommunityToolkit.Datasync.Client.Test.Offline.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using System.Net;
using System.Text;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Offline;

[ExcludeFromCodeCoverage]
public class DbSetExtension_Tests : BaseTest
{
    #region PushAsync
    [Fact]
    public async void PushAsync_Addition_Works()
    {
        TestDbContext context = CreateContext();
        ClientMovie clientMovie = new() { Id = Guid.NewGuid().ToString("N") };
        TestData.Movies.BlackPanther.CopyTo(clientMovie);
        string clientMovieJson = DatasyncSerializer.Serialize(clientMovie);
        Type[] entityTypes = [typeof(ClientMovie)];

        context.Movies.Add(clientMovie);
        context.SaveChanges();

        ClientMovie responseMovie = new() { Id = clientMovie.Id, UpdatedAt = DateTimeOffset.UtcNow, Version = Guid.NewGuid().ToString() };
        TestData.Movies.BlackPanther.CopyTo(responseMovie);
        string expectedJson = DatasyncSerializer.Serialize(responseMovie);
        context.Handler.Responses.Add(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(expectedJson, Encoding.UTF8, "application/json")
        });

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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

        PushOperationResult results = await context.Movies.PushAsync();
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
    public async Task PushAsync_Throws_NonOffline()
    {
        SqliteDbContext context = SqliteDbContext.CreateContext();
        Func<Task> act = async () => _ = await context.Movies.PushAsync();
        await act.Should().ThrowAsync<DatasyncException>();
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
}
