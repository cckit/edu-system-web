
namespace DataSourceServer.Message.Stream
{
    public class ImageHeaderStreamMessage : StreamMessage
    {
        public int width { get; set; }

        public int height { get; set; }

        public int bufferLength { get; set; }

        public string format { get; set; }

        public ImageHeaderStreamMessage()
        {
            format = "png";
        }
    }
}
