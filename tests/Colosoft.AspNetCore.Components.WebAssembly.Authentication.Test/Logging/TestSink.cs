using System.Collections.Concurrent;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Test.Logging
{
    public class TestSink : ITestSink
    {
        private ConcurrentQueue<BeginScopeContext> scopes;
        private ConcurrentQueue<WriteContext> writes;

        public TestSink(
            Func<WriteContext, bool>? writeEnabled = null,
            Func<BeginScopeContext, bool>? beginEnabled = null)
        {
            this.WriteEnabled = writeEnabled;
            this.BeginEnabled = beginEnabled;

            this.scopes = new ConcurrentQueue<BeginScopeContext>();
            this.writes = new ConcurrentQueue<WriteContext>();
        }

        public static bool EnableWithTypeName<T>(WriteContext context)
        {
            return context.LoggerName!.Equals(typeof(T).FullName);
        }

        public static bool EnableWithTypeName<T>(BeginScopeContext context)
        {
            return context.LoggerName!.Equals(typeof(T).FullName);
        }

        public Func<WriteContext, bool>? WriteEnabled { get; set; }

        public Func<BeginScopeContext, bool>? BeginEnabled { get; set; }

        public IProducerConsumerCollection<BeginScopeContext> Scopes { get => this.scopes; set => this.scopes = new ConcurrentQueue<BeginScopeContext>(value); }

        public IProducerConsumerCollection<WriteContext> Writes { get => this.writes; set => this.writes = new ConcurrentQueue<WriteContext>(value); }

        public event Action<WriteContext>? MessageLogged;

        public event Action<BeginScopeContext>? ScopeStarted;

        public void Write(WriteContext context)
        {
            if (this.WriteEnabled == null || this.WriteEnabled(context))
            {
                this.writes.Enqueue(context);
            }

            this.MessageLogged?.Invoke(context);
        }

        public void Begin(BeginScopeContext context)
        {
            if (this.BeginEnabled == null || this.BeginEnabled(context))
            {
                this.scopes.Enqueue(context);
            }

            this.ScopeStarted?.Invoke(context);
        }
    }
}
