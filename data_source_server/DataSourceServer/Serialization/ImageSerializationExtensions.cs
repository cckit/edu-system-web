using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataSourceServer.Serialization
{
    public static class ImageSerializationExtensions
    {
        private static PngBitmapEncoder enc = new PngBitmapEncoder();
        private static readonly PixelFormat format = PixelFormats.Bgra32;

        private static MemoryStream BitmapToImageStream(BitmapEncoder enc, byte[] src, int width, int height)
        {
            BitmapSource bs = BitmapSource.Create(width, height, 96.0, 96.0, format, null, src, width * format.BitsPerPixel / 8);
            var ms = new MemoryStream();

            enc.Frames.Add(BitmapFrame.Create(bs));
            enc.Save(ms);

            return ms;
        }

        public static MemoryStream BitmapToPngImageStream(this byte[] src, int width, int height)
        {
            return BitmapToImageStream(new PngBitmapEncoder(), src, width, height);
        }

        public static MemoryStream BitmapToJpgImageStream(this byte[] src, int width, int height)
        {
            return BitmapToImageStream(new JpegBitmapEncoder(), src, width, height);
        }
    }
}
