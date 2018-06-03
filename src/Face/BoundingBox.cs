namespace Face
{
    public class BoundingBox
    {
        // upper left corner
        public float x1 { get; set; } = float.MaxValue;
        public float y1 { get; set; } = float.MaxValue;

        // bottom right corner
        public float x2 { get; set; } = float.MinValue;
        public float y2 { get; set; } = float.MinValue;

        public float Confidence { get; set; }
    }
}