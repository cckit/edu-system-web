using System;
using DataSourceServer.Message;

namespace KinectDataSourceServer.Sensor
{
    public enum StreamHandlerType
    {
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
                case StreamHandlerType.SensorStatus:
                    return new SensorStatusStreamHandler(context);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
