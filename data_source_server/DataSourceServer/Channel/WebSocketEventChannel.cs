using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataSourceServer.Message;
using SuperWebSocket;

namespace DataSourceServer.Channel
{
    public class WebSocketEventChannel
    {
        private WebSocketSession session;

        private Action<WebSocketEventChannel> closedAction;

        private const int MaximumOverlappingTaskCount = 10;

        private Task lastSendTask;

        private int overrlappingTaskCount;

        private WebSocketEventChannel(WebSocketSession session, Action<WebSocketEventChannel> closedAction)
        {
            this.session = session;
            this.closedAction = closedAction;
        }

        private bool SerializedSendMessages(IEnumerable<WebSocketMessage> messages)
        {
            foreach (var message in messages)
            {
                bool success = false;

                if (message.Binary)
                {
                    success = session.TrySend(message.Content);
                }
                else
                {
                    success = session.TrySend(message.Message);
                }

                if (!success)
                {
                    return false;
                }
            }
            return true;
        }

        public bool SendMessage(params WebSocketMessage[] messages)
        {
            if (messages.Length == 0)
            {
                return true;
            }

            ++this.overrlappingTaskCount;

            try
            {
                if (this.overrlappingTaskCount > MaximumOverlappingTaskCount)
                {
                    throw new InvalidOperationException(@"Events are being generated faster than web socket channel can handle");
                }

                return this.SerializedSendMessages(messages);
            }
            finally
            {
                --this.overrlappingTaskCount;
            }
        }

        public static void TryOpen(WebSocketSession session,
            Action<WebSocketEventChannel> openedAction,
            Action<WebSocketEventChannel> closedAction)
        {
            if (session != null)
            {
                var channel = new WebSocketEventChannel(session, closedAction);
                openedAction(channel);
            }
        }
    }
}
