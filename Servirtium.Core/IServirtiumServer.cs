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
}
