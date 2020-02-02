using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Servirtium.Core.Tests")]
namespace Servirtium.Core
{
    public interface IServirtiumServer
    {
        Task<IServirtiumServer> Start();

        Task Stop();

        void FinishedScript();
    }

    public class StubServirtiumServer : IServirtiumServer
    {
        public void FinishedScript()
        { }

        public Task<IServirtiumServer> Start() => Task.FromResult<IServirtiumServer>(this);

        public Task Stop() => Task.CompletedTask;
    }
}
