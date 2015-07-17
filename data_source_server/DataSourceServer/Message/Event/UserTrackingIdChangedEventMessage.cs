namespace DataSourceServer.Message.Event
{
    public class UserTrackingIdChangedEventMessage : EventMessage
    {
        public int oldValue { get; set; }

        public int newValue { get; set; }
    }
}
