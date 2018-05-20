using System;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;

namespace Face.Tools.Dataset
{
    public class FaceDetectionDataset : DataSet<FaceDetectionEntry>
    {
        public FaceDetectionDataset(int width, int heigth) : base(width, heigth)
        {
        }

        protected override Tuple<Volume, Volume, FaceDetectionEntry[]> CreateBatch(int n, FaceDetectionEntry[] entries)
        {
            // Create volume that will contain all those images
            var input = BuilderInstance.Volume.SameAs(new Shape(this.Width, this.Heigth, 1, n));
            // Create volume that will contain the ground truth (one hot vector => face / no face)
            var output = BuilderInstance.Volume.SameAs(new Shape(1, 1, 2, n));

            for (var i = 0; i < n; i++)
            {
                var entry = entries[i];
                for (var y = 0; y < this.Heigth; y++)
                {
                    for (var x = 0; x < this.Width; x++)
                    {
                        input.Set(x, y, 0, i, entry.ImageData[x + y * this.Width]);
                    }
                }

                output.Set(0, 0, 0, i, entry.IsFace ? 0.0f : 1.0f); // Class 0 : it is not a face
                output.Set(0, 0, 1, i, entry.IsFace ? 1.0f : 0.0f); // Class 1 : it is a face
            }

            return new Tuple<Volume, Volume, FaceDetectionEntry[]>((Volume)input, (Volume)output, entries);
        }
    }
}