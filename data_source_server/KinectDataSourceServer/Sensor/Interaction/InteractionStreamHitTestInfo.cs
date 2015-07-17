
namespace KinectDataSourceServer.Sensor.Interaction
{
    internal class InteractionStreamHitTestInfo
    {
        public bool isPressTarget { get; set; }

        public string pressTargetControlId { get; set; }

        public double pressAttractionPointX { get; set; }

        public double pressAttractionPointY { get; set; }

        public bool isGripTarget { get; set; }
    }
}
