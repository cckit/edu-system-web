using System;
using System.Collections.Generic;
using System.Linq;
using DataSourceServer.Message;

namespace KinectDataSourceServer.Sensor
{
    public abstract class SensorStreamHandlerBase : ISensorStreamHandler
    {
        protected SensorStreamHandlerContext Context { get; private set; }

        private readonly Dictionary<string, StreamConfiguration> streamHandlerConfiguration;
        private string[] supportedStreams;

        public SensorStreamHandlerBase(SensorStreamHandlerContext context)
        {
            this.Context = context;
            this.streamHandlerConfiguration = new Dictionary<string, StreamConfiguration>();
        }

        public virtual void OnSensorChanged(Microsoft.Kinect.KinectSensor newSensor) { }

        public virtual void ProcessColor(byte[] colorData, Microsoft.Kinect.ColorImageFrame colorFrame) { }

        public virtual void ProcessDepth(Microsoft.Kinect.DepthImagePixel[] depthData, Microsoft.Kinect.DepthImageFrame depthFrame) { }

        public virtual void ProcessSkeleton(Microsoft.Kinect.Skeleton[] skeletons, Microsoft.Kinect.SkeletonFrame skeletonFrame) { }

        public virtual string[] GetSupportedStreamNames()
        {
            return this.supportedStreams ?? (this.supportedStreams = this.streamHandlerConfiguration.Keys.ToArray());
        }

        public virtual System.Threading.Tasks.Task UninitializeAsync()
        {
            return SharedConstants.EmptyCompletedTask;
        }

        protected void AddStreamConfiguration(string name, StreamConfiguration configuration)
        {
            this.streamHandlerConfiguration.Add(name, configuration);
            this.supportedStreams = null;
        }

        protected class StreamConfiguration
        {
            public Action<Dictionary<string, object>> GetPropertiesCallback { get; private set; }
            public Func<string, object, string> SetPropertyCallback { get; private set; }

            public StreamConfiguration(Action<Dictionary<string, object>> getPropertiesCallback, Func<string, object, string> setPropertyCallback)
            {
                this.GetPropertiesCallback = getPropertiesCallback;
                this.SetPropertyCallback = setPropertyCallback;
            }
        }
    }
}
