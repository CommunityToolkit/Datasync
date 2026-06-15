// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Datasync.Server.Test;

/// <summary>
/// A simple <see cref="ILogger"/> implementation that captures every log entry so that
/// tests can assert on the level and rendered message of each log statement.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class CapturingLogger : ILogger
{
    /// <summary>
    /// The list of captured log entries, in the order in which they were written.
    /// </summary>
    public List<LogEntry> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    /// <summary>
    /// A single captured log entry.
    /// </summary>
    /// <param name="LogLevel">The level the entry was logged at.</param>
    /// <param name="Message">The rendered log message.</param>
    public sealed record LogEntry(LogLevel LogLevel, string Message);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
