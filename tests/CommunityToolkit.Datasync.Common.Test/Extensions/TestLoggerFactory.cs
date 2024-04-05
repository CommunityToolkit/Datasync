// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CommunityToolkit.Datasync.Common.Test;

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
        this._categories = Array.Empty<string>();
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

    class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string[] _categories;
        private readonly ITestOutputHelper _output;

        public TestLogger(string categoryName, string[] categories, ITestOutputHelper output)
        {
            this._categoryName = categoryName;
            this._categories = categories;
            this._output = output;
        }

        public bool IsEnabled(LogLevel logLevel) => this._categories.Contains(this._categoryName);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this._output.WriteLine($"{this._categoryName}>> {formatter(state, exception)}");
        }

        public IDisposable BeginScope<TState>(TState state) => null;
    }
}
