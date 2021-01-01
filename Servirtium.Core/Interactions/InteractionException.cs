using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Core.Interactions
{
    public abstract class InteractionException : Exception
    {
        public InteractionException(string message, Exception inner) : base(message, inner) { }
    }
    public class PlaybackException : InteractionException
    {
        public PlaybackException(string message, Exception inner) : base(message, inner) { }
    }
    public class RecordException : InteractionException
    {
        public RecordException(string message, Exception inner) : base(message, inner) { }
    }

    public class MissingInteractionException : RecordException
    {
        public MissingInteractionException(string message, int interactionNumber) : base(message, null)
        {
            MissingInteractionNumber = interactionNumber;
        }

        public int MissingInteractionNumber { get; }
    }
}
