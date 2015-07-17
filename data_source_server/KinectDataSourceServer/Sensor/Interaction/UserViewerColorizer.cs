using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectDataSourceServer.Sensor.Interaction
{
    internal class UserViewerColorizer
    {
        private const int BytesPerPixel = 4;
        private const int BackgroundColor = 0x00000000;
        private readonly int[] playerColorLookupTable;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Buffer { get; private set; }

        public UserViewerColorizer(int width, int height)
        {
            this.playerColorLookupTable = new int[SharedConstants.MaxUsersTracked];
            this.SetResolution(width, height);
        }

        public void SetResolution(int width, int height)
        {
            if ((this.Buffer == null) || (this.Width != width) || (this.Height != height))
            {
                this.Width = width;
                this.Height = height;
                this.Buffer = new byte[width * height * BytesPerPixel];
            }
        }

        private void checkValid(DepthImagePixel[] depthImagePixels, int depthWidth, int depthHeight)
        {
            if (depthImagePixels == null)
            {
                throw new ArgumentNullException("depthImagePixels");
            }

            if (depthWidth <= 0)
            {
                throw new ArgumentException(@"Width of depth image must be greater than zero", "depthWidth");
            }

            if (depthWidth % this.Width != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Depth image width '{0}' is not a multiple of the desired user viewer image width '{1}'",
                        depthWidth,
                        this.Width),
                    "depthWidth");
            }

            if (depthHeight <= 0)
            {
                throw new ArgumentException(@"Height of depth image must be greater than zero", "depthHeight");
            }

            if (depthHeight % this.Height != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Depth image height '{0}' is not a multiple of the desired user viewer image height '{1}'",
                        depthHeight,
                        this.Height),
                    "depthHeight");
            }
        }

        public void ColorizeDepthPixels(DepthImagePixel[] depthImagePixels, int depthWidth, int depthHeight)
        {
            checkValid(depthImagePixels, depthWidth, depthHeight);

            int downscaleFactor = depthWidth / this.Width;
            Debug.Assert(depthHeight / this.Height == downscaleFactor, "Downscale factor in x and y dimensions should be exactly the same.");

            int pixelDisplacementBetweenRows = depthWidth * downscaleFactor;

            unsafe
            {
                fixed (byte* colorBufferPtr = this.Buffer)
                {
                    fixed (DepthImagePixel* depthImagePixelPtr = depthImagePixels)
                    {
                        fixed (int* playerColorLookupPtr = this.playerColorLookupTable)
                        {
                            // Write color values using int pointers instead of byte pointers,
                            // since each color pixel is 32-bits wide.
                            int* colorBufferIntPtr = (int*)colorBufferPtr;
                            DepthImagePixel* currentPixelRowPtr = depthImagePixelPtr;

                            for (int row = 0; row < depthHeight; row += downscaleFactor)
                            {
                                DepthImagePixel* currentPixelPtr = currentPixelRowPtr;
                                for (int column = 0; column < depthWidth; column += downscaleFactor)
                                {
                                    *colorBufferIntPtr++ = playerColorLookupPtr[currentPixelPtr->PlayerIndex];
                                    currentPixelPtr += downscaleFactor;
                                }

                                currentPixelRowPtr += pixelDisplacementBetweenRows;
                            }
                        }
                    }
                }
            }
        }

        public void ResetColorLookupTable()
        {
            // Initialize all player indexes to background color
            for (int entryIndex = 0; entryIndex < this.playerColorLookupTable.Length; ++entryIndex)
            {
                this.playerColorLookupTable[entryIndex] = BackgroundColor;
            }
        }

        public void UpdateColorLookupTable(UserInfo[] userInfos, int defaultUserColor, IDictionary<int, string> userStates, IDictionary<string, int> userColors)
        {
            if ((userInfos == null) || (userStates == null) || (userColors == null))
            {
                this.ResetColorLookupTable();
                return;
            }

            // Reset lookup table to have all player indexes map to default user color
            for (int i = 1; i < this.playerColorLookupTable.Length; i++)
            {
                this.playerColorLookupTable[i] = defaultUserColor;
            }

            // Iterate through user tracking Ids to populate color table.
            for (int i = 0; i < userInfos.Length; ++i)
            {
                // Player indexes in depth image are shifted by one in order to be able to
                // use zero as a marker to mean "pixel does not correspond to any player".
                int depthPlayerIndex = i + 1;
                var trackingId = userInfos[i].SkeletonTrackingId;

                string state;
                if (userStates.TryGetValue(trackingId, out state))
                {
                    int color;
                    if (userColors.TryGetValue(state, out color))
                    {
                        this.playerColorLookupTable[depthPlayerIndex] = color;
                    }
                }
            }
        }
    }
}
