using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CustomerStoreApi.Tests.Fakes;

/// <summary>
/// Defines a logger that records the formatted messages it receives.
/// </summary>
/// <typeparam name="T">The category type.</typeparam>
public sealed class ListLogger<T> : ILogger<T>
{
    /// <summary>
    /// Gets the recorded messages (including exception messages).
    /// </summary>
    public IList<string> Messages { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        this.Messages.Add($"{logLevel}: {formatter(state, exception)} {exception}");
    }
}
