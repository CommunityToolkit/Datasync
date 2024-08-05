// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Datasync.Client.Paging;

/// <summary>
/// An abstraction layer for the <see cref="SynchronizationContext"/> that we use
/// for mocking out context calls.
/// </summary>
internal interface ISynchronizationContext
{
    bool IsCurrentContext();
    void Send(SendOrPostCallback callback, object? state);
}

/// <summary>
/// A concrete implementation of the <see cref="ISynchronizationContext"/> that handles 
/// a real synchronization context.
/// </summary>
[ExcludeFromCodeCoverage]
internal class SynchronizationContextAdapter(SynchronizationContext context) : ISynchronizationContext
{
    public bool IsCurrentContext()
        => SynchronizationContext.Current == context;

    public void Send(SendOrPostCallback callback, object? state)
        => context.Send(callback, state);
}
