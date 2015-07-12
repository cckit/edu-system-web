using System.Globalization;
using SuperWebSocket;

namespace DataSourceServer
{
    public abstract class RequestRouter
    {
        private const string KinectEndpointBasePath = "/Kinect";
        private const string DefaultSensorName = "/default";

        public void NewRequest(WebSocketSession session)
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

            this.OnNewRequest(session, subPath);
        }

        public abstract void OnNewRequest(WebSocketSession session, string subPath);
    }
}
