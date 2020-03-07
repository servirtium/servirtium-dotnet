using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public interface IScriptReader
    {
        IDictionary<int, IInteraction> Read(TextReader reader);
    }
}
