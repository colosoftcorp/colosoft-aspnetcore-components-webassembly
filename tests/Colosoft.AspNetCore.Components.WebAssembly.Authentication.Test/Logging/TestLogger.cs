using Microsoft.Extensions.Logging;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Test.Logging
{
    public class TestLogger : ILogger
    {
        private readonly ITestSink sink;
        private readonly string? name;
        private readonly Func<LogLevel, bool> filter;
        private object? scope;

        public TestLogger(string name, ITestSink sink, bool enabled)
            : this(name, sink, _ => enabled)
        {
        }

        public TestLogger(string name, ITestSink sink, Func<LogLevel, bool> filter)
        {
            this.sink = sink;
            this.name = name;
            this.filter = filter;
        }

        public string? Name { get; set; }
        public int IsEnabledCallCount { get; private set; }

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
        public IDisposable? BeginScope<TState>(TState state)
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
        {
            this.scope = state;

            this.sink.Begin(new BeginScopeContext()
            {
                LoggerName = this.name!,
                Scope = state!,
            });

            return TestDisposable.Instance;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
            {
                return;
            }

            this.sink.Write(new WriteContext()
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state!,
                Exception = exception!,
                Formatter = (s, e) => formatter((TState)s, e),
                LoggerName = this.name!,
                Scope = this.scope!,
            });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            this.IsEnabledCallCount++;
            return logLevel != LogLevel.None && this.filter(logLevel);
        }

        private sealed class TestDisposable : IDisposable
        {
            public static readonly TestDisposable Instance = new TestDisposable();

            public void Dispose()
            {
                // intentionally does nothing
            }
        }
    }
}