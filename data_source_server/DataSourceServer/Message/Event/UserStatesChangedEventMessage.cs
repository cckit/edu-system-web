namespace DataSourceServer.Message.Event
{
    public class UserStatesChangedEventMessage : EventMessage
    {
        public StateMappingEntry[] userStates { get; set; }
    }
}
