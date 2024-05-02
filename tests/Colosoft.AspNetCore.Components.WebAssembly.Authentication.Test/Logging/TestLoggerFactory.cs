using Microsoft.Extensions.Logging;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Test.Logging
{
    public sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly ITestSink sink;
        private readonly bool enabled;

        public TestLoggerFactory(ITestSink sink, bool enabled)
        {
            this.sink = sink;
            this.enabled = enabled;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, this.sink, this.enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
            // ignore
        }
    }
}