using Moq;
using System;

namespace Ae.Dns.Tests
{
    public abstract class MockTestClass : IDisposable
    {
        protected MockRepository Repository { get; } = new MockRepository(MockBehavior.Strict);

        public void Dispose() => Repository.VerifyAll();
    }
}
