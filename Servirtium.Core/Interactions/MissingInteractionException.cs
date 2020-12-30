using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public class MissingInteractionException : Exception
    {
        public MissingInteractionException(string message, int interactionNumber) : base(message)
        {
            MissingInteractionNumber = interactionNumber;
        }

        public int MissingInteractionNumber { get; }
    }
}
