using System;
using System.Collections.Generic;
using System.Linq;
using DataSourceServer.Message;
using Microsoft.Kinect;

namespace KinectDataSourceServer.Sensor
{
    public abstract class SensorStreamHandlerBase : ISensorStreamHandler
    {
        protected SensorStreamHandlerContext Context { get; private set; }
        public const string EnabledPropertyName = "enabled";

        private readonly IDictionary<string, object> errorSink = new Dictionary<string, object>();
        private readonly Dictionary<string, StreamConfiguration> streamHandlerConfiguration;
        private string[] supportedStreams;

        public SensorStreamHandlerBase(SensorStreamHandlerContext context)
        {
            this.Context = context;
            this.streamHandlerConfiguration = new Dictionary<string, StreamConfiguration>();
        }

        public virtual void OnSensorChanged(KinectSensor newSensor) { }

        public virtual void ProcessColor(byte[] colorData, ColorImageFrame colorFrame) { }

        public virtual void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame) { }

        public virtual void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame) { }

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

        public IDictionary<string, object> GetState(string streamName)
        {
            var propertyMap = new Dictionary<string, object>();

            StreamConfiguration config;
            if (!this.streamHandlerConfiguration.TryGetValue(streamName, out config))
            {
                throw new ArgumentException(@"Unsupported stream name", "streamName");
            }

            config.GetPropertiesCallback(propertyMap);
            return propertyMap;
        }

        public bool SetState(string streamName, IReadOnlyDictionary<string, object> properties, IDictionary<string, object> errors)
        {
            bool successful = true;

            if (properties == null)
            {
                throw new ArgumentException(@"properties must not be null", "properties");
            }

            if (errors == null)
            {
                this.errorSink.Clear();
                errors = this.errorSink;
            }

            StreamConfiguration config;
            if (!this.streamHandlerConfiguration.TryGetValue(streamName, out config))
            {
                throw new ArgumentException(@"Unsupported stream name", "streamName");
            }

            foreach (var keyValuePair in properties)
            {
                try
                {
                    var error = config.SetPropertyCallback(keyValuePair.Key, keyValuePair.Value);
                    if (error != null)
                    {
                        errors.Add(keyValuePair.Key, error);
                        successful = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    successful = false;
                    errors.Add(keyValuePair.Key, Properties.Resources.PropertySetError);
                }
            }

            return successful;
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
