namespace DataSourceServer.Message.Stream
{
    public class InteractionStreamMessage : StreamMessage
    {
        public const int MaximumHandPointers = 4;
        public readonly MessageHandPointer[] internalHandPointers;

        public MessageHandPointer[] handPointers { get; set; }


        public InteractionStreamMessage()
        {
            this.internalHandPointers = new MessageHandPointer[MaximumHandPointers];

            for (int i = 0; i < this.internalHandPointers.Length; i++)
            {
                this.internalHandPointers[i] = new MessageHandPointer();
            }
        }
    }
}
