namespace DataSourceServer.Message.Stream
{
    public class BackgroundRemovalStreamMessage : ImageHeaderStreamMessage
    {
        public const int BytesPerPixel = 4;

        public int trackedPlayerId { get; set; }

        public short averageDepth { get; set; }

        public byte[] Buffer { get; set; }

        public void UpdateBuffer(byte[] bgraData)
        {

            if ((this.Buffer == null) || (this.Buffer.Length != this.bufferLength))
            {
                this.Buffer = new byte[this.bufferLength];
            }

            unsafe
            {
                fixed (byte* messageDataPtr = this.Buffer)
                {
                    fixed (byte* frameDataPtr = bgraData)
                    {
                        byte* messageDataPixelPtr = messageDataPtr;
                        byte* frameDataPixelPtr = frameDataPtr;

                        byte* messageDataPixelPtrEnd = messageDataPixelPtr + this.bufferLength;

                        while (messageDataPixelPtr != messageDataPixelPtrEnd)
                        {
                            // Convert from BGRA to RGBA format
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 2);
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 1);
                            *(messageDataPixelPtr++) = *frameDataPixelPtr;
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 3);

                            frameDataPixelPtr += BackgroundRemovalStreamMessage.BytesPerPixel;
                        }
                    }
                }
            }
        }
    }
}
