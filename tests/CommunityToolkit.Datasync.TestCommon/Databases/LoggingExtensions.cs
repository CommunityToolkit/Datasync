// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.TestCommon.Databases;

public static class LoggingExtensions
{
    private static readonly string[] categories = ["Microsoft.EntityFrameworkCore.Database.Command"];

    /// <summary>
    /// Enables the correct logging on a database context.
    /// </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <param name="current">The current database context.</param>
    /// <param name="output">The logging output helper.</param>
    /// <returns>The database context (for chaining).</returns>
    public static DbContextOptionsBuilder<TContext> EnableLogging<TContext>(this DbContextOptionsBuilder<TContext> current, ITestOutputHelper output) where TContext : DbContext
    {
        bool enableLogging = (Environment.GetEnvironmentVariable("ENABLE_SQL_LOGGING") ?? "false") == "true";
        if (output != null && enableLogging)
        {
            current
                .UseLoggerFactory(new TestLoggerFactory(output, categories))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
        }

        return current;
    }
}

/// <summary>
/// Provides an ILoggerProvider for testing purposes.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestLoggerFactory : ILoggerFactory
{
    private readonly ITestOutputHelper _output;
    private readonly string[] _categories;

    public TestLoggerFactory(ITestOutputHelper output)
    {
        this._output = output;
        this._categories = [];
    }

    public TestLoggerFactory(ITestOutputHelper output, IEnumerable<string> categories)
    {
        this._output = output;
        this._categories = categories.ToArray();
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
        => new TestLogger(categoryName, this._categories, this._output);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Nothing to do here
        }
    }

    class TestLogger(string categoryName, string[] categories, ITestOutputHelper output) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => categories.Contains(categoryName);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            output.WriteLine($"{categoryName}>> {formatter(state, exception)}");
        }

        public IDisposable BeginScope<TState>(TState state) => null;
    }
}

