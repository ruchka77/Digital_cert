using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigitalCertHelper.Logging;

/// <summary>
/// Creates the real logging system on a background thread, so that the window can open without waiting for
/// Serilog to load or for the log folder to be prepared. Writing a log entry waits only if that is still unfinished.
/// </summary>
public sealed class BackgroundLoggerFactory : ILoggerFactory
{
    private readonly Task<ILoggerFactory> _inner;

    public BackgroundLoggerFactory(Func<ILoggerFactory> create) =>
        _inner = Task.Run(() =>
        {
            try
            {
                return create();
            }
            catch (Exception)
            {
                // Logging must never prevent the application from working.
                return NullLoggerFactory.Instance;
            }
        });

    public ILogger CreateLogger(string categoryName) => new DeferredLogger(_inner, categoryName);

    public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

    /// <summary>Flushes and closes the log file.</summary>
    public void Dispose() => _inner.GetAwaiter().GetResult().Dispose();

    private sealed class DeferredLogger(Task<ILoggerFactory> factory, string category) : ILogger
    {
        private ILogger? _logger;

        private ILogger Resolve() => _logger ??= factory.GetAwaiter().GetResult().CreateLogger(category);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => Resolve().BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => Resolve().IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Resolve().Log(logLevel, eventId, state, exception, formatter);
    }
}
