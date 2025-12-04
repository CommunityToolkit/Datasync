// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Test.Helpers;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;

namespace CommunityToolkit.Datasync.Client.Test.Service;

/// <summary>
/// A set of tests that use the online client and an actual server
/// </summary>
/// <param name="factory"></param>
[ExcludeFromCodeCoverage]
[Collection("SynchronizedOfflineTests")]
public class Integration_Query_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Fact]
    public async Task Query_Test_001()
    {
        await MovieQueryTest(
            x => x,
            Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_002()
    {
        await MovieQueryTest(
            x => x.IncludeTotalCount(),
            Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_003()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year / 1000.5 == 2 && m.Rating == MovieRating.R),
            2,
            ["id-061", "id-173"]
        );
    }

    [Fact]
    public async Task Query_Test_004()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year - 1900 >= 80 && m.Year + 10 < 2000 && m.Duration < 120),
            12,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Query_Test_005()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year / 1000.5 == 2),
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Query_Test_006()
    {
        await MovieQueryTest(
            x => x.Where(m => (m.Year >= 1930 && m.Year <= 1940) || (m.Year >= 1950 && m.Year <= 1960)),
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_007()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year - 1900 >= 80),
            138,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Query_Test_008()
    {
        await MovieQueryTest(
            x => x.Where(m => !m.BestPictureWinner),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_009()
    {
        await MovieQueryTest(
            x => x.Where(m => m.BestPictureWinner && Math.Ceiling(m.Duration / 60.0) == 2),
            11,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [Fact]
    public async Task Query_Test_010()
    {
        await MovieQueryTest(
            x => x.Where(m => m.BestPictureWinner && Math.Floor(m.Duration / 60.0) == 2),
            21,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_011()
    {
        await MovieQueryTest(
            x => x.Where(m => m.BestPictureWinner && Math.Round(m.Duration / 60.0) == 2),
            24,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [Fact]
    public async Task Query_Test_012()
    {
        await MovieQueryTest(
            x => x.Where(m => m.BestPictureWinner),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_013()
    {
        bool expected = false;
        await MovieQueryTest(
            x => x.Where(m => m.BestPictureWinner != expected),
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_016()
    {
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate.Day == 1),
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Query_Test_017()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Duration >= 60),
            Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_018()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Title.EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_019()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Title.ToLowerInvariant().EndsWith("er")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_020()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Title.ToUpperInvariant().EndsWith("ER")),
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_022()
    {
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate.Month == 11),
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Query_Test_024()
    {
        await MovieQueryTest(
            x => x.Where(m => !(m.BestPictureWinner == true)),
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_027()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Rating == MovieRating.R),
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_028()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Rating != MovieRating.PG13),
            220,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_029()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Rating == MovieRating.Unrated),
            74,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_030()
    {
        DateOnly comparison = new(1994, 10, 14);
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate == comparison),
            2,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Query_Test_031()
    {
        DateOnly comparison = new(1999, 12, 31);
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate >= comparison),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_032()
    {
        DateOnly comparison = new(1999, 12, 31);
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate > comparison),
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_033()
    {
        DateOnly comparison = new(2000, 1, 1);
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate <= comparison),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_034()
    {
        DateOnly comparison = new(2000, 1, 1);
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate < comparison),
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_035()
    {
        await MovieQueryTest(
            x => x.Where(m => Math.Round(m.Duration / 60.0) == 2.0),
            TestCommon.TestData.Movies.MovieList.Count(x => Math.Round(x.Duration / 60.0) == 2.0),
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_037()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Title.StartsWith("the", StringComparison.InvariantCultureIgnoreCase)),
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_039()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year == 1994),
            5,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_040()
    {
        await MovieQueryTest(
            x => x.Where(m => m.Year >= 2000).Where(m => m.Year <= 2009),
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_046()
    {
        await MovieQueryTest(
            x => x.Where(m => m.ReleaseDate.Year == 1994),
            6,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_047()
    {
        await MovieQueryTest(
            x => x.OrderBy(m => m.BestPictureWinner),
            Count<InMemoryMovie>(),
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_048()
    {
        await MovieQueryTest(
            x => x.OrderByDescending(m => m.BestPictureWinner),
            Count<InMemoryMovie>(),
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_049()
    {
        await MovieQueryTest(
            x => x.OrderBy(m => m.Duration),
            Count<InMemoryMovie>(),
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_050()
    {
        await MovieQueryTest(
            x => x.OrderByDescending(m => m.Duration),
            Count<InMemoryMovie>(),
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Query_Test_059()
    {
        await MovieQueryTest(
            x => x.OrderBy(m => m.Year).ThenByDescending(m => m.Title),
            Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_061()
    {
        await MovieQueryTest(
            x => x.OrderByDescending(m => m.Year).ThenBy(m => m.Title),
            Count<InMemoryMovie>(),
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_002()
    {
        SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await SoftDeletedMovieQueryTest(
            x => x.IncludeTotalCount(),
            153,
            ["id-004", "id-005", "id-006", "id-008", "id-010"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_003()
    {
        SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await SoftDeletedMovieQueryTest(
            x => x.IncludeDeletedItems().Where(m => !m.Deleted),
            153,
            ["id-004", "id-005", "id-006", "id-008", "id-010"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_004()
    {
        SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await MovieQueryTest(
            x => x.IncludeDeletedItems().Where(m => m.Deleted),
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_010()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Second == 0).OrderBy(m => m.Id),
            365,
            ["id-000", "id-001", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_011()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Hour == 3).OrderBy(m => m.Id),
            31,
            ["id-059", "id-060", "id-061", "id-062", "id-063", "id-064"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_012()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue.Minute == 21).OrderBy(m => m.Id),
            12,
            ["id-020", "id-051", "id-079", "id-110", "id-140", "id-171"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_014()
    {
        SeedKitchenSinkWithDateTimeData();
        TimeOnly comparison = new(2, 14, 0);
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue == comparison),
            1,
            ["id-044"]

        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_015()
    {
        SeedKitchenSinkWithDateTimeData();
        TimeOnly comparison = new(2, 15, 0);
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue >= comparison).OrderBy(m => m.Id),
            320,
            ["id-045", "id-046", "id-047", "id-048"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_016()
    {
        SeedKitchenSinkWithDateTimeData();
        TimeOnly comparison = new(2, 15, 0);
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue > comparison).OrderBy(m => m.Id),
            319,
            ["id-046", "id-047", "id-048", "id-049"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_017()
    {
        SeedKitchenSinkWithDateTimeData();
        TimeOnly comparison = new(7, 14, 0);
        await KitchenSinkQueryTest(
            x => x.Where(m => m.TimeOnlyValue <= comparison).OrderBy(m => m.Id),
            195,
            ["id-000", "id-001", "id-002", "id-003"]
        );
    }

    //[Fact]
    //public async Task KitchenSinkQueryTest_019()
    //{
    //    SeedKitchenSinkWithCountryData();
    //    await KitchenSinkQueryTest(
    //        $"{this.factory.KitchenSinkEndpoint}?$filter=geo.distance(pointValue, geography'POINT(-97 38)') lt 0.2",
    //        1,
    //        null,
    //        null,
    //        ["US"]
    //    );
    //}

    [Fact(Skip = "OData v8.4 does not allow string.contains")]
    public async Task KitchenSinkQueryTest_020()
    {
        SeedKitchenSinkWithCountryData();
        string[] comparison = ["IT", "GR", "EG"];
        await KitchenSinkQueryTest(
            x => x.Where(m => comparison.Contains(m.Id)).OrderBy(m => m.Id),
            3,
            ["EG", "GR", "IT"]
        );
    }

    #region Base Tests
    private async Task MovieQueryTest(
        Func<IDatasyncQueryable<ClientMovie>, IDatasyncQueryable<ClientMovie>> query, 
        int itemCount, 
        string[] firstItems)
    {
        DatasyncServiceClient<ClientMovie> client = GetMovieClient();

        IDatasyncQueryable<ClientMovie> executableQuery = query.Invoke(client.AsQueryable());
        List<ClientMovie> results = await executableQuery.ToListAsync();

        results.Count.Should().Be(itemCount);
        results.Take(firstItems.Length).Select(m => m.Id).Should().BeEquivalentTo(firstItems);
        foreach (ClientMovie item in results)
        {
            InMemoryMovie expected = GetServerEntityById<InMemoryMovie>(item.Id)!;
            item.Should().BeEquivalentTo<IMovie>(expected);
        }
    }

    private async Task SoftDeletedMovieQueryTest(
        Func<IDatasyncQueryable<ClientMovie>, IDatasyncQueryable<ClientMovie>> query,
        int itemCount,
        string[] firstItems)
    {
        DatasyncServiceClient<ClientMovie> client = GetSoftDeletedMovieClient();

        IDatasyncQueryable<ClientMovie> executableQuery = query.Invoke(client.AsQueryable());
        List<ClientMovie> results = await executableQuery.ToListAsync();

        results.Count.Should().Be(itemCount);
        results.Take(firstItems.Length).Select(m => m.Id).Should().BeEquivalentTo(firstItems);
        foreach (ClientMovie item in results)
        {
            InMemoryMovie expected = GetServerEntityById<InMemoryMovie>(item.Id)!;
            item.Should().BeEquivalentTo<IMovie>(expected);
        }
    }

    private async Task KitchenSinkQueryTest(
        Func<IDatasyncQueryable<ClientKitchenSink>, IDatasyncQueryable<ClientKitchenSink>> query,
        int itemCount,
        string[] firstItems)
    {
        DatasyncServiceClient<ClientKitchenSink> client = GetKitchenSinkClient();

        IDatasyncQueryable<ClientKitchenSink> executableQuery = query.Invoke(client.AsQueryable());
        List<ClientKitchenSink> results = await executableQuery.ToListAsync();

        results.Count.Should().Be(itemCount);
        results.Take(firstItems.Length).Select(m => m.Id).Should().BeEquivalentTo(firstItems);
        foreach (ClientKitchenSink item in results)
        {
            InMemoryKitchenSink expected = GetServerEntityById<InMemoryKitchenSink>(item.Id)!;
            item.Should().BeEquivalentTo<IKitchenSink>(expected);
        }
    }
    #endregion
}
