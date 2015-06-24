using System;
using Quobject.SocketIoClientDotNet.Client;

namespace DataSourceServer
{
    public class SocketIOHandler
    {
        private Socket WebServerSocket;

        public SocketIOHandler(string url)
        {
            WebServerSocket = IO.Socket(url);
            WebServerSocket.On(Socket.EVENT_CONNECT, onConnect);
            WebServerSocket.On(Socket.EVENT_CONNECT_ERROR, onError);
            WebServerSocket.On(Socket.EVENT_CONNECT_TIMEOUT, onConnectTimeout);
            WebServerSocket.On(Socket.EVENT_DISCONNECT, onDisconnect);
            WebServerSocket.On(Socket.EVENT_ERROR, onError);
            WebServerSocket.On(Socket.EVENT_MESSAGE, onMessage);
            WebServerSocket.On(Socket.EVENT_RECONNECT, onReconnect);
            WebServerSocket.On(Socket.EVENT_RECONNECT_FAILED, onReconnectFailed);
            WebServerSocket.On(Socket.EVENT_RECONNECTING, onReconnecting);
        }

        private void onConnect()
        {
            Console.WriteLine("Connected to server");
            WebServerSocket.Emit("whoami", "back");
        }

        private void onConnectTimeout()
        {
            long timout = WebServerSocket.Io().Timeout();
            Console.WriteLine("Connect timeout ({0} ms)", timout);
        }

        private void onDisconnect()
        {
            Console.WriteLine("Disconnected from server");
        }

        private void onReconnect(object data)
        {
            Console.WriteLine("Reconnected after {0} trial(s)", data);
        }

        private void onReconnecting(object data)
        {
            int max = WebServerSocket.Io().ReconnectionAttempts();
            string format = (max == int.MaxValue ? "Reconnecting - {0} trial(s)" : "Reconnecting - {0} trial(s), max: {1}");
            Console.WriteLine(format, data, max);
        }

        private void onReconnectFailed()
        {
            Console.WriteLine("Failed to reconnect server...");
        }

        private void onError(object error)
        {
            Console.WriteLine("Error: [{0}]", (error as Exception).Message);
        }

        private void onMessage(object message)
        {
            Console.WriteLine("Message: {0}", message.ToString());
        }

        public void onImageUpdated(byte[] imageByteArray)
        {
            WebServerSocket.Emit("kinect-image", imageByteArray);
        }
    }
}
