using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Servirtium.Core
{
    public class InteractionCounter
    {
        private long _currentInteraction = -1;

        public int Bump()
        {
            return (int)Interlocked.Increment(ref _currentInteraction);
        }

        public int Get()
        {
            return (int)Interlocked.Read(ref _currentInteraction);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _currentInteraction, -1);
        }
    }
}
