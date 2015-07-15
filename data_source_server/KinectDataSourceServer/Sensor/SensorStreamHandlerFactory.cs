using System;
using DataSourceServer.Message;

namespace KinectDataSourceServer.Sensor
{
    public enum StreamHandlerType
    {
        BackgroundRemoval,
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
                case StreamHandlerType.SensorStatus:
                    return new SensorStatusStreamHandler(context);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
