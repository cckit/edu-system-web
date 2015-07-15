namespace DataSourceServer.Message.Event
{
    public class SensorStatusEventMessage : EventMessage
    {
        public bool connected { get; set; }
    }
}
