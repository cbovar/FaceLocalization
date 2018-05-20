using System;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;

namespace Face.Tools.Dataset
{
    public class FaceLocalizationDataset : DataSet<FaceLocalizationEntry>
    {
        public FaceLocalizationDataset(int width, int heigth) : base(width, heigth)
        {
        }

        protected override Tuple<Volume, Volume, FaceLocalizationEntry[]> CreateBatch(int n, FaceLocalizationEntry[] entries)
        {
            // Create volume that will contain all those images
            var input = BuilderInstance.Volume.SameAs(new Shape(this.Width, this.Heigth, 1, n));
            // Create volume that will contain the ground truth (bouding boxes)
            var output = BuilderInstance.Volume.SameAs(new Shape(1, 1, 4, n));

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

                output.Set(0, 0, 0, i, entry.BoundingBox.x1);
                output.Set(0, 0, 1, i, entry.BoundingBox.y1);
                output.Set(0, 0, 2, i, entry.BoundingBox.x2);
                output.Set(0, 0, 3, i, entry.BoundingBox.y2);
            }

            return new Tuple<Volume, Volume, FaceLocalizationEntry[]>((Volume)input, (Volume)output, entries);
        }
    }
}