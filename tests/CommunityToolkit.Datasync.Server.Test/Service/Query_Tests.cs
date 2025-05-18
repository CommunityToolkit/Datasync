// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon;
using CommunityToolkit.Datasync.TestCommon.Databases;
using CommunityToolkit.Datasync.TestCommon.Models;
using System.Net;
using System.Text.Json;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Server.Test.Service;

[ExcludeFromCodeCoverage]
public class Query_Tests(ServiceApplicationFactory factory) : ServiceTest(factory), IClassFixture<ServiceApplicationFactory>
{
    [Theory]
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
        HttpResponseMessage response = await this.client.GetAsync($"{this.factory.MovieEndpoint}?{query}");
        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task Query_Test_001()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}",
            100,
            "$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_002()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true",
            100,
            "$count=true&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_003()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            2,
            ["id-061", "id-173"]
        );
    }

    [Fact]
    public async Task Query_Test_004()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Query_Test_005()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=(year div 1000.5) eq 2",
            6,
            null,
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Query_Test_006()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_007()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=(year sub 1900) ge 80",
            100,
            "$count=true&$filter=(year sub 1900) ge 80&$skip=100",
            138,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
            );
    }

    [Fact]
    public async Task Query_Test_008()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner eq false",
            100,
            "$count=true&$filter=bestPictureWinner eq false&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_009()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            11,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [Fact]
    public async Task Query_Test_010()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            21,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_011()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            24,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [Fact]
    public async Task Query_Test_012()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner eq true",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_013()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner ne false",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_014()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=bestPictureWinner ne true",
            100,
            "$count=true&$filter=bestPictureWinner ne true&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_015()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=ceiling(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=ceiling(duration div 60.0) eq 2&$skip=100",
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Query_Test_016()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=day(releaseDate) eq 1",
            7,
            null,
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Query_Test_017()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=duration ge 60",
            100,
            "$count=true&$filter=duration ge 60&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_018()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=endswith(title, 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_019()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=endswith(tolower(title), 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_020()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_021()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=floor(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=floor(duration div 60.0) eq 2&$skip=100",
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_022()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=month(releaseDate) eq 11",
            14,
            null,
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Query_Test_023()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=not(bestPictureWinner eq false)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_024()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=not(bestPictureWinner eq true)",
            100,
            "$count=true&$filter=not(bestPictureWinner eq true)&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_025()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=not(bestPictureWinner ne false)",
            100,
            "$count=true&$filter=not(bestPictureWinner ne false)&$skip=100",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_026()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=not(bestPictureWinner ne true)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_027()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=rating eq 'R'",
            95,
            null,
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_028()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=rating ne 'PG13'",
            100,
            "$count=true&$filter=rating ne 'PG13'&$skip=100",
            220,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_029()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=rating eq 'Unrated'",
            74,
            null,
            74,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_030()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            2,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Query_Test_031()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_032()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_033()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            100,
            "$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_034()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            100,
            "$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_035()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=round(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=round(duration div 60.0) eq 2&$skip=100",
            TestData.Movies.MovieList.Count(x => Math.Round(x.Duration / 60.0) == 2.0),
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_037()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=startswith(tolower(title), 'the')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_038()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_039()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year eq 1994",
            5,
            null,
            5,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_040()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year ge 2000 and year le 2009",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_041()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year ge 2000",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_042()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year gt 1999 and year lt 2010",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_043()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year gt 1999",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_044()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year le 2000",
            100,
            "$count=true&$filter=year le 2000&$skip=100",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_045()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year lt 2001",
            100,
            "$count=true&$filter=year lt 2001&$skip=100",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_046()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$filter=year(releaseDate) eq 1994",
            6,
            null,
            6,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_047()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=bestPictureWinner asc",
            100,
            "$count=true&$orderby=bestPictureWinner asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_048()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=bestPictureWinner desc",
            100,
            "$count=true&$orderby=bestPictureWinner desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_049()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=duration asc",
            100,
            "$count=true&$orderby=duration asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_050()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=duration desc",
            100,
            "$count=true&$orderby=duration desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Query_Test_051()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=rating asc",
            100,
            "$count=true&$orderby=rating asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_052()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=rating desc",
            100,
            "$count=true&$orderby=rating desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_053()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=releaseDate asc",
            100,
            "$count=true&$orderby=releaseDate asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Query_Test_054()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=releaseDate desc",
            100,
            "$count=true&$orderby=releaseDate desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_055()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=title asc",
            100,
            "$count=true&$orderby=title asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Query_Test_056()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=title desc",
            100,
            "$count=true&$orderby=title desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Query_Test_057()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year asc",
            100,
            "$count=true&$orderby=year asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_058()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year asc,title asc",
            100,
            "$count=true&$orderby=year asc,title asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_059()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year asc,title desc",
            100,
            "$count=true&$orderby=year asc,title desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_060()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year desc",
            100,
            "$count=true&$orderby=year desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [Fact]
    public async Task Query_Test_061()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year desc,title asc",
            100,
            "$count=true&$orderby=year desc,title asc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [Fact]
    public async Task Query_Test_062()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$orderby=year desc,title desc",
            100,
            "$count=true&$orderby=year desc,title desc&$skip=100",
            this.factory.Count<InMemoryMovie>(),
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_063()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            2,
            ["id-061", "id-173"]
        );
    }

    [Fact]
    public async Task Query_Test_064()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            13,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Query_Test_065()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=(year div 1000.5) eq 2",
            6,
            null,
            6,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Query_Test_066()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            46,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_067()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=(year sub 1900) ge 80",
            100,
            "$count=true&$filter=(year sub 1900) ge 80&$skip=100&$top=25",
            138,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Query_Test_068()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq false",
            100,
            "$count=true&$filter=bestPictureWinner eq false&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_069()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            11,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [Fact]
    public async Task Query_Test_070()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            21,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_071()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            24,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [Fact]
    public async Task Query_Test_072()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner eq true",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_073()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner ne false",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_074()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=bestPictureWinner ne true",
            100,
            "$count=true&$filter=bestPictureWinner ne true&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_075()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=ceiling(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=ceiling(duration div 60.0) eq 2&$skip=100&$top=25",
            124,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Query_Test_076()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=day(releaseDate) eq 1",
            7,
            null,
            7,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Query_Test_077()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=duration ge 60",
            100,
            "$count=true&$filter=duration ge 60&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_078()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=endswith(title, 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_079()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=endswith(tolower(title), 'er')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_080()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            12,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_081()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=floor(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=floor(duration div 60.0) eq 2&$skip=100&$top=25",
            120,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_082()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=month(releaseDate) eq 11",
            14,
            null,
            14,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Query_Test_083()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner eq false)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_084()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner eq true)",
            100,
            "$count=true&$filter=not(bestPictureWinner eq true)&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_085()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner ne false)",
            100,
            "$count=true&$filter=not(bestPictureWinner ne false)&$skip=100&$top=25",
            210,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_086()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=not(bestPictureWinner ne true)",
            38,
            null,
            38,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_087()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=rating eq 'R'",
            95,
            null,
            95,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_088()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=rating ne 'PG13'",
            100,
            "$count=true&$filter=rating ne 'PG13'&$skip=100&$top=25",
            220,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_089()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=rating eq 'Unrated'",
            74,
            null,
            74,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_090()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            2,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Query_Test_091()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_092()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_093()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            100,
            "$count=true&$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100&$top=25",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_094()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            100,
            "$count=true&$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100&$top=25",
            179,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_095()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=round(duration div 60.0) eq 2",
            100,
            "$count=true&$filter=round(duration div 60.0) eq 2&$skip=100&$top=25",
            186,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_097()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=startswith(tolower(title), 'the')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_098()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            63,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_099()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year eq 1994",
            5,
            null,
            5,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_100()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year ge 2000 and year le 2009",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_101()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year ge 2000",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_102()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year gt 1999 and year lt 2010",
            55,
            null,
            55,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_103()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year gt 1999",
            69,
            null,
            69,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_104()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year le 2000",
            100,
            "$count=true&$filter=year le 2000&$skip=100&$top=25",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_105()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year lt 2001",
            100,
            "$count=true&$filter=year lt 2001&$skip=100&$top=25",
            185,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_106()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$filter=year(releaseDate) eq 1994",
            6,
            null,
            6,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_107()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=bestPictureWinner asc",
            100,
            "$count=true&$orderby=bestPictureWinner asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_108()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=bestPictureWinner desc",
            100,
            "$count=true&$orderby=bestPictureWinner desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_109()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=duration asc",
            100,
            "$count=true&$orderby=duration asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_110()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=duration desc",
            100,
            "$count=true&$orderby=duration desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Query_Test_111()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=rating asc",
            100,
            "$count=true&$orderby=rating asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_112()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=rating desc",
            100,
            "$count=true&$orderby=rating desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_113()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=releaseDate asc",
            100,
            "$count=true&$orderby=releaseDate asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Query_Test_114()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=releaseDate desc",
            100,
            "$count=true&$orderby=releaseDate desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_115()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=title asc",
            100,
            "$count=true&$orderby=title asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Query_Test_116()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=title desc",
            100,
            "$count=true&$orderby=title desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Query_Test_117()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year asc",
            100,
            "$count=true&$orderby=year asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_118()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year asc,title asc",
            100,
            "$count=true&$orderby=year asc,title asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_119()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year asc,title desc",
            100,
            "$count=true&$orderby=year asc,title desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_120()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year desc",
            100,
            "$count=true&$orderby=year desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [Fact]
    public async Task Query_Test_121()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year desc,title asc",
            100,
            "$count=true&$orderby=year desc,title asc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [Fact]
    public async Task Query_Test_122()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$count=true&$top=125&$orderby=year desc,title desc",
            100,
            "$count=true&$orderby=year desc,title desc&$skip=100&$top=25",
            this.factory.Count<InMemoryMovie>(),
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_123()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            null,
            ["id-061", "id-173"]
        );
    }

    [Fact]
    public async Task Query_Test_124()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            13,
            null,
            null,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Query_Test_125()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year div 1000.5) eq 2",
            6,
            null,
            null,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Query_Test_126()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            46,
            null,
            null,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_127()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year sub 1900) ge 80",
            100,
            "$filter=(year sub 1900) ge 80&$skip=100",
            null,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Query_Test_128()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq false",
            100,
            "$filter=bestPictureWinner eq false&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_129()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            11,
            null,
            null,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [Fact]
    public async Task Query_Test_130()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            21,
            null,
            null,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_131()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            24,
            null,
            null,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [Fact]
    public async Task Query_Test_132()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_133()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner ne false",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_134()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner ne true",
            100,
            "$filter=bestPictureWinner ne true&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_135()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=ceiling(duration div 60.0) eq 2",
            100,
            "$filter=ceiling(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Query_Test_136()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=day(releaseDate) eq 1",
            7,
            null,
            null,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Query_Test_137()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=duration ge 60",
            100,
            "$filter=duration ge 60&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_138()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(title, 'er')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_139()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(tolower(title), 'er')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_140()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(toupper(title), 'ER')",
            12,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_141()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=floor(duration div 60.0) eq 2",
            100,
            "$filter=floor(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_142()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=month(releaseDate) eq 11",
            14,
            null,
            null,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Query_Test_143()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner eq false)",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_144()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner eq true)",
            100,
            "$filter=not(bestPictureWinner eq true)&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_145()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner ne false)",
            100,
            "$filter=not(bestPictureWinner ne false)&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_146()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner ne true)",
            38,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_147()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating eq 'R'",
            95,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_148()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating ne 'PG13'",
            100,
            "$filter=rating ne 'PG13'&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_149()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating eq 'Unrated'",
            74,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_150()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            null,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Query_Test_151()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_152()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_153()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            100,
            "$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_154()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            100,
            "$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_155()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=round(duration div 60.0) eq 2",
            100,
            "$filter=round(duration div 60.0) eq 2&$skip=100",
            null,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_157()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=startswith(tolower(title), 'the')",
            63,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_158()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=startswith(toupper(title), 'THE')",
            63,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_159()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_160()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year ge 2000 and year le 2009",
            55,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_161()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year ge 2000",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_162()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year gt 1999 and year lt 2010",
            55,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_163()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year gt 1999",
            69,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_164()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year le 2000",
            100,
            "$filter=year le 2000&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_165()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year lt 2001",
            100,
            "$filter=year lt 2001&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_166()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year(releaseDate) eq 1994",
            6,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_167()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=bestPictureWinner asc",
            100,
            "$orderby=bestPictureWinner asc&$skip=100",
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_168()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=bestPictureWinner desc",
            100,
            "$orderby=bestPictureWinner desc&$skip=100",
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_169()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=duration asc",
            100,
            "$orderby=duration asc&$skip=100",
            null,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_170()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=duration desc",
            100,
            "$orderby=duration desc&$skip=100",
            null,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Query_Test_171()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=rating asc",
            100,
            "$orderby=rating asc&$skip=100",
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_172()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=rating desc",
            100,
            "$orderby=rating desc&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_173()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=releaseDate asc",
            100,
            "$orderby=releaseDate asc&$skip=100",
            null,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Query_Test_174()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=releaseDate desc",
            100,
            "$orderby=releaseDate desc&$skip=100",
            null,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_175()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=title asc",
            100,
            "$orderby=title asc&$skip=100",
            null,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Query_Test_176()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=title desc",
            100,
            "$orderby=title desc&$skip=100",
            null,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Query_Test_177()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc",
            100,
            "$orderby=year asc&$skip=100",
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_178()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc,title asc",
            100,
            "$orderby=year asc,title asc&$skip=100",
            null,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_179()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc,title desc",
            100,
            "$orderby=year asc,title desc&$skip=100",
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_180()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc",
            100,
            "$orderby=year desc&$skip=100",
            null,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [Fact]
    public async Task Query_Test_181()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc,title asc",
            100,
            "$orderby=year desc,title asc&$skip=100",
            null,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [Fact]
    public async Task Query_Test_182()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc,title desc",
            100,
            "$orderby=year desc,title desc&$skip=100",
            null,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_183()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=((year div 1000.5) eq 2) and (rating eq 'R')&$skip=5",
            0,
            null,
            null,
            []);
    }

    [Fact]
    public async Task Query_Test_184()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)&$skip=5",
            8,
            null,
            null,
            ["id-142", "id-143", "id-162", "id-166", "id-172"]
        );
    }

    [Fact]
    public async Task Query_Test_185()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year div 1000.5) eq 2&$skip=5",
            1,
            null,
            null,
            ["id-216"]
        );
    }

    [Fact]
    public async Task Query_Test_186()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)&$skip=5",
            41,
            null,
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [Fact]
    public async Task Query_Test_187()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=(year sub 1900) ge 80&$skip=5",
            100,
            "$filter=(year sub 1900) ge 80&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_188()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq false&$skip=5",
            100,
            "$filter=bestPictureWinner eq false&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_189()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2&$skip=5",
            6,
            null,
            null,
            ["id-150", "id-155", "id-186", "id-189", "id-196"]
        );
    }

    [Fact]
    public async Task Query_Test_190()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2&$skip=5",
            16,
            null,
            null,
            ["id-062", "id-083", "id-087", "id-092", "id-093"]
        );
    }

    [Fact]
    public async Task Query_Test_191()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2&$skip=5",
            19,
            null,
            null,
            ["id-092", "id-093", "id-094", "id-096", "id-112"]
        );
    }

    [Fact]
    public async Task Query_Test_192()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner eq true&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_193()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner ne false&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_194()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=bestPictureWinner ne true&$skip=5",
            100,
            "$filter=bestPictureWinner ne true&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_195()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=ceiling(duration div 60.0) eq 2&$skip=5",
            100,
            "$filter=ceiling(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-027", "id-028", "id-030", "id-031", "id-032"]
        );
    }

    [Fact]
    public async Task Query_Test_196()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=day(releaseDate) eq 1&$skip=5",
            2,
            null,
            null,
            ["id-197", "id-215"]
        );
    }

    [Fact]
    public async Task Query_Test_197()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=duration ge 60&$skip=5",
            100,
            "$filter=duration ge 60&$skip=105",
            null,
            ["id-005", "id-006", "id-007", "id-008", "id-009"]
        );
    }

    [Fact]
    public async Task Query_Test_198()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(title, 'er')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [Fact]
    public async Task Query_Test_199()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(tolower(title), 'er')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [Fact]
    public async Task Query_Test_200()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=endswith(toupper(title), 'ER')&$skip=5",
            7,
            null,
            null,
            ["id-170", "id-193", "id-197", "id-205", "id-217"]
        );
    }

    [Fact]
    public async Task Query_Test_201()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=floor(duration div 60.0) eq 2&$skip=5",
            100,
            "$filter=floor(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-009", "id-010", "id-011", "id-012", "id-013"]
        );
    }

    [Fact]
    public async Task Query_Test_202()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=month(releaseDate) eq 11&$skip=5",
            9,
            null,
            null,
            ["id-115", "id-131", "id-136", "id-146", "id-167"]
        );
    }

    [Fact]
    public async Task Query_Test_203()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner eq false)&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_204()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner eq true)&$skip=5",
            100,
            "$filter=not(bestPictureWinner eq true)&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_205()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner ne false)&$skip=5",
            100,
            "$filter=not(bestPictureWinner ne false)&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_206()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=not(bestPictureWinner ne true)&$skip=5",
            33,
            null,
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_207()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating eq 'Unrated'&$skip=5",
            69,
            null,
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [Fact]
    public async Task Query_Test_208()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating eq 'R'&$skip=5",
            90,
            null,
            null,
            ["id-009", "id-014", "id-017", "id-019", "id-022"]
        );
    }

    [Fact]
    public async Task Query_Test_209()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=rating ne 'PG13'&$skip=5",
            100,
            "$filter=rating ne 'PG13'&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_210()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate eq cast(1994-10-14,Edm.Date)&$skip=5",
            0,
            null,
            null,
            []);
    }

    [Fact]
    public async Task Query_Test_211()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate ge cast(1999-12-31,Edm.Date)&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [Fact]
    public async Task Query_Test_212()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate gt cast(1999-12-31,Edm.Date)&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [Fact]
    public async Task Query_Test_213()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=5",
            100,
            "$filter=releaseDate le cast(2000-01-01,Edm.Date)&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_214()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=5",
            100,
            "$filter=releaseDate lt cast(2000-01-01,Edm.Date)&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_215()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=round(duration div 60.0) eq 2&$skip=5",
            100,
            "$filter=round(duration div 60.0) eq 2&$skip=105",
            null,
            ["id-013", "id-014", "id-015", "id-016", "id-017"]
        );
    }

    [Fact]
    public async Task Query_Test_217()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=startswith(tolower(title), 'the')&$skip=5",
            58,
            null,
            null,
            ["id-008", "id-012", "id-017", "id-020", "id-023"]
        );
    }

    [Fact]
    public async Task Query_Test_218()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=startswith(toupper(title), 'THE')&$skip=5",
            58,
            null,
            null,
            ["id-008", "id-012", "id-017", "id-020", "id-023"]
        );
    }

    [Fact]
    public async Task Query_Test_219()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year eq 1994&$skip=5",
            0,
            null,
            null,
            []);
    }

    [Fact]
    public async Task Query_Test_220()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year ge 2000 and year le 2009&$skip=5",
            50,
            null,
            null,
            ["id-032", "id-042", "id-050", "id-051", "id-058"]
        );
    }

    [Fact]
    public async Task Query_Test_221()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year ge 2000&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [Fact]
    public async Task Query_Test_222()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year gt 1999 and year lt 2010&$skip=5",
            50,
            null,
            null,
            ["id-032", "id-042", "id-050", "id-051", "id-058"]
        );
    }

    [Fact]
    public async Task Query_Test_223()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year gt 1999&$skip=5",
            64,
            null,
            null,
            ["id-020", "id-032", "id-033", "id-042", "id-050"]
        );
    }

    [Fact]
    public async Task Query_Test_224()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year le 2000&$skip=5",
            100,
            "$filter=year le 2000&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_225()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year lt 2001&$skip=5",
            100,
            "$filter=year lt 2001&$skip=105",
            null,
            ["id-005", "id-007", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_226()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$filter=year(releaseDate) eq 1994&$skip=5",
            1,
            null,
            null,
            ["id-217"]
        );
    }

    [Fact]
    public async Task Query_Test_227()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=bestPictureWinner asc&$skip=5",
            100,
            "$orderby=bestPictureWinner asc&$skip=105",
            null,
            ["id-009", "id-010", "id-012", "id-013", "id-014"]
        );
    }

    [Fact]
    public async Task Query_Test_228()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=bestPictureWinner desc&$skip=5",
            100,
            "$orderby=bestPictureWinner desc&$skip=105",
            null,
            ["id-018", "id-023", "id-024", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_229()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=duration asc&$skip=5",
            100,
            "$orderby=duration asc&$skip=105",
            null,
            ["id-238", "id-201", "id-115", "id-229", "id-181"]
        );
    }

    [Fact]
    public async Task Query_Test_230()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=duration desc&$skip=5",
            100,
            "$orderby=duration desc&$skip=105",
            null,
            ["id-007", "id-183", "id-063", "id-202", "id-130"]
        );
    }

    [Fact]
    public async Task Query_Test_231()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=rating asc&$skip=5",
            100,
            "$orderby=rating asc&$skip=105",
            null,
            ["id-040", "id-041", "id-044", "id-046", "id-049"]
        );
    }

    [Fact]
    public async Task Query_Test_232()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=rating desc&$skip=5",
            100,
            "$orderby=rating desc&$skip=105",
            null,
            ["id-009", "id-014", "id-017", "id-019", "id-022"]
        );
    }

    [Fact]
    public async Task Query_Test_233()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=releaseDate asc&$skip=5",
            100,
            "$orderby=releaseDate asc&$skip=105",
            null,
            ["id-229", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [Fact]
    public async Task Query_Test_234()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=releaseDate desc&$skip=5",
            100,
            "$orderby=releaseDate desc&$skip=105",
            null,
            ["id-149", "id-213", "id-102", "id-155", "id-169"]
        );
    }

    [Fact]
    public async Task Query_Test_235()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=title asc&$skip=5",
            100,
            "$orderby=title asc&$skip=105",
            null,
            ["id-214", "id-102", "id-215", "id-039", "id-057"]
        );
    }

    [Fact]
    public async Task Query_Test_236()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=title desc&$skip=5",
            100,
            "$orderby=title desc&$skip=105",
            null,
            ["id-058", "id-046", "id-160", "id-092", "id-176"]
        );
    }

    [Fact]
    public async Task Query_Test_237()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc&$skip=5",
            100,
            "$orderby=year asc&$skip=105",
            null,
            ["id-088", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [Fact]
    public async Task Query_Test_238()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc,title asc&$skip=5",
            100,
            "$orderby=year asc,title asc&$skip=105",
            null,
            ["id-088", "id-224", "id-041", "id-049", "id-135"]
        );
    }

    [Fact]
    public async Task Query_Test_239()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year asc,title desc&$skip=5",
            100,
            "$orderby=year asc,title desc&$skip=105",
            null,
            ["id-088", "id-224", "id-049", "id-041", "id-135"]
        );
    }

    [Fact]
    public async Task Query_Test_240()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc&$skip=5",
            100,
            "$orderby=year desc&$skip=105",
            null,
            ["id-149", "id-186", "id-213", "id-013", "id-053"]
        );
    }

    [Fact]
    public async Task Query_Test_241()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc,title asc&$skip=5",
            100,
            "$orderby=year desc,title asc&$skip=105",
            null,
            ["id-186", "id-064", "id-149", "id-169", "id-161"]
        );
    }

    [Fact]
    public async Task Query_Test_242()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$orderby=year desc,title desc&$skip=5",
            100,
            "$orderby=year desc,title desc&$skip=105",
            null,
            ["id-186", "id-213", "id-102", "id-053", "id-155"]
        );
    }

    [Fact]
    public async Task Query_Test_243()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$skip=0",
            100,
            "$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_244()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$skip=100",
            100,
            "$skip=200",
            null,
            ["id-100", "id-101", "id-102", "id-103", "id-104"]
        );
    }

    [Fact]
    public async Task Query_Test_245()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$skip=200",
            48,
            null,
            null,
            ["id-200", "id-201", "id-202", "id-203", "id-204"]
        );
    }

    [Fact]
    public async Task Query_Test_246()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$skip=300",
            0,
            null,
            null,
            []);
    }

    [Fact]
    public async Task Query_Test_247()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=((year div 1000.5) eq 2) and (rating eq 'R')",
            2,
            null,
            null,
            ["id-061", "id-173"]
        );
    }

    [Fact]
    public async Task Query_Test_248()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=((year sub 1900) ge 80) and ((year add 10) le 2000) and (duration le 120)",
            5,
            null,
            null,
            ["id-026", "id-047", "id-081", "id-103", "id-121"]
        );
    }

    [Fact]
    public async Task Query_Test_249()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=(year div 1000.5) eq 2",
            5,
            null,
            null,
            ["id-012", "id-042", "id-061", "id-173", "id-194"]
        );
    }

    [Fact]
    public async Task Query_Test_250()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=(year ge 1930 and year le 1940) or (year ge 1950 and year le 1960)",
            5,
            null,
            null,
            ["id-005", "id-016", "id-027", "id-028", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_251()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=(year sub 1900) ge 80",
            5,
            null,
            null,
            ["id-000", "id-003", "id-006", "id-007", "id-008"]
        );
    }

    [Fact]
    public async Task Query_Test_252()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner eq false",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_253()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and ceiling(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-023", "id-024", "id-112", "id-135", "id-142"]
        );
    }

    [Fact]
    public async Task Query_Test_254()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and floor(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-001", "id-011", "id-018", "id-048", "id-051"]
        );
    }

    [Fact]
    public async Task Query_Test_255()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true and round(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-011", "id-018", "id-023", "id-024", "id-048"]
        );
    }

    [Fact]
    public async Task Query_Test_256()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner eq true",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_257()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner ne false",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_258()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=bestPictureWinner ne true",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_259()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=ceiling(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-005", "id-023", "id-024", "id-025", "id-026"]
        );
    }

    [Fact]
    public async Task Query_Test_260()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=day(releaseDate) eq 1",
            5,
            null,
            null,
            ["id-019", "id-048", "id-129", "id-131", "id-132"]
        );
    }

    [Fact]
    public async Task Query_Test_261()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=duration ge 60",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_262()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=endswith(title, 'er')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_263()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=endswith(tolower(title), 'er')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_264()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=endswith(toupper(title), 'ER')",
            5,
            null,
            null,
            ["id-001", "id-052", "id-121", "id-130", "id-164"]
        );
    }

    [Fact]
    public async Task Query_Test_265()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=floor(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-000", "id-001", "id-003", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_266()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=month(releaseDate) eq 11",
            5,
            null,
            null,
            ["id-011", "id-016", "id-030", "id-064", "id-085"]
        );
    }

    [Fact]
    public async Task Query_Test_267()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=not(bestPictureWinner eq false)",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_268()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=not(bestPictureWinner eq true)",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_269()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=not(bestPictureWinner ne false)",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_270()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=not(bestPictureWinner ne true)",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_271()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=rating eq 'R'",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_272()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=rating ne 'PG13'",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_273()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=rating eq 'Unrated'",
            5,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_274()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=releaseDate eq cast(1994-10-14,Edm.Date)",
            2,
            null,
            null,
            ["id-000", "id-003"]
        );
    }

    [Fact]
    public async Task Query_Test_275()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=releaseDate ge cast(1999-12-31,Edm.Date)",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_276()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=releaseDate gt cast(1999-12-31,Edm.Date)",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_277()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=releaseDate le cast(2000-01-01,Edm.Date)",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_278()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=releaseDate lt cast(2000-01-01,Edm.Date)",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_279()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=round(duration div 60.0) eq 2",
            5,
            null,
            null,
            ["id-000", "id-005", "id-009", "id-010", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_281()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=startswith(tolower(title), 'the')",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_282()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=startswith(toupper(title), 'THE')",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-004", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_283()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_284()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year ge 2000 and year le 2009",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_285()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year ge 2000",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_286()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year gt 1999 and year lt 2010",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-019", "id-020"]
        );
    }

    [Fact]
    public async Task Query_Test_287()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year gt 1999",
            5,
            null,
            null,
            ["id-006", "id-008", "id-012", "id-013", "id-019"]
        );
    }

    [Fact]
    public async Task Query_Test_288()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year le 2000",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_289()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year lt 2001",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004"]
        );
    }

    [Fact]
    public async Task Query_Test_290()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$filter=year(releaseDate) eq 1994",
            5,
            null,
            null,
            ["id-000", "id-003", "id-018", "id-030", "id-079"]
        );
    }

    [Fact]
    public async Task Query_Test_291()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=bestPictureWinner asc",
            5,
            null,
            null,
            ["id-000", "id-003", "id-004", "id-005", "id-006"]
        );
    }

    [Fact]
    public async Task Query_Test_292()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=bestPictureWinner desc",
            5,
            null,
            null,
            ["id-001", "id-002", "id-007", "id-008", "id-011"]
        );
    }

    [Fact]
    public async Task Query_Test_293()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=duration asc",
            5,
            null,
            null,
            ["id-227", "id-125", "id-133", "id-107", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_294()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=duration desc",
            5,
            null,
            null,
            ["id-153", "id-065", "id-165", "id-008", "id-002"]
        );
    }

    [Fact]
    public async Task Query_Test_295()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=rating asc",
            5,
            null,
            null,
            ["id-004", "id-005", "id-011", "id-016", "id-031"]
        );
    }

    [Fact]
    public async Task Query_Test_296()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=rating desc",
            5,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task Query_Test_297()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=releaseDate asc",
            5,
            null,
            null,
            ["id-125", "id-133", "id-227", "id-118", "id-088"]
        );
    }

    [Fact]
    public async Task Query_Test_298()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=releaseDate desc",
            5,
            null,
            null,
            ["id-188", "id-033", "id-122", "id-186", "id-064"]
        );
    }

    [Fact]
    public async Task Query_Test_299()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=title asc",
            5,
            null,
            null,
            ["id-005", "id-091", "id-243", "id-194", "id-060"]
        );
    }

    [Fact]
    public async Task Query_Test_300()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=title desc",
            5,
            null,
            null,
            ["id-107", "id-100", "id-123", "id-190", "id-149"]
        );
    }

    [Fact]
    public async Task Query_Test_301()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year asc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_302()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year asc,title asc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-227", "id-133", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_303()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year asc,title desc",
            5,
            null,
            null,
            ["id-125", "id-229", "id-133", "id-227", "id-118"]
        );
    }

    [Fact]
    public async Task Query_Test_304()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year desc",
            5,
            null,
            null,
            ["id-033", "id-122", "id-188", "id-064", "id-102"]
        );
    }

    [Fact]
    public async Task Query_Test_305()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year desc,title asc",
            5,
            null,
            null,
            ["id-188", "id-122", "id-033", "id-102", "id-213"]
        );
    }

    [Fact]
    public async Task Query_Test_306()
    {
        await MovieQueryTest(
            $"{this.factory.MovieEndpoint}?$top=5&$orderby=year desc,title desc",
            5,
            null,
            null,
            ["id-033", "id-122", "id-188", "id-149", "id-064"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_002()
    {
        this.factory.SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await MovieQueryTest(
            $"{this.factory.SoftDeletedMovieEndpoint}?$count=true",
            100,
            "$count=true&$skip=100",
            153,
            ["id-004", "id-005", "id-006", "id-008", "id-010"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_003()
    {
        this.factory.SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await MovieQueryTest(
            $"{this.factory.SoftDeletedMovieEndpoint}?$filter=deleted eq false&__includedeleted=true",
            100,
            "$filter=deleted eq false&__includedeleted=true&$skip=100",
            null,
            ["id-004", "id-005", "id-006", "id-008", "id-010"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_004()
    {
        this.factory.SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await MovieQueryTest(
            $"{this.factory.SoftDeletedMovieEndpoint}?$filter=deleted eq true&__includedeleted=true",
            95,
            null,
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-007"]
        );
    }

    [Fact]
    public async Task SoftDeleteQueryTest_005()
    {
        this.factory.SoftDelete<InMemoryMovie>(x => x.Rating == MovieRating.R);
        await MovieQueryTest(
            $"{this.factory.SoftDeletedMovieEndpoint}?__includedeleted=true",
            100,
            "__includedeleted=true&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_010()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$orderby=id&$filter=(second(timeOnlyValue) eq 0)",
            100,
            "$orderby=id&$filter=(second(timeOnlyValue) eq 0)&$skip=100",
            null,
            ["id-000", "id-001", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_011()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$orderby=id&$filter=(hour(timeOnlyValue) eq 3)",
            31,
            null,
            null,
            ["id-059", "id-060", "id-061", "id-062", "id-063", "id-064"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_012()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$orderby=id&$filter=(minute(timeOnlyValue) eq 21)",
            12,
            null,
            null,
            ["id-020", "id-051", "id-079", "id-110", "id-140", "id-171"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_013()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=(hour(timeOnlyValue) le 12)",
            100,
            "$count=true&$orderby=id&$filter=(hour(timeOnlyValue) le 12)&$skip=100",
            365,
            ["id-000", "id-001", "id-002", "id-003", "id-004", "id-005"]
        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_014()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=timeOnlyValue eq 02:14:00",
            1,
            null,
            1,
            ["id-044"]

        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_015()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=timeOnlyValue ge 12:15:00",
            17,
            null,
            17,
            ["id-348", "id-349", "id-350", "id-351"]

        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_016()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=timeOnlyValue gt 12:15:00",
            16,
            null,
            16,
            ["id-349", "id-350", "id-351", "id-352"]

        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_017()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=timeOnlyValue le cast(07:14:00,Edm.TimeOfDay)",
            100,
            "$count=true&$orderby=id&$filter=timeOnlyValue le cast(07:14:00,Edm.TimeOfDay)&$skip=100",
            195,
            ["id-000", "id-001", "id-002", "id-003"]

        );
    }

    [Fact]
    public async Task KitchSinkQueryTest_018()
    {
        SeedKitchenSinkWithDateTimeData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$count=true&$orderby=id&$filter=timeOnlyValue lt cast(07:14:00,Edm.TimeOfDay)",
            100,
            "$count=true&$orderby=id&$filter=timeOnlyValue lt cast(07:14:00,Edm.TimeOfDay)&$skip=100",
            194,
            ["id-000", "id-001", "id-002", "id-003"]
        );
    }

    [Fact]
    public async Task KitchenSinkQueryTest_019()
    {
        SeedKitchenSinkWithCountryData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$filter=geo.distance(pointValue, geography'POINT(-97 38)') lt 0.2",
            1,
            null,
            null,
            ["US"]
        );
    }

    [Fact]
    public async Task KitchenSinkQueryTest_020()
    {
        SeedKitchenSinkWithCountryData();
        await KitchenSinkQueryTest(
            $"{this.factory.KitchenSinkEndpoint}?$filter=id in ( 'IT', 'GR', 'EG' )",
            3,
            null,
            null,
            ["EG", "GR", "IT"]
        );
    }

    [Fact]
    public async Task Paging_Test_1()
    {
        await PagingTest(this.factory.MovieEndpoint, 248, 3);
    }

    [Fact]
    public async Task Paging_Test_2()
    {
        await PagingTest($"{this.factory.PagedMovieEndpoint}?$top=100", 100, 4);
    }

    [Fact]
    public async Task Paging_Test_3()
    {
        await PagingTest($"{this.factory.MovieEndpoint}?$count=true", 248, 3);
    }

    [Fact]
    public async Task Paging_Test_4()
    {
        await PagingTest($"{this.factory.MovieEndpoint}?$top=50&$count=true", 50, 1);
    }

    [Fact]
    public async Task Paging_Test_5()
    {
        await PagingTest($"{this.factory.MovieEndpoint}?$filter=releaseDate ge cast(1960-01-01,Edm.Date)&orderby=releaseDate asc", 186, 2);
    }

    [Fact]
    public async Task Paging_Test_6()
    {
        await PagingTest($"{this.factory.MovieEndpoint}?$filter=releaseDate ge cast(1960-01-01,Edm.Date)&orderby=releaseDate asc&$top=20", 20, 1);
    }

    /// <summary>
    /// We do a bunch of tests for select by dealing with the overflow properties capabilities within
    /// System.Text.Json - we are not interested in the search (we're doing the same thing over and
    /// over).  Instead, we are ensuring the right selections are made.
    /// </summary>
    [Theory, PairwiseData]
    public async Task SelectQueryTest(bool sId, bool sUpdatedAt, bool sVersion, bool sDeleted, bool sBPW, bool sduration, bool srating, bool sreleaseDate, bool stitle, bool syear)
    {
        List<string> selection = [];
        selection.AddIf(sId, "id");
        selection.AddIf(sUpdatedAt, "updatedAt");
        selection.AddIf(sVersion, "version");
        selection.AddIf(sDeleted, "deleted");
        selection.AddIf(sBPW, "bestPictureWinner");
        selection.AddIf(sduration, "duration");
        selection.AddIf(srating, "rating");
        selection.AddIf(sreleaseDate, "releaseDate");
        selection.AddIf(stitle, "title");
        selection.AddIf(syear, "year");

        if (selection.Count == 0)
        {
            return; // Ignore this test
        }

        string query = $"{this.factory.MovieEndpoint}?$top=5&$skip=5&$select={string.Join(',', selection)}";

        HttpResponseMessage response = await this.client.GetAsync(query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        PageOfItems<ClientObject> result = JsonSerializer.Deserialize<PageOfItems<ClientObject>>(content, this.serializerOptions);
        result.Should().NotBeNull();
        result.Items.Should().NotBeNullOrEmpty();
        foreach (ClientObject item in result.Items)
        {
            List<string> keys = [.. item.Data.Keys];
            keys.Should().BeEquivalentTo(selection);
        }
    }

    #region Base Tests
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
        HttpResponseMessage response = await this.client.GetAsync(pathAndQuery);
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PageOfItems<ClientMovie> result = JsonSerializer.Deserialize<PageOfItems<ClientMovie>>(content, this.serializerOptions);

        // Payload has the right content
        result?.Items?.Length.Should().Be(itemCount);
        result.NextLink.Should().MatchQueryString(nextLinkQuery);
        result.Count.Should().Be(totalCount);

        // The first n items must match what is expected
        result.Items.Take(firstItems.Length).Select(m => m.Id).ToList().Should().ContainInConsecutiveOrder(firstItems);
        for (int idx = 0; idx < firstItems.Length; idx++)
        {
            InMemoryMovie expected = this.factory.GetServerEntityById<InMemoryMovie>(firstItems[idx])!;
            result.Items[idx].Should().HaveEquivalentMetadataTo(expected).And.BeEquivalentTo<IMovie>(expected);
        }
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
    private async Task KitchenSinkQueryTest(string pathAndQuery, int itemCount, string nextLinkQuery, int? totalCount, string[] firstItems)
    {
        HttpResponseMessage response = await this.client.GetAsync(pathAndQuery);
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PageOfItems<ClientKitchenSink> result = JsonSerializer.Deserialize<PageOfItems<ClientKitchenSink>>(content, this.serializerOptions);

        // Payload has the right content
        Assert.Equal(itemCount, result!.Items!.Length);
        Assert.Equal(nextLinkQuery, result.NextLink == null ? null : Uri.UnescapeDataString(result.NextLink));
        Assert.Equal(totalCount, result.Count);

        // The first n items must match what is expected
        Assert.True(result.Items.Length >= firstItems.Length);
        Assert.Equal(firstItems, result.Items.Take(firstItems.Length).Select(m => m.Id).ToArray());
        for (int idx = 0; idx < firstItems.Length; idx++)
        {
            InMemoryKitchenSink expected = this.factory.GetServerEntityById<InMemoryKitchenSink>(firstItems[idx])!;
            result.Items[idx].Should().HaveEquivalentMetadataTo(expected).And.BeEquivalentTo<IKitchenSink>(expected);
        }
    }

    /// <summary>
    /// Tests the paging capability of the query table endpoint.
    /// </summary>
    /// <param name="startQuery">The starting query.</param>
    /// <param name="expectedCount">The total expected number of records, after paging is complete.</param>
    /// <param name="expectedLoops">The number of loops expected.</param>
    /// <returns>A task that completes when the test is complete.</returns>
    private async Task PagingTest(string startQuery, int expectedCount, int expectedLoops)
    {
        string path = startQuery.Contains('?') ? startQuery[..(startQuery.IndexOf('?'))] : startQuery;
        string query = startQuery;

        int loops = 0;
        Dictionary<string, ClientMovie> items = [];

        do
        {
            loops++;
            HttpResponseMessage response = await this.client.GetAsync(query);
            string content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            PageOfItems<ClientMovie> result = JsonSerializer.Deserialize<PageOfItems<ClientMovie>>(content, this.serializerOptions);
            result.Items.Should().NotBeNull();
            result.Items.ToList().ForEach(x => items.Add(x.Id, x));
            if (result.NextLink != null)
            {
                query = $"{path}?{result.NextLink}";
            }
            else
            {
                break;
            }
        } while (loops < expectedLoops + 2);

        items.Should().HaveCount(expectedCount);
        loops.Should().Be(expectedLoops);
    }
    #endregion
}
