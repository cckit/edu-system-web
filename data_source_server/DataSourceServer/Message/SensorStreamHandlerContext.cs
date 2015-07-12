using System;
using System.Threading.Tasks;

namespace DataSourceServer.Message
{
    public sealed class SensorStreamHandlerContext
    {
        public Func<EventMessage, Task> SendEventMessageAsync { get; private set; }

        public SensorStreamHandlerContext(Func<EventMessage, Task> sendEventMessageAsync)
        {
            this.SendEventMessageAsync = sendEventMessageAsync;
        }
    }
}
