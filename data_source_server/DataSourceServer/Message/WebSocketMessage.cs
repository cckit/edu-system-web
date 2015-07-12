using System;

namespace DataSourceServer.Message
{
    public class WebSocketMessage
    {
        public string Message { get; private set; }
        public ArraySegment<byte> Content { get; private set; }
        public bool Binary { get; private set; }

        public WebSocketMessage(string message)
        {
            this.Message = message;
            this.Binary = false;
        }

        public WebSocketMessage(ArraySegment<byte> messageContent)
        {
            this.Content = messageContent;
            this.Binary = true;
        }
    }
}
