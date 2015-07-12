using System.Collections.ObjectModel;
using Microsoft.Kinect.Toolkit;

namespace KinectDataSourceServer.Sensor
{
    public class KinectRequestHandler
    {
        private KinectRawFrameHandler kinectRawFrameHandler;
        public KinectRequestRouter KinectRequestRouter { get; private set; }

        internal KinectRequestHandler(KinectSensorChooser sensorChooser, Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.KinectRequestRouter = new KinectRequestRouter(streamHandlerFactories);
            this.kinectRawFrameHandler = new KinectRawFrameHandler(sensorChooser, this.KinectRequestRouter.StreamHandlers);
        }

        public void Start()
        {
            kinectRawFrameHandler.InitializeAsync();
        }

        public async void Stop()
        {
            await kinectRawFrameHandler.UninitializeAsync();
        }
    }
}
