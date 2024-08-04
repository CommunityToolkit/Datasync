// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.TestCommon.Databases;

using TestData = CommunityToolkit.Datasync.TestCommon.TestData;

namespace CommunityToolkit.Datasync.Client.Test.Paging;

[ExcludeFromCodeCoverage]
public class ConcurrentObservableCollection_Tests
{
    private readonly ConcurrentObservableCollection<InMemoryMovie> movies;
    private int collectionChangedCallCount = 0;

    public ConcurrentObservableCollection_Tests()
    {
        List<InMemoryMovie> seed = TestData.Movies.OfType<InMemoryMovie>().ToList();
        this.movies = new ConcurrentObservableCollection<InMemoryMovie>(seed);
        this.movies.CollectionChanged += (sender, e) => this.collectionChangedCallCount++;
    }

    [Fact]
    public void ReplaceAll_Null_Throws()
    {
        Action act = () => this.movies.ReplaceAll(null);
        act.Should().Throw<ArgumentNullException>();
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
        List<InMemoryMovie> data = TestData.Movies.OfType<InMemoryMovie>().Take(10).ToList();
        this.movies.ReplaceAll(data);
        this.movies.Should().HaveCount(10).And.BeEquivalentTo(data);
        this.collectionChangedCallCount.Should().Be(1);
    }

    [Fact]
    public void AddRange_Null_Throws()
    {
        Action act = () => _ = this.movies.AddRange(null);
        act.Should().Throw<ArgumentNullException>();
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
        List<InMemoryMovie> d1 = TestData.Movies.OfType<InMemoryMovie>().Take(10).ToList();
        List<InMemoryMovie> d2 = TestData.Movies.OfType<InMemoryMovie>().Skip(10).Take(10).ToList();
        this.movies.ReplaceAll(d1);

        bool actual = this.movies.AddRange(d2);
        actual.Should().BeTrue();
        this.movies.Should().HaveCount(20).And.BeEquivalentTo(d1.Concat(d2));
        this.collectionChangedCallCount.Should().Be(2);
    }

    [Fact]
    public void AddIfMissing_NullCondition_Throws()
    {
        InMemoryMovie item = this.movies.First();
        Action act = () => this.movies.AddIfMissing(null, item);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfMissing_NullItem_Throws()
    {
        Action act = () => this.movies.AddIfMissing(t => t.Id == string.Empty, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddIfMissing_False_IfMatch()
    {
        InMemoryMovie item = new() { Id = "foo" };
        int count = this.movies.Count;
        bool actual = this.movies.AddIfMissing(t => t.Id == "id-001", item);
        actual.Should().BeFalse();
        this.movies.Should().HaveCount(count);
    }

    [Fact]
    public void AddIfMissing_True_IfNoMatch()
    {
        InMemoryMovie item = new() { Id = "foo" };
        int count = this.movies.Count;
        bool actual = this.movies.AddIfMissing(t => t.Id == item.Id, item);
        actual.Should().BeTrue();
        this.movies.Should().HaveCount(count + 1);
    }

    [Fact]
    public void RemoveIf_Null_Throws()
    {
        Action act = () => this.movies.RemoveIf(null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveIf_NotMatched_False()
    {
        int count = this.movies.Count;
        bool actual = this.movies.RemoveIf(t => t.Id == "foo");
        actual.Should().BeFalse();
        this.movies.Should().HaveCount(count);
    }

    [Fact]
    public void RemoveIf_Matched_True()
    {
        int count = this.movies.Count;
        bool actual = this.movies.RemoveIf(t => t.Id == "id-001");
        actual.Should().BeTrue();
        this.movies.Should().HaveCount(count - 1);
        Assert.True(actual);
        Assert.Equal(count - 1, this.movies.Count);
    }

    [Fact]
    public void ReplaceIf_NullCondition_Throws()
    {
        InMemoryMovie item = this.movies.First();
        Action act = () => this.movies.ReplaceIf(null, item);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReplaceIf_NullItem_Throws()
    {
        Action act = () => this.movies.ReplaceIf(t => t.Id == string.Empty, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReplaceIf_True_IfMatch()
    {
        InMemoryMovie item = new() { Id = "foo" };
        int count = this.movies.Count;
        bool actual = this.movies.ReplaceIf(t => t.Id == "id-001", item);
        actual.Should().BeTrue();
        this.movies.Should().HaveCount(count).And.Contain(item);
    }

    [Fact]
    public void ReplaceIf_False_IfNoMatch()
    {
        InMemoryMovie item = new() { Id = "foo" };
        int count = this.movies.Count;
        bool actual = this.movies.ReplaceIf(t => t.Id == item.Id, item);
        actual.Should().BeFalse();
        this.movies.Should().HaveCount(count).And.NotContain(item);
    }
}