using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Servirtium.Core
{
    class InteractionCounter
    {
        private long _currentInteraction = -1;

        long bump()
        {
            return Interlocked.Increment(ref _currentInteraction);
        }

        long get()
        {
            return (int)Interlocked.Read(ref _currentInteraction);
        }

        void reset()
        {
            Interlocked.Exchange(ref _currentInteraction, -1);
        }
    }
}
