using System.Windows.Media;
using DataSourceServer.Serialization;

namespace DataSourceServer.Message.Stream
{
    public class BackgroundRemovalStreamMessage : ImageHeaderStreamMessage
    {
        public const int BytesPerPixel = 4;

        public int trackedPlayerId { get; set; }

        public short averageDepth { get; set; }

        private byte[] CompressedImage { get; set; }

        public void UpdateBuffer(byte[] bgraData, int width, int height)
        {
            this.CompressedImage = bgraData.BitmapToPngImageStream(PixelFormats.Bgra32, width, height).ToArray();
            this.bufferLength = this.CompressedImage.Length;
        }

        public byte[] GetCompressedImage()
        {
            return this.CompressedImage;
        }
    }
}
