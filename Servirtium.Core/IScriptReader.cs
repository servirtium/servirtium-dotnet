using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Servirtium.Core
{
    public interface IScriptReader
    {
        IDictionary<int, IInteraction> Read(TextReader reader);
    }
}
