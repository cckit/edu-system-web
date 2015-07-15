using System;
using System.Threading.Tasks;
using DataSourceServer.Message.Event;
using DataSourceServer.Message.Stream;

namespace DataSourceServer.Message
{
    public sealed class SensorStreamHandlerContext
    {
        public Func<EventMessage, Task> SendEventMessageAsync { get; private set; }
        public Func<StreamMessage, Task> SendStreamMessageAsync { get; private set; }
        public Func<StreamMessage, byte[], Task> SendStreamMessageWithDataAsync { get; private set; }

        public SensorStreamHandlerContext(
            Func<EventMessage, Task> sendEventMessageAsync,
            Func<StreamMessage, byte[], Task> sendStreamMessageWithDataAsync)
        {
            this.SendEventMessageAsync = sendEventMessageAsync;
            this.SendStreamMessageAsync = (message => sendStreamMessageWithDataAsync(message, null));
            this.SendStreamMessageWithDataAsync = sendStreamMessageWithDataAsync;
        }
    }
}
