using System.Globalization;
using SuperWebSocket;

namespace DataSourceServer
{
    public abstract class RequestRouter
    {
        private const string KinectEndpointBasePath = "/Kinect";
        private const string DefaultSensorName = "/default";

        private string getSubPath(WebSocketSession session)
        {
            var subPath = session.Path.ToUpperInvariant();

            if (!subPath.StartsWith(KinectEndpointBasePath, true, CultureInfo.InvariantCulture))
            {
                session.CloseWithHandshake(404, "Not Found");
            }

            subPath = subPath.Substring(KinectEndpointBasePath.Length);

            if (!subPath.StartsWith(DefaultSensorName, true, CultureInfo.InvariantCulture))
            {
                session.CloseWithHandshake(404, "Not Found");
            }

            subPath = subPath.Substring(DefaultSensorName.Length);

            return subPath;
        }

        public void NewRequest(WebSocketSession session)
        {
            this.OnNewRequest(session, getSubPath(session));
        }

        public void NewMessage(WebSocketSession session, string message)
        {
            this.OnNewMessage(session, message, getSubPath(session));
        }

        public abstract void OnNewRequest(WebSocketSession session, string subPath);

        public abstract void OnNewMessage(WebSocketSession session, string message, string subPath);
    }
}
