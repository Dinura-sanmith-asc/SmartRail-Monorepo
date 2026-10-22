using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmartRail.IntegrationTests.Identity;

public sealed class IdentityTestLogCaptureProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _messages = new();
    public IReadOnlyCollection<string> Messages => _messages.ToArray();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(_messages, categoryName);
    public void Dispose() { }

    private sealed class CaptureLogger(ConcurrentQueue<string> messages, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var text = $"{category} {formatter(state, exception)} {exception?.Message}";
            messages.Enqueue(text);
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static NoopScope Instance { get; } = new();
        public void Dispose() { }
    }
}
