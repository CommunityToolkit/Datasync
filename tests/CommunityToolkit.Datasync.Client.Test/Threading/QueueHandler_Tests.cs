// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;
using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class QueueHandler_Tests
{
    [Theory(Timeout = 30000)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task QueueHandler_WithThreads_Enqueue(int nThreads)
    {
        ConcurrentQueue<string> accId = new();
        ConcurrentQueue<int> accTh = new();
        QueueHandler<string> sut = new(nThreads, (el) =>
        {
            accId.Enqueue(el);
            accTh.Enqueue(Environment.CurrentManagedThreadId);
            Thread.Sleep(2000);
            return Task.CompletedTask;
        });
        DateTimeOffset startTime = DateTimeOffset.Now;

        sut.Count.Should().Be(0);

        // Add in some elements
        int nElements = nThreads * 5;
        for (int i = 0; i < nElements; i++)
        {
            sut.Enqueue($"el-{i}");
        }

        sut.Count.Should().NotBe(0);

        // Now wait for completion
        await sut.WhenComplete();
        DateTimeOffset endTime = DateTimeOffset.Now;

        // Check everything ran.
        accId.Should().HaveCount(nElements);
        accTh.Should().HaveCount(nElements);
        accTh.AsEnumerable().Distinct().Should().HaveCount(nThreads);
        // This just makes sure that the amount of time is "of the right order of magnitude" since CI systems
        // are notoriously bad at correct timings.  We just don't want it to be 10x the expected time.
        (endTime - startTime).TotalSeconds.Should().BeLessThanOrEqualTo(2 * (nElements / nThreads) + 5);
    }

    [Theory(Timeout = 30000)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task QueueHandler_WithThreads_EnqueueRange(int nThreads)
    {
        ConcurrentQueue<string> accId = new();
        ConcurrentQueue<int> accTh = new();
        QueueHandler<string> sut = new(nThreads, (el) =>
        {
            accId.Enqueue(el);
            accTh.Enqueue(Environment.CurrentManagedThreadId);
            Thread.Sleep(1000);
            return Task.CompletedTask;
        });
        DateTimeOffset startTime = DateTimeOffset.Now;
        sut.Count.Should().Be(0);

        // Add in some elements
        int nElements = nThreads * 5;
        List<string> elements = Enumerable.Range(0, nElements).Select(x => $"el-{x}").ToList();
        sut.EnqueueRange(elements);
        sut.Count.Should().NotBe(0);

        // Now wait for completion
        await sut.WhenComplete();
        DateTimeOffset endTime = DateTimeOffset.Now;

        // Check everything ran.
        accId.Should().HaveCount(nElements);
        accTh.Should().HaveCount(nElements);
        accTh.AsEnumerable().Distinct().Should().HaveCount(nThreads);
        (endTime - startTime).TotalSeconds.Should().BeLessThanOrEqualTo((nElements / nThreads) + 2);
    }

    [Theory, CombinatorialData]
    public void QueueHandler_Throws_OutOfRangeMaxThreads([CombinatorialValues(-1, 0, 9, 10)] int threads)
    {
        Action act = () => _ = new QueueHandler<string>(threads, _ => Task.CompletedTask);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void QueueHandler_Throws_NullJobRunner()
    {
        Action act = () => _ = new QueueHandler<string>(1, null);
        act.Should().Throw<ArgumentNullException>();
    }
}
