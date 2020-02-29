using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Servirtium.Core.Tests")]
namespace Servirtium.Core
{
    public interface IServirtiumServer
    {
        Task<IServirtiumServer> Start();

        Task Stop();

        void FinishedScript()
        {
            InternalRequestHandler.FinishedScript();
        }

        void MakeNote(string title, string note);

        void MakeCodeNote(string title, string note);

        protected IServirtiumRequestHandler InternalRequestHandler { get; }
    }

    public class StubServirtiumServer : IServirtiumServer
    {
        IServirtiumRequestHandler IServirtiumServer.InternalRequestHandler => throw new NotImplementedException();

        public void MakeCodeNote(string title, string note)
        {
            throw new NotImplementedException();
        }

        public void MakeNote(string title, string note)
        {
            throw new NotImplementedException();
        }

        public Task<IServirtiumServer> Start() => Task.FromResult<IServirtiumServer>(this);

        public Task Stop() => Task.CompletedTask;

    }
}
