using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DataSourceServer;

namespace KinectDataSourceServer.KinectFrameHandler
{
    public class UpdateImageKinectFrameHandler : AbstractKinectFramesHandler
    {
        private Server webServer;

        public UpdateImageKinectFrameHandler(Server webServer)
        {
            this.webServer = webServer;
        }

        public override void ColorFrameCallback(long timestamp, int frameNumber, byte[] colorPixels)
        {
            JpegBitmapEncoder enc = new JpegBitmapEncoder();
            var format = PixelFormats.Bgr32;
            BitmapSource bs = BitmapSource.Create(640, 480, 96.0, 96.0, format, null, colorPixels, 640 * format.BitsPerPixel / 8);
            var ms = new MemoryStream();

            enc.Frames.Add(BitmapFrame.Create(bs));
            enc.Save(ms);
            webServer.SocketIOHandler.onImageUpdated(ms.ToArray());
        }
    }
}
