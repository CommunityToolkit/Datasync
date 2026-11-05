// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Threading.Tasks.Dataflow;

namespace CommunityToolkit.Datasync.Client.Threading;

/// <summary>
/// A parallel async queue runner.
/// </summary>
/// <typeparam name="T">The type of the argument to the queue runner</typeparam>
internal class QueueHandler<T> where T : class
{
    private readonly ActionBlock<T> jobs;
    private ExceptionDispatchInfo? cancellation;

    /// <summary>
    /// Creates a new <see cref="QueueHandler{T}"/>
    /// </summary>
    /// <param name="maxThreads">The maximum number of threads to use.</param>
    /// <param name="jobRunner">The job runner.</param>
    public QueueHandler(int maxThreads, Func<T, Task> jobRunner)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxThreads, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxThreads, 8);
        ArgumentNullException.ThrowIfNull(jobRunner);
        ExecutionDataflowBlockOptions options = new() { MaxDegreeOfParallelism = maxThreads };
        this.jobs = new(async item =>
        {
            try
            {
                await jobRunner(item).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                // TPL Dataflow treats an OperationCanceledException thrown by the delegate as cooperative cancellation:
                // the item is silently dropped and Completion finishes normally.  Remember the first one so that
                // WhenComplete() can surface it instead of reporting a clean run.
                _ = Interlocked.CompareExchange(ref this.cancellation, ExceptionDispatchInfo.Capture(ex), null);
            }
        }, options);
    }

    /// <summary>
    /// Enqueues a new job.
    /// </summary>
    /// <param name="item">The entity to be queued up for running.</param>
    public void Enqueue(T item)
    {
        _ = this.jobs.Post(item);
    }

    /// <summary>
    /// Enqueues a list of jobs.
    /// </summary>
    /// <param name="items">The entities to be queued up for running.</param>
    public void EnqueueRange(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            Enqueue(item);
        }
    }

    /// <summary>
    /// Returns a task that resolves when all the runners are completed.
    /// </summary>
    /// <returns>A task that completes when all jobs are done.</returns>
    /// <exception cref="OperationCanceledException">If a job was canceled.</exception>
    public async Task WhenComplete()
    {
        this.jobs.Complete();
        await this.jobs.Completion.ConfigureAwait(false);
        this.cancellation?.Throw();
    }

    /// <summary>
    /// The number of items still to be processed.
    /// </summary>
    public int Count { get => this.jobs.InputCount; }
}
