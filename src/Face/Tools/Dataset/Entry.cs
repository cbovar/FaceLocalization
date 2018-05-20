namespace Face.Tools.Dataset
{
    public abstract class Entry
    {
        public string Filename { get; set; }
    }

    public class FaceLocalizationEntry : Entry
    {
        // Bounding box
        public BoundingBox BoundingBox { get; set; }

        // Image 0.0-1.0 grayscale
        public float[] ImageData { get; set; }
    }

    public class FaceDetectionEntry : Entry
    {
        public bool IsFace { get; set; }

        // Image 0.0-1.0 grayscale
        public float[] ImageData { get; set; }
    }
}