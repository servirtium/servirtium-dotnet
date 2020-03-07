using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public interface IScriptWriter
    {
        void Write(TextWriter writer, IDictionary<int, IInteraction> interactions);
    }
}
