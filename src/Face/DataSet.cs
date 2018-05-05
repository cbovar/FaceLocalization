using System;
using System.Collections.Concurrent;
using System.Linq;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;

namespace Face
{
    internal class DataSet
    {
        private readonly int _heigth;
        private readonly Random _random = new Random();
        private readonly int _width;

        public DataSet(int width, int heigth)
        {
            this._width = width;
            this._heigth = heigth;
        }

        public ConcurrentBag<HelenEntry> TrainSet { get; set; } = new ConcurrentBag<HelenEntry>();

        public Tuple<Volume, Volume, HelenEntry[]> GetBatch(int n)
        {
            // Select n entry randomly
            var entries = this.TrainSet.OrderBy(x => this._random.Next()).Take(n).ToArray();

            // Create volume that will contain all those images
            var input = BuilderInstance.Volume.SameAs(new Shape(this._width, this._heigth, 1, n));
            // Create volume that will contain the ground truth (bouding boxes)
            var output = BuilderInstance.Volume.SameAs(new Shape(1, 1, 4, n));

            for (var i = 0; i < n; i++)
            {
                var entry = entries[i];
                for (var y = 0; y < this._heigth; y++)
                {
                    for (var x = 0; x < this._width; x++)
                    {
                        input.Set(x, y, 0, i, entry.ImageData[x + y * this._width]);
                    }
                }

                output.Set(0, 0, 0, i, entry.BoundingBox.x1);
                output.Set(0, 0, 1, i, entry.BoundingBox.y1);
                output.Set(0, 0, 2, i, entry.BoundingBox.x2);
                output.Set(0, 0, 3, i, entry.BoundingBox.y2);
            }

            return new Tuple<Volume, Volume, HelenEntry[]>((Volume)input, (Volume)output, entries);
        }
    }
}