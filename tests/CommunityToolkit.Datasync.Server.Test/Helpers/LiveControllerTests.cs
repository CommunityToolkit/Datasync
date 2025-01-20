// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CommunityToolkit.Datasync.Server.Test.Helpers;

/// <summary>
/// The base set of tests for the controller tests going against a live server.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class LiveControllerTests<TEntity> : BaseTest where TEntity : class, ITableData
{
    /// <summary>
    /// Returns true if all the requirements for live tests are met.
    /// </summary>
    protected virtual bool CanRunLiveTests() => true;

    /// <summary>
    /// Some tests require the ability to run math queries. This method returns true if the
    /// service can run those tests.  Notably, Cosmos can't run those tests.
    /// </summary>
    protected virtual bool CanRunMathQueryTests() => true;

    /// <summary>
    /// The actual test class must provide an implementation that retrieves the entity through
    /// the backing data store.
    /// </summary>
    /// <param name="id">The ID of the entity to retrieve.</param>
    /// <returns>Either <c>null</c> if the entity does not exist, or the entity.</returns>
    protected abstract Task<TEntity> GetEntityAsync(string id);

    /// <summary>
    /// The actual test class must provide an implementation that retrieves the entity count in
    /// the backing data store.
    /// </summary>
    /// <returns>The number of entities in the store.</returns>
    protected abstract Task<int> GetEntityCountAsync();

    /// <summary>
    /// Retrieves a populated repository for testing.
    /// </summary>
    protected abstract Task<IRepository<TEntity>> GetPopulatedRepositoryAsync();

    /// <summary>
    /// Retrieves a random ID from the database for testing.
    /// </summary>
    protected abstract Task<string> GetRandomEntityIdAsync(bool exists);

    /// <summary>
    /// The Movie Endpoint to put into the controller context.
    /// </summary>
    private const string MovieEndpoint = "http://localhost/tables/movies";

    /// <summary>
    /// The number of movies in the dataset.
    /// </summary>
    private const int MovieCount = 248;

    /// <summary>
    /// The number of items to return by default.
    /// </summary>
    private const int DefaultPageSize = 100;

    /// <summary>
    /// The various providers that are set up by default to cover most cases.
    /// </summary>
    private IRepository<TEntity> repository;
    private TableController<TEntity> tableController;

    /// <summary>
    /// The mechanism for creating a repository and table controller.
    /// </summary>
    /// <param name="uri">The URI to set as the controller context.</param>
    protected virtual async Task CreateControllerAsync(HttpMethod method = null, string uri = null)
    {
        this.repository = await GetPopulatedRepositoryAsync();
        this.tableController = new(this.repository);
        this.tableController.ControllerContext.HttpContext = CreateHttpContext(method ?? HttpMethod.Get, uri);
    }

    private async Task<List<TEntity>> GetListOfEntitiesAsync(IEnumerable<string> ids)
    {
        List<TEntity> entities = [];
        foreach (string id in ids)
        {
            TEntity entity = await GetEntityAsync(id);
            if (entity != null)
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    /// <summary>
    /// This is the base test for the individual query tests.
    /// </summary>
    /// <param name="pathAndQuery">The request URI (path and query only)</param>
    /// <param name="itemCount">The number of items expected to be returned</param>
    /// <param name="nextLinkQuery">The value of the nextLink expected</param>
    /// <param name="totalCount">If provided, the value of the count expected</param>
    /// <param name="firstItems">The start of the list of IDs that should be returned.</param>
    /// <returns>A task that completes when the test is complete.</returns>
    private async Task MovieQueryTest(string pathAndQuery, int itemCount, string nextLinkQuery, int? totalCount, string[] firstItems)
    {
        await CreateControllerAsync(HttpMethod.Get, pathAndQuery);

        IActionResult response = await this.tableController.QueryAsync();

        response.Should().BeAssignableTo<OkObjectResult>();
        ((OkObjectResult)response).Value.Should().BeAssignableTo<PagedResult>();
        PagedResult result = (PagedResult)((OkObjectResult)response).Value;

        List<TEntity> items = result.Items.Cast<TEntity>().ToList();
        items.Should().HaveCount(itemCount);
        result.Count.Should().Be(totalCount);
        List<string> actualItems = items.Select(m => m.Id).Take(firstItems.Length).ToList();

        // Get the list of items in firstItems and actualItems
        List<TEntity> expA1 = await GetListOfEntitiesAsync(firstItems);
        List<TEntity> expA2 = await GetListOfEntitiesAsync(actualItems);
        expA2.Count.Should().Be(actualItems.Count);

        actualItems.Should().BeEquivalentTo(firstItems);

        if (nextLinkQuery is not null)
        {
            result.NextLink.Should().NotBeNull();
            Uri.UnescapeDataString(result.NextLink).Should().Be(nextLinkQuery);
        }
        else
        {
            result.NextLink.Should().BeNull();
        }
    }

    #region Tests
    [SkippableTheory]
    [InlineData("$filter=(year - 1900) ge 100", HttpStatusCode.BadRequest)]
    [InlineData("$filter=missing eq 20", HttpStatusCode.BadRequest)]
    [InlineData("$orderby=duration fizz", HttpStatusCode.BadRequest)]
    [InlineData("$orderby=missing asc", HttpStatusCode.BadRequest)]
    [InlineData("$select=foo", HttpStatusCode.BadRequest)]
    [InlineData("$select=year rating", HttpStatusCode.BadRequest)]
    [InlineData("$skip=-1", HttpStatusCode.BadRequest)]
    [InlineData("$skip=NaN", HttpStatusCode.BadRequest)]
    [InlineData("$top=-1", HttpStatusCode.BadRequest)]
    [InlineData("$top=1000000", HttpStatusCode.BadRequest)]
    [InlineData("$top=NaN", HttpStatusCode.BadRequest)]
    public async Task FailedQueryTest(string query, HttpStatusCode expectedStatusCode)
    {
        Skip.IfNot(CanRunLiveTests());

        await CreateControllerAsync(HttpMethod.Get, $"{MovieEndpoint}?{query}");

        IActionResult result = await this.tableController.QueryAsync();

        result.Should().BeAssignableTo<StatusCodeResult>();
        ((StatusCodeResult)result).StatusCode.Should().Be((int)expectedStatusCode);
    }

    [SkippableFact]
    public async Task Query_Test_001()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            MovieEndpoint,
            DefaultPageSize,
            "$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_002()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true",
            DefaultPageSize,
            "$count=true&$skip=100",
            MovieCount,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_003()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            2,
            ["id-061", "id-173"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_004()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_005()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=(year div 1000.5) eq 2",
            6,
            null,
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_006()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_007()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=(year sub 1900) ge 80",
            DefaultPageSize,
            "$count=true&$filter=(year sub 1900) ge 80&$skip=100",
            138,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
            );
    }

    [SkippableFact]
    public async Task Query_Test_008()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner eq false",
            DefaultPageSize,
            "$count=true&$filter=bestPictureWinner eq false&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_009()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            11,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_010()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            21,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_011()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            24,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_012()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_013()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner ne false",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_014()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=bestPictureWinner ne true",
            DefaultPageSize,
            "$count=true&$filter=bestPictureWinner ne true&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_015()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=ceiling(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=ceiling(duration div 60.0) eq 2&$skip=100",
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_016()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=day(releaseDate) eq 1",
            7,
            null,
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_017()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=duration ge 60",
            DefaultPageSize,
            "$count=true&$filter=duration ge 60&$skip=100",
            MovieCount,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_018()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=endswith(title, 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_019()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=endswith(tolower(title), 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_020()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_021()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=floor(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=floor(duration div 60.0) eq 2&$skip=100",
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_022()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=month(releaseDate) eq 11",
            14,
            null,
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_023()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=not(bestPictureWinner eq false)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_024()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=not(bestPictureWinner eq true)",
            DefaultPageSize,
            "$count=true&$filter=not(bestPictureWinner eq true)&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_025()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=not(bestPictureWinner ne false)",
            DefaultPageSize,
            "$count=true&$filter=not(bestPictureWinner ne false)&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_026()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=not(bestPictureWinner ne true)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_027()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=rating eq 'R'",
            95,
            null,
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_028()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=rating ne 'PG13'",
            DefaultPageSize,
            "$count=true&$filter=rating ne 'PG13'&$skip=100",
            220,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_029()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=rating eq 'Unrated'",
            74,
            null,
            74,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_030()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            2,
            ["id-000", "id-003"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_031()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_032()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_033()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_034()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_035()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=round(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=round(duration div 60.0) eq 2&$skip=100",
            TestCommon.TestData.Movies.MovieList.Count(x => Math.Round(x.Duration / 60.0) == 2.0),
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_037()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=startswith(tolower(title), 'the')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_038()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_039()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year eq 1994",
            5,
            null,
            5,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_040()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year ge 2000 and year le 2009",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_041()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year ge 2000",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_042()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year gt 1999 and year lt 2010",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_043()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year gt 1999",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_044()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year le 2000",
            DefaultPageSize,
            "$count=true&$filter=year le 2000&$skip=100",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_045()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year lt 2001",
            DefaultPageSize,
            "$count=true&$filter=year lt 2001&$skip=100",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_046()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$filter=year(releaseDate) eq 1994",
            6,
            null,
            6,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_047()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=bestPictureWinner asc",
            DefaultPageSize,
            "$count=true&$orderby=bestPictureWinner asc&$skip=100",
            MovieCount,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_048()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=bestPictureWinner desc",
            DefaultPageSize,
            "$count=true&$orderby=bestPictureWinner desc&$skip=100",
            MovieCount,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_049()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=duration asc",
            DefaultPageSize,
            "$count=true&$orderby=duration asc&$skip=100",
            MovieCount,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_050()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=duration desc",
            DefaultPageSize,
            "$count=true&$orderby=duration desc&$skip=100",
            MovieCount,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_051()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=rating asc",
            DefaultPageSize,
            "$count=true&$orderby=rating asc&$skip=100",
            MovieCount,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_052()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=rating desc",
            DefaultPageSize,
            "$count=true&$orderby=rating desc&$skip=100",
            MovieCount,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_053()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=releaseDate asc",
            DefaultPageSize,
            "$count=true&$orderby=releaseDate asc&$skip=100",
            MovieCount,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_054()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=releaseDate desc",
            DefaultPageSize,
            "$count=true&$orderby=releaseDate desc&$skip=100",
            MovieCount,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_055()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=title asc",
            DefaultPageSize,
            "$count=true&$orderby=title asc&$skip=100",
            MovieCount,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_056()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=title desc",
            DefaultPageSize,
            "$count=true&$orderby=title desc&$skip=100",
            MovieCount,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_057()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year asc",
            DefaultPageSize,
            "$count=true&$orderby=year asc&$skip=100",
            MovieCount,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_058()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year asc,title asc",
            DefaultPageSize,
            "$count=true&$orderby=year asc,title asc&$skip=100",
            MovieCount,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_059()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year asc,title desc",
            DefaultPageSize,
            "$count=true&$orderby=year asc,title desc&$skip=100",
            MovieCount,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_060()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year desc",
            DefaultPageSize,
            "$count=true&$orderby=year desc&$skip=100",
            MovieCount,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_061()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year desc,title asc",
            DefaultPageSize,
            "$count=true&$orderby=year desc,title asc&$skip=100",
            MovieCount,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_062()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$orderby=year desc,title desc",
            DefaultPageSize,
            "$count=true&$orderby=year desc,title desc&$skip=100",
            MovieCount,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_063()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            2,
            ["id-061", "id-173"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_064()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_065()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=(year div 1000.5) eq 2",
            6,
            null,
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_066()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_067()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=(year sub 1900) ge 80",
            DefaultPageSize,
            "$count=true&$filter=(year sub 1900) ge 80&$skip=100&$top=25",
            138,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_068()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq false",
            DefaultPageSize,
            "$count=true&$filter=bestPictureWinner eq false&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_069()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            11,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_070()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            21,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_071()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            24,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_072()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_073()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner ne false",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_074()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner ne true",
            DefaultPageSize,
            "$count=true&$filter=bestPictureWinner ne true&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_075()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=ceiling(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=ceiling(duration div 60.0) eq 2&$skip=100&$top=25",
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_076()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=day(releaseDate) eq 1",
            7,
            null,
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_077()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=duration ge 60",
            DefaultPageSize,
            "$count=true&$filter=duration ge 60&$skip=100&$top=25",
            MovieCount,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_078()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=endswith(title, 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_079()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=endswith(tolower(title), 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_080()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_081()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=floor(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=floor(duration div 60.0) eq 2&$skip=100&$top=25",
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_082()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=month(releaseDate) eq 11",
            14,
            null,
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_083()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner eq false)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_084()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner eq true)",
            DefaultPageSize,
            "$count=true&$filter=not(bestPictureWinner eq true)&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_085()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner ne false)",
            DefaultPageSize,
            "$count=true&$filter=not(bestPictureWinner ne false)&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_086()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner ne true)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_087()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=rating eq 'R'",
            95,
            null,
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_088()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=rating ne 'PG13'",
            DefaultPageSize,
            "$count=true&$filter=rating ne 'PG13'&$skip=100&$top=25",
            220,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_089()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=rating eq 'Unrated'",
            74,
            null,
            74,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_090()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            2,
            ["id-000", "id-003"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_091()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_092()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_093()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100&$top=25",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_094()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100&$top=25",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_095()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=round(duration div 60.0) eq 2",
            DefaultPageSize,
            "$count=true&$filter=round(duration div 60.0) eq 2&$skip=100&$top=25",
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_097()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=startswith(tolower(title), 'the')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_098()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_099()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year eq 1994",
            5,
            null,
            5,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_100()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year ge 2000 and year le 2009",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_101()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year ge 2000",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_102()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year gt 1999 and year lt 2010",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_103()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year gt 1999",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_104()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year le 2000",
            DefaultPageSize,
            "$count=true&$filter=year le 2000&$skip=100&$top=25",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_105()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year lt 2001",
            DefaultPageSize,
            "$count=true&$filter=year lt 2001&$skip=100&$top=25",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_106()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$filter=year(releaseDate) eq 1994",
            6,
            null,
            6,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_107()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=bestPictureWinner asc",
            DefaultPageSize,
            "$count=true&$orderby=bestPictureWinner asc&$skip=100&$top=25",
            MovieCount,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_108()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=bestPictureWinner desc",
            DefaultPageSize,
            "$count=true&$orderby=bestPictureWinner desc&$skip=100&$top=25",
            MovieCount,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_109()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=duration asc",
            DefaultPageSize,
            "$count=true&$orderby=duration asc&$skip=100&$top=25",
            MovieCount,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_110()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=duration desc",
            DefaultPageSize,
            "$count=true&$orderby=duration desc&$skip=100&$top=25",
            MovieCount,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_111()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=rating asc",
            DefaultPageSize,
            "$count=true&$orderby=rating asc&$skip=100&$top=25",
            MovieCount,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_112()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=rating desc",
            DefaultPageSize,
            "$count=true&$orderby=rating desc&$skip=100&$top=25",
            MovieCount,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_113()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=releaseDate asc",
            DefaultPageSize,
            "$count=true&$orderby=releaseDate asc&$skip=100&$top=25",
            MovieCount,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_114()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=releaseDate desc",
            DefaultPageSize,
            "$count=true&$orderby=releaseDate desc&$skip=100&$top=25",
            MovieCount,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_115()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=title asc",
            DefaultPageSize,
            "$count=true&$orderby=title asc&$skip=100&$top=25",
            MovieCount,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_116()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=title desc",
            DefaultPageSize,
            "$count=true&$orderby=title desc&$skip=100&$top=25",
            MovieCount,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_117()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year asc",
            DefaultPageSize,
            "$count=true&$orderby=year asc&$skip=100&$top=25",
            MovieCount,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_118()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year asc,title asc",
            DefaultPageSize,
            "$count=true&$orderby=year asc,title asc&$skip=100&$top=25",
            MovieCount,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_119()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year asc,title desc",
            DefaultPageSize,
            "$count=true&$orderby=year asc,title desc&$skip=100&$top=25",
            MovieCount,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_120()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year desc",
            DefaultPageSize,
            "$count=true&$orderby=year desc&$skip=100&$top=25",
            MovieCount,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_121()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year desc,title asc",
            DefaultPageSize,
            "$count=true&$orderby=year desc,title asc&$skip=100&$top=25",
            MovieCount,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_122()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$count=true&$top=125&$orderby=year desc,title desc",
            DefaultPageSize,
            "$count=true&$orderby=year desc,title desc&$skip=100&$top=25",
            MovieCount,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_123()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            null,
            ["id-061", "id-173"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_124()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            null,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_125()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year div 1000.5) eq 2",
            6,
            null,
            null,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_126()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            null,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_127()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year sub 1900) ge 80",
            DefaultPageSize,
            "$filter=(year sub 1900) ge 80&$skip=100",
            null,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_128()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq false",
            DefaultPageSize,
            "$filter=bestPictureWinner eq false&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_129()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            null,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_130()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            null,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_131()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            null,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_132()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_133()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner ne false",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_134()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner ne true",
            DefaultPageSize,
            "$filter=bestPictureWinner ne true&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_135()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=ceiling(duration div 60.0) eq 2",
            DefaultPageSize,
            "$filter=ceiling(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_136()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=day(releaseDate) eq 1",
            7,
            null,
            null,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_137()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=duration ge 60",
            DefaultPageSize,
            "$filter=duration ge 60&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_138()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(title, 'er')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_139()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(tolower(title), 'er')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_140()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_141()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=floor(duration div 60.0) eq 2",
            DefaultPageSize,
            "$filter=floor(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_142()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=month(releaseDate) eq 11",
            14,
            null,
            null,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_143()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner eq false)",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_144()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner eq true)",
            DefaultPageSize,
            "$filter=not(bestPictureWinner eq true)&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_145()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner ne false)",
            DefaultPageSize,
            "$filter=not(bestPictureWinner ne false)&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_146()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner ne true)",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_147()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating eq 'R'",
            95,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_148()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating ne 'PG13'",
            DefaultPageSize,
            "$filter=rating ne 'PG13'&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_149()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating eq 'Unrated'",
            74,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_150()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            null,
            ["id-000", "id-003"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_151()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_152()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_153()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_154()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            DefaultPageSize,
            "$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_155()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=round(duration div 60.0) eq 2",
            DefaultPageSize,
            "$filter=round(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_157()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=startswith(tolower(title), 'the')",
            63,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_158()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_159()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_160()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year ge 2000 and year le 2009",
            55,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_161()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year ge 2000",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_162()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year gt 1999 and year lt 2010",
            55,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_163()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year gt 1999",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_164()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year le 2000",
            DefaultPageSize,
            "$filter=year le 2000&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_165()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year lt 2001",
            DefaultPageSize,
            "$filter=year lt 2001&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_166()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year(releaseDate) eq 1994",
            6,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_167()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=bestPictureWinner asc",
            DefaultPageSize,
            "$orderby=bestPictureWinner asc&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_168()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=bestPictureWinner desc",
            DefaultPageSize,
            "$orderby=bestPictureWinner desc&$skip=100",
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_169()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=duration asc",
            DefaultPageSize,
            "$orderby=duration asc&$skip=100",
            null,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_170()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=duration desc",
            DefaultPageSize,
            "$orderby=duration desc&$skip=100",
            null,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_171()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=rating asc",
            DefaultPageSize,
            "$orderby=rating asc&$skip=100",
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_172()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=rating desc",
            DefaultPageSize,
            "$orderby=rating desc&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_173()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=releaseDate asc",
            DefaultPageSize,
            "$orderby=releaseDate asc&$skip=100",
            null,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_174()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=releaseDate desc",
            DefaultPageSize,
            "$orderby=releaseDate desc&$skip=100",
            null,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_175()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=title asc",
            DefaultPageSize,
            "$orderby=title asc&$skip=100",
            null,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_176()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=title desc",
            DefaultPageSize,
            "$orderby=title desc&$skip=100",
            null,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_177()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc",
            DefaultPageSize,
            "$orderby=year asc&$skip=100",
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_178()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc,title asc",
            DefaultPageSize,
            "$orderby=year asc,title asc&$skip=100",
            null,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_179()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc,title desc",
            DefaultPageSize,
            "$orderby=year asc,title desc&$skip=100",
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_180()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc",
            DefaultPageSize,
            "$orderby=year desc&$skip=100",
            null,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_181()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc,title asc",
            DefaultPageSize,
            "$orderby=year desc,title asc&$skip=100",
            null,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_182()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc,title desc",
            DefaultPageSize,
            "$orderby=year desc,title desc&$skip=100",
            null,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_183()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=((year div 1000.5) eq 2) and (rating eq 'R')&$skip=5",
            0,
            null,
            null,
            []);
    }

    [SkippableFact]
    public async Task Query_Test_184()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)&$skip=5",
            8,
            null,
            null,
            ["id-142", "id-143", "id-162", "id-166", "id-172"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_185()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year div 1000.5) eq 2&$skip=5",
            1,
            null,
            null,
            ["id-216"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_186()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)&$skip=5",
            41,
            null,
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_187()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=(year sub 1900) ge 80&$skip=5",
            DefaultPageSize,
            "$filter=(year sub 1900) ge 80&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_188()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq false&$skip=5",
            DefaultPageSize,
            "$filter=bestPictureWinner eq false&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_189()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2&$skip=5",
            6,
            null,
            null,
            ["id-150", "id-155", "id-186", "id-189", "id-196"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_190()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2&$skip=5",
            16,
            null,
            null,
            ["id-062", "id-083", "id-087", "id-092", "id-093"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_191()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2&$skip=5",
            19,
            null,
            null,
            ["id-092", "id-093", "id-094", "id-096", "id-112"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_192()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner eq true&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_193()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner ne false&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_194()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=bestPictureWinner ne true&$skip=5",
            DefaultPageSize,
            "$filter=bestPictureWinner ne true&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_195()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=ceiling(duration div 60.0) eq 2&$skip=5",
            DefaultPageSize,
            "$filter=ceiling(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-027", "id-028", "id-030", "id-031", "id-032"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_196()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=day(releaseDate) eq 1&$skip=5",
            2,
            null,
            null,
            ["id-197", "id-215"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_197()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=duration ge 60&$skip=5",
            DefaultPageSize,
            "$filter=duration ge 60&$skip=105",
            null,
            ["id-005", "id-006", "id-007", "id-008", "id-009"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_198()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(title, 'er')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_199()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(tolower(title), 'er')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_200()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=endswith(toupper(title), 'ER')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_201()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=floor(duration div 60.0) eq 2&$skip=5",
            DefaultPageSize,
            "$filter=floor(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-009", "id-010", "id-011", "id-012", "id-013"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_202()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=month(releaseDate) eq 11&$skip=5",
            9,
            null,
            null,
            ["id-115", "id-131", "id-136", "id-146", "id-167"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_203()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner eq false)&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_204()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner eq true)&$skip=5",
            DefaultPageSize,
            "$filter=not(bestPictureWinner eq true)&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_205()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner ne false)&$skip=5",
            DefaultPageSize,
            "$filter=not(bestPictureWinner ne false)&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_206()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=not(bestPictureWinner ne true)&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_207()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating eq 'Unrated'&$skip=5",
            69,
            null,
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_208()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating eq 'R'&$skip=5",
            90,
            null,
            null,
            ["id-009", "id-014", "id-017", "id-019", "id-022"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_209()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=rating ne 'PG13'&$skip=5",
            DefaultPageSize,
            "$filter=rating ne 'PG13'&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_210()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate eq cast(1994-10-14,Edm.Date)&$skip=5",
            0,
            null,
            null,
            []);
    }

    [SkippableFact]
    public async Task Query_Test_211()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate ge cast(1999-12-31,Edm.Date)&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_212()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate gt cast(1999-12-31,Edm.Date)&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_213()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=5",
            DefaultPageSize,
            "$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_214()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=5",
            DefaultPageSize,
            "$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_215()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=round(duration div 60.0) eq 2&$skip=5",
            DefaultPageSize,
            "$filter=round(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-013", "id-014", "id-015", "id-016", "id-017"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_217()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=startswith(tolower(title), 'the')&$skip=5",
            58,
            null,
            null,
            ["id-008", "id-012", "id-017", "id-020", "id-023"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_218()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=startswith(toupper(title), 'THE')&$skip=5",
            58,
            null,
            null,
            ["id-008", "id-012", "id-017", "id-020", "id-023"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_219()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year eq 1994&$skip=5",
            0,
            null,
            null,
            []);
    }

    [SkippableFact]
    public async Task Query_Test_220()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year ge 2000 and year le 2009&$skip=5",
            50,
            null,
            null,
            ["id-032", "id-042", "id-050", "id-051", "id-058"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_221()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year ge 2000&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_222()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year gt 1999 and year lt 2010&$skip=5",
            50,
            null,
            null,
            ["id-032", "id-042", "id-050", "id-051", "id-058"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_223()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year gt 1999&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_224()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year le 2000&$skip=5",
            DefaultPageSize,
            "$filter=year le 2000&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_225()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year lt 2001&$skip=5",
            DefaultPageSize,
            "$filter=year lt 2001&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_226()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$filter=year(releaseDate) eq 1994&$skip=5",
            1,
            null,
            null,
            ["id-217"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_227()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=bestPictureWinner asc&$skip=5",
            DefaultPageSize,
            "$orderby=bestPictureWinner asc&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_228()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=bestPictureWinner desc&$skip=5",
            DefaultPageSize,
            "$orderby=bestPictureWinner desc&$skip=105",
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_229()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=duration asc&$skip=5",
            DefaultPageSize,
            "$orderby=duration asc&$skip=105",
            null,
            ["id-238", "id-201", "id-115", "id-229", "id-181"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_230()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=duration desc&$skip=5",
            DefaultPageSize,
            "$orderby=duration desc&$skip=105",
            null,
            ["id-007", "id-183", "id-063", "id-202", "id-130"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_231()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=rating asc&$skip=5",
            DefaultPageSize,
            "$orderby=rating asc&$skip=105",
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_232()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=rating desc&$skip=5",
            DefaultPageSize,
            "$orderby=rating desc&$skip=105",
            null,
            ["id-009", "id-014", "id-017", "id-019", "id-022"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_233()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=releaseDate asc&$skip=5",
            DefaultPageSize,
            "$orderby=releaseDate asc&$skip=105",
            null,
            ["id-229", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_234()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=releaseDate desc&$skip=5",
            DefaultPageSize,
            "$orderby=releaseDate desc&$skip=105",
            null,
            ["id-149", "id-213", "id-102", "id-155", "id-169"]
        );
    }

    // PROBLEM - THIS TEST RESULTS IN DIFFERENT ORDERING ON PGSQL vs. AZURESQL
    //[SkippableFact]
    //public async Task Query_Test_235()
    //{
    //    Skip.IfNot(CanRunLiveTests());

    //    await MovieQueryTest(
    //        $"{MovieEndpoint}?$orderby=title asc&$skip=5",
    //        DefaultPageSize,
    //        "$orderby=title asc&$skip=105",
    //        null,
    //        ["id-214", "id-102", "id-215", "id-039", "id-057"]
    //    );
    //}

    [SkippableFact]
    public async Task Query_Test_236()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=title desc&$skip=5",
            DefaultPageSize,
            "$orderby=title desc&$skip=105",
            null,
            ["id-058", "id-046", "id-160", "id-092", "id-176"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_237()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc&$skip=5",
            DefaultPageSize,
            "$orderby=year asc&$skip=105",
            null,
            ["id-088", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_238()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc,title asc&$skip=5",
            DefaultPageSize,
            "$orderby=year asc,title asc&$skip=105",
            null,
            ["id-088", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_239()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year asc,title desc&$skip=5",
            DefaultPageSize,
            "$orderby=year asc,title desc&$skip=105",
            null,
            ["id-088", "id-224", "id-049", "id-041", "id-135"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_240()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc&$skip=5",
            DefaultPageSize,
            "$orderby=year desc&$skip=105",
            null,
            ["id-149", "id-186", "id-213", "id-013", "id-053"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_241()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc,title asc&$skip=5",
            DefaultPageSize,
            "$orderby=year desc,title asc&$skip=105",
            null,
            ["id-186", "id-064", "id-149", "id-169", "id-161"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_242()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$orderby=year desc,title desc&$skip=5",
            DefaultPageSize,
            "$orderby=year desc,title desc&$skip=105",
            null,
            ["id-186", "id-213", "id-102", "id-053", "id-155"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_243()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$skip=0",
            DefaultPageSize,
            "$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_244()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$skip=100",
            DefaultPageSize,
            "$skip=200",
            null,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_245()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$skip=200",
            48,
            null,
            null,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_246()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$skip=300",
            0,
            null,
            null,
            []);
    }

    [SkippableFact]
    public async Task Query_Test_247()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            null,
            ["id-061", "id-173"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_248()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            5,
            null,
            null,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_249()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=(year div 1000.5) eq 2",
            5,
            null,
            null,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_250()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            5,
            null,
            null,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_251()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=(year sub 1900) ge 80",
            5,
            null,
            null,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_252()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner eq false",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_253()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_254()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_255()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_256()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_257()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner ne false",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_258()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=bestPictureWinner ne true",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_259()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=ceiling(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_260()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=day(releaseDate) eq 1",
            5,
            null,
            null,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_261()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=duration ge 60",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_262()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=endswith(title, 'er')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_263()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=endswith(tolower(title), 'er')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_264()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=endswith(toupper(title), 'ER')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_265()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=floor(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_266()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=month(releaseDate) eq 11",
            5,
            null,
            null,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_267()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=not(bestPictureWinner eq false)",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_268()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=not(bestPictureWinner eq true)",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_269()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=not(bestPictureWinner ne false)",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_270()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=not(bestPictureWinner ne true)",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_271()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=rating eq 'R'",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_272()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=rating ne 'PG13'",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_273()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=rating eq 'Unrated'",
            5,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_274()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            null,
            ["id-000", "id-003"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_275()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_276()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_277()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_278()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_279()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=round(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_281()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=startswith(tolower(title), 'the')",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_282()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=startswith(toupper(title), 'THE')",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_283()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_284()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year ge 2000 and year le 2009",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_285()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year ge 2000",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_286()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year gt 1999 and year lt 2010",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_287()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year gt 1999",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_288()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year le 2000",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_289()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year lt 2001",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_290()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$filter=year(releaseDate) eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_291()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=bestPictureWinner asc",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_292()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=bestPictureWinner desc",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_293()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=duration asc",
            5,
            null,
            null,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_294()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=duration desc",
            5,
            null,
            null,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_295()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=rating asc",
            5,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_296()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=rating desc",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_297()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=releaseDate asc",
            5,
            null,
            null,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_298()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=releaseDate desc",
            5,
            null,
            null,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_299()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=title asc",
            5,
            null,
            null,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_300()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=title desc",
            5,
            null,
            null,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_301()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year asc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_302()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year asc,title asc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_303()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year asc,title desc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_304()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year desc",
            5,
            null,
            null,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_305()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year desc,title asc",
            5,
            null,
            null,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [SkippableFact]
    public async Task Query_Test_306()
    {
        Skip.IfNot(CanRunLiveTests());

        await MovieQueryTest(
            $"{MovieEndpoint}?$top=5&$orderby=year desc,title desc",
            5,
            null,
            null,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }
    #endregion
}
