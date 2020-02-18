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

        void MakeNote(string title, string note);

        void MakeCodeNote(string title, string note);
    }

    public class StubServirtiumServer : IServirtiumServer
    {
        public void FinishedScript()
        { }

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
