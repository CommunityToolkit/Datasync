// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Common.Test;
using CommunityToolkit.Datasync.Common.Test.Models;

// ToList() is more readable in this case.
#pragma warning disable IDE0305 // Simplify collection initialization

namespace CommunityToolkit.Datasync.Client.Test.Models;

[ExcludeFromCodeCoverage]
public class ConcurrentObservableCollection_Tests
{
    private readonly ConcurrentObservableCollection<EntityMovie> movies;
    private int collectionChangedCallCount = 0;

    public ConcurrentObservableCollection_Tests()
    {
        List<EntityMovie> seed = Movies.OfType<EntityMovie>().ToList();
        this.movies = new ConcurrentObservableCollection<EntityMovie>(seed);
        this.movies.CollectionChanged += (sender, e) => this.collectionChangedCallCount++;
    }

    [Fact]
    public void Ctor_Creates_EmptyCollection()
    {
        ConcurrentObservableCollection<EntityMovie> collection = [];
        collection.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceAll_Empty_Clears()
    {
        this.movies.ReplaceAll([]);
        this.movies.Should().BeEmpty();
        this.collectionChangedCallCount.Should().Be(1);
    }

    [Fact]
    public void ReplaceAll_List_SetsList()
    {
        List<EntityMovie> data = Movies.OfType<EntityMovie>().Take(10).ToList();
        this.movies.ReplaceAll(data);
        this.movies.Should().HaveCount(10);
        this.collectionChangedCallCount.Should().Be(1);
    }

    [Fact]
    public void AddRange_Empty_ReturnsFalse()
    {
        bool actual = this.movies.AddRange([]);
        actual.Should().BeFalse();
        this.collectionChangedCallCount.Should().Be(0);
    }

    [Fact]
    public void AddRange_Sequence_ReturnsTrue()
    {
        this.movies.ReplaceAll(Movies.OfType<EntityMovie>().Take(10));

        bool actual = this.movies.AddRange(Movies.OfType<EntityMovie>().Skip(10).Take(10));
        actual.Should().BeTrue();
        this.movies.Should().HaveCount(20);
        this.collectionChangedCallCount.Should().Be(2);
    }

    [Fact]
    public void AddIfMissing_NullCondition_Throws()
    {
        EntityMovie item = this.movies.First();

        Action act = () => this.movies.AddIfMissing(null, item);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfMissing_NullItem_Throws()
    {
        EntityMovie item = this.movies.First();

        Action act = () => this.movies.AddIfMissing(t => t.Id == item.Id, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfMissing_False_IfMatch()
    {
        EntityMovie item = new() { Id = "foo" };
        int cnt = this.movies.Count;
        bool actual = this.movies.AddIfMissing(t => t.Id == "id-001", item);

        actual.Should().BeFalse();
        this.movies.Should().HaveCount(cnt);
    }

    [Fact]
    public void AddIfMissing_True_IfNoMatch()
    {
        EntityMovie item = new() { Id = "foo" };
        int cnt = this.movies.Count;
        bool actual = this.movies.AddIfMissing(t => t.Id == item.Id, item);

        actual.Should().BeTrue();
        this.movies.Should().HaveCount(cnt + 1);
    }

    [Fact]
    public void RemoveIf_NotMatched_False()
    {
        int cnt = this.movies.Count;
        bool actual = this.movies.RemoveIf(t => t.Id == "foo");

        actual.Should().BeFalse();
        this.movies.Should().HaveCount(cnt);
    }

    [Fact]
    public void RemoveIf_Matched_True()
    {
        int cnt = this.movies.Count;
        bool actual = this.movies.RemoveIf(t => t.Id == "id-001");

        actual.Should().BeTrue();
        this.movies.Should().HaveCount(cnt - 1);
    }

    [Fact]
    public void ReplaceIf_NullItem_Throws()
    {
        EntityMovie item = this.movies.First();
        Action act = () => this.movies.ReplaceIf(t => t.Id == item.Id, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReplaceIf_NullCondition_Throws()
    {
        EntityMovie item = this.movies.First();
        Action act = () => this.movies.ReplaceIf(null, item);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReplaceIf_True_IfMatch()
    {
        EntityMovie item = new() { Id = "foo" };
        int cnt = this.movies.Count;
        bool actual = this.movies.ReplaceIf(t => t.Id == "id-001", item);

        actual.Should().BeTrue();
        this.movies.Should().HaveCount(cnt).And.Contain(item);
    }

    [Fact]
    public void ReplaceIf_False_IfNoMatch()
    {
        EntityMovie item = new() { Id = "foo" };
        int cnt = this.movies.Count;
        bool actual = this.movies.ReplaceIf(t => t.Id == item.Id, item);

        actual.Should().BeFalse();
        this.movies.Should().HaveCount(cnt).And.NotContain(item);
    }
}
