using System;
using System.Collections.Generic;
using System.Text;

namespace Servirtium.Core
{
    public interface IInteraction
    {
    }

    class NoopInteraction : IInteraction { }

    public static class Interaction
    {
        public static IInteraction Noop { get; }=new NoopInteraction();
    }
}
