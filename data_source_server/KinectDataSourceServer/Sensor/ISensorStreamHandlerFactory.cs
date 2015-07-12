using DataSourceServer.Message;

namespace KinectDataSourceServer.Sensor
{
    public interface ISensorStreamHandlerFactory
    {
        ISensorStreamHandler CreateHandler(SensorStreamHandlerContext context);
    }
}
