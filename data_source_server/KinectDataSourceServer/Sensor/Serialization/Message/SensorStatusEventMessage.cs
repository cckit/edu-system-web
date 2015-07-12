using DataSourceServer.Message;

namespace KinectDataSourceServer.Sensor.Serialization.Message
{
    internal class SensorStatusEventMessage : EventMessage
    {
        public bool connected { get; set; }
    }
}
