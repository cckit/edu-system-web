using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataSourceServer.Serialization
{
    public static class ImageSerializationExtensions
    {
        public static MemoryStream BitmapToImageStream(this byte[] src, BitmapEncoder enc, PixelFormat format, int width, int height)
        {
            BitmapSource bs = BitmapSource.Create(width, height, 96.0, 96.0, format, null, src, width * format.BitsPerPixel / 8);
            var ms = new MemoryStream();

            enc.Frames.Add(BitmapFrame.Create(bs));
            enc.Save(ms);

            return ms;
        }

        public static MemoryStream BitmapToPngImageStream(this byte[] src, PixelFormat format, int width, int height)
        {
            return src.BitmapToImageStream(new PngBitmapEncoder(), format, width, height);
        }

        public static MemoryStream BitmapToJpgImageStream(this byte[] src, PixelFormat format, int width, int height)
        {
            return src.BitmapToImageStream(new JpegBitmapEncoder(), format, width, height);
        }
    }
}
