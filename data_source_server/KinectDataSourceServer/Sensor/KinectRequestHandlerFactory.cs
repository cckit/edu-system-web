using System;
using System.Collections.ObjectModel;
using Microsoft.Kinect.Toolkit;

namespace KinectDataSourceServer.Sensor
{
    public class KinectRequestHandlerFactory
    {
        private readonly KinectSensorChooser sensorChooser;
        private readonly Collection<ISensorStreamHandlerFactory> streamHandlerFactories;

        public KinectRequestHandlerFactory(KinectSensorChooser sensorChooser)
            : this(sensorChooser, CreateDefaultStreamHandlerFactories())
        {

        }

        public KinectRequestHandlerFactory(KinectSensorChooser sensorChooser, Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.sensorChooser = sensorChooser;
            this.streamHandlerFactories = streamHandlerFactories;
        }

        public static Collection<ISensorStreamHandlerFactory> CreateDefaultStreamHandlerFactories()
        {
            var streamHandlerTypes = new[]
            {
                StreamHandlerType.BackgroundRemoval,
                StreamHandlerType.SensorStatus
            };

            var factoryCollection = new Collection<ISensorStreamHandlerFactory>();
            Array.ForEach(streamHandlerTypes, type => factoryCollection.Add(new SensorStreamHandlerFactory(type)));
            return factoryCollection;
        }

        public KinectRequestHandler CreateHandler()
        {
            return new KinectRequestHandler(sensorChooser, streamHandlerFactories);
        }
    }
}
