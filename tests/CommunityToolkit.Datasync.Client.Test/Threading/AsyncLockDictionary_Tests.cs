// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Threading;

namespace CommunityToolkit.Datasync.Client.Test.Threading;

[ExcludeFromCodeCoverage]
public class AsyncLockDictionary_Tests
{
    [Fact]
    public async Task AcquireLockAsync_Works()
    {
        AsyncLockDictionary sut = new();

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();

        using (await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken))
        {
            sut.IsLocked("key1").Should().BeTrue();
            sut.IsLocked("key2").Should().BeFalse();
        }

        sut.IsLocked("key1").Should().BeFalse();
        sut.IsLocked("key2").Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_CanReacquireAfterRelease()
    {
        AsyncLockDictionary sut = new();

        using (await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken))
        {
            sut.IsLocked("key1").Should().BeTrue();
        }

        using (await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken))
        {
            sut.IsLocked("key1").Should().BeTrue();
        }

        sut.IsLocked("key1").Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_WaitsForHolder()
    {
        AsyncLockDictionary sut = new();
        IDisposable first = await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken);

        Task<IDisposable> secondTask = sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        secondTask.IsCompleted.Should().BeFalse();

        first.Dispose();
        using IDisposable second = await secondTask;
        sut.IsLocked("key1").Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLockAsync_CancelledWait_DoesNotLeakEntry()
    {
        AsyncLockDictionary sut = new();
        IDisposable holder = await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken);

        using CancellationTokenSource cts = new();
        Task<IDisposable> waiter = sut.AcquireLockAsync("key1", cts.Token);
        await cts.CancelAsync();
        await FluentActions.Awaiting(() => waiter).Should().ThrowAsync<OperationCanceledException>();

        // The holder still owns the lock, and the cancelled waiter must not keep it alive afterwards.
        sut.IsLocked("key1").Should().BeTrue();
        holder.Dispose();
        sut.IsLocked("key1").Should().BeFalse();

        // The key must be usable again (a fresh entry is created).
        using (await sut.AcquireLockAsync("key1", TestContext.Current.CancellationToken))
        {
            sut.IsLocked("key1").Should().BeTrue();
        }

        sut.IsLocked("key1").Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_ConcurrentCallers_AreMutuallyExclusive()
    {
        AsyncLockDictionary sut = new();
        const int taskCount = 16;
        const int iterations = 500;
        int inCriticalSection = 0;
        int maxObserved = 0;
        int total = 0;

        async Task Worker()
        {
            await Task.Yield();
            for (int i = 0; i < iterations; i++)
            {
                using (await sut.AcquireLockAsync("key1", CancellationToken.None))
                {
                    int now = Interlocked.Increment(ref inCriticalSection);
                    int seen;
                    while (now > (seen = Volatile.Read(ref maxObserved)))
                    {
                        _ = Interlocked.CompareExchange(ref maxObserved, now, seen);
                    }

                    total++;
                    await Task.Yield();
                    _ = Interlocked.Decrement(ref inCriticalSection);
                }
            }
        }

        Task[] workers = [.. Enumerable.Range(0, taskCount).Select(_ => Task.Run(Worker, TestContext.Current.CancellationToken))];
        await Task.WhenAll(workers);

        maxObserved.Should().Be(1);
        total.Should().Be(taskCount * iterations);
        sut.IsLocked("key1").Should().BeFalse();
    }
}
