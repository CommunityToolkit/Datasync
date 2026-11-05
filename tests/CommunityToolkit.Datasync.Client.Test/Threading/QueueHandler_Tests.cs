// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;
using System.Collections.Concurrent;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class QueueHandler_Tests
{
    /// <summary>
    /// The delay each job takes.  This is an async delay so that a job does not hold a thread-pool thread, which makes
    /// the amount of concurrency depend only on <c>MaxDegreeOfParallelism</c> and not on thread-pool thread injection.
    /// </summary>
    private static readonly TimeSpan JobDelay = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Tracks the number of jobs running at the same time, and the peak.
    /// </summary>
    private sealed class ConcurrencyTracker
    {
        private int current;
        private int peak;

        public int Peak => Volatile.Read(ref this.peak);

        public void Enter()
        {
            int now = Interlocked.Increment(ref this.current);
            int seen;
            while (now > (seen = Volatile.Read(ref this.peak)))
            {
                if (Interlocked.CompareExchange(ref this.peak, now, seen) == seen)
                {
                    break;
                }
            }
        }

        public void Exit() => Interlocked.Decrement(ref this.current);
    }

    [Theory(Timeout = 30000)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task QueueHandler_WithThreads_Enqueue(int nThreads)
    {
        ConcurrentQueue<string> accId = new();
        ConcurrencyTracker tracker = new();
        QueueHandler<string> sut = new(nThreads, async (el) =>
        {
            tracker.Enter();
            try
            {
                accId.Enqueue(el);
                await Task.Delay(JobDelay);
            }
            finally
            {
                tracker.Exit();
            }
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

        // Check everything ran, and that concurrency was bounded by (and, for more than 1 thread, actually used)
        // the requested degree of parallelism.
        accId.Should().HaveCount(nElements);
        AssertConcurrency(tracker, nThreads);
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
        ConcurrencyTracker tracker = new();
        QueueHandler<string> sut = new(nThreads, async (el) =>
        {
            tracker.Enter();
            try
            {
                accId.Enqueue(el);
                await Task.Delay(JobDelay);
            }
            finally
            {
                tracker.Exit();
            }
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
        AssertConcurrency(tracker, nThreads);
        (endTime - startTime).TotalSeconds.Should().BeLessThanOrEqualTo(2 * (nElements / nThreads) + 5);
    }

    private static void AssertConcurrency(ConcurrencyTracker tracker, int nThreads)
    {
        // MaxDegreeOfParallelism is an upper bound, so the peak can never exceed it.
        tracker.Peak.Should().BeLessThanOrEqualTo(nThreads);
        if (nThreads > 1)
        {
            // Jobs must have overlapped at least once; exact thread counts are not asserted because they
            // depend on the thread pool and are not deterministic.
            tracker.Peak.Should().BeGreaterThan(1);
        }
    }

    [Fact(Timeout = 30000)]
    public async Task QueueHandler_JobCanceled_WhenCompleteThrows()
    {
        ConcurrentQueue<string> processed = new();
        QueueHandler<string> sut = new(1, el =>
        {
            if (el == "el-1")
            {
                throw new TaskCanceledException("simulated HttpClient timeout");
            }

            processed.Enqueue(el);
            return Task.CompletedTask;
        });

        sut.EnqueueRange(["el-0", "el-1", "el-2"]);

        Func<Task> act = sut.WhenComplete;
        await act.Should().ThrowAsync<OperationCanceledException>();
        processed.Should().BeEquivalentTo(["el-0", "el-2"]);
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
