﻿using System.Collections.Concurrent;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Test.Logging
{
    public interface ITestSink
    {
        event Action<WriteContext> MessageLogged;

        event Action<BeginScopeContext> ScopeStarted;

        Func<WriteContext, bool>? WriteEnabled { get; set; }

        Func<BeginScopeContext, bool>? BeginEnabled { get; set; }

        IProducerConsumerCollection<BeginScopeContext> Scopes { get; set; }

        IProducerConsumerCollection<WriteContext> Writes { get; set; }

        void Write(WriteContext context);

        void Begin(BeginScopeContext context);
    }
}