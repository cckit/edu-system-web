using System;
using SuperWebSocket;

namespace DataSourceServer
{
    public class DataSourceWebSocketServer
    {
        private WebSocketServer webSocketServer;
        private RequestRouter requestRouter;
        private int port;

        public DataSourceWebSocketServer(RequestRouter requestRouter, int port)
        {
            this.webSocketServer = new WebSocketServer();
            this.requestRouter = requestRouter;
            this.port = port;
        }

        public void Start()
        {
            if (!webSocketServer.Setup(port))
            {
                Console.WriteLine("Failed to setup!");
                Console.ReadKey();
                return;
            }

            webSocketServer.NewMessageReceived += webSocketServer_NewMessageReceived;
            webSocketServer.NewDataReceived += webSocketServer_NewDataReceived;
            webSocketServer.NewSessionConnected += webSocketServer_NewSessionConnected;

            if (!webSocketServer.Start())
            {
                Console.WriteLine("Failed to start!");
                Console.ReadKey();
                return;
            }
        }

        public void Stop()
        {
            webSocketServer.Stop();
        }

        void webSocketServer_NewMessageReceived(WebSocketSession session, string message)
        {
            requestRouter.NewMessage(session, message);
        }

        void webSocketServer_NewSessionConnected(WebSocketSession session)
        {
            requestRouter.NewRequest(session);
        }

        void webSocketServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
