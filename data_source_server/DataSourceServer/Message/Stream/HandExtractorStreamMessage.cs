using System.Windows.Media;
using DataSourceServer.Serialization;

namespace DataSourceServer.Message.Stream
{
    public class HandExtractorStreamMessage : ImageHeaderStreamMessage
    {
        private byte[] CompressedImage { get; set; }

        public void UpdateBuffer(byte[] img, int width, int height)
        {
            base.width = width;
            base.height = height;

            var ms = img.BitmapToPngImageStream(PixelFormats.Bgra32, width, height);
            this.CompressedImage = ms.ToArray();
            base.bufferLength = this.CompressedImage.Length;
        }

        public byte[] GetCompressedImage()
        {
            return CompressedImage;
        }
    }
}
