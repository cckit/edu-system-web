using System;
using DataSourceServer.Message;
using KinectDataSourceServer.Sensor.Interaction;

namespace KinectDataSourceServer.Sensor
{
    public enum StreamHandlerType
    {
        BackgroundRemoval,
        Interaction,
        SensorStatus,
    }

    public class SensorStreamHandlerFactory : ISensorStreamHandlerFactory
    {
        private readonly StreamHandlerType streamType;

        public SensorStreamHandlerFactory(StreamHandlerType streamType)
        {
            this.streamType = streamType;
        }

        public ISensorStreamHandler CreateHandler(SensorStreamHandlerContext context)
        {
            switch (streamType)
            {
                case StreamHandlerType.BackgroundRemoval:
                    return new BackgroundRemovalStreamHandler(context);
                case StreamHandlerType.Interaction:
                    return new InteractionStreamHandler(context);
                case StreamHandlerType.SensorStatus:
                    return new SensorStatusStreamHandler(context);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
