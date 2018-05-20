using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Face.Tools.Dataset.LfwCrop
{
    public class ImageLoader
    {
        private static int seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        private readonly bool _random;
        private readonly int _randomPerImage;
        private readonly int minWidthPixel = 30;

        public ImageLoader(bool random = false, int randomPerImage = 1)
        {
            this._random = random;
            this._randomPerImage = randomPerImage;
        }

        public IEnumerable<FaceDetectionEntry> LoadDataset(string datasetPath, int width, int height, int n = -1)
        {
            var result = new ConcurrentBag<FaceDetectionEntry>();

            var fileNames = Directory.EnumerateFiles(datasetPath, "*.jpg", SearchOption.AllDirectories);
            if (n != -1)
            {
                fileNames = fileNames.Take(n);
            }

            Parallel.ForEach(fileNames, filename =>
            //foreach (var filename in fileNames)
            {
                Console.Write(".");
                var data = new float[width * height];
                var image = Image.FromFile(filename) as Bitmap;

                if (this._random)
                {
                    for (var i = 0; i < this._randomPerImage; i++)
                    {
                        var x1 = Random.Value.Next((int)(image.Width * 0.8));
                        var x2 = Random.Value.Next(image.Width - x1) + this.minWidthPixel + x1;
                        var y1 = Random.Value.Next((int)(image.Height * 0.8));
                        var y2 = (int)((x2 - x1) / (float)image.Width * image.Height) + y1;
                        x2 = Math.Min(image.Width, x2);
                        y2 = Math.Min(image.Height, y2);


                        var subImage = image.Clone(new Rectangle(x1, y1, x2 - x1, y2 - y1), image.PixelFormat);
                        //       subImage.Save("test.jpg");

                        BitmapTool.ExtractData(width, height, data, subImage);
                        result.Add(new FaceDetectionEntry { IsFace = false, ImageData = data, Filename = filename });

                        subImage.Dispose();
                    }
                }
                else
                {
                    BitmapTool.ExtractData(width, height, data, image);
                    result.Add(new FaceDetectionEntry { IsFace = false, ImageData = data, Filename = filename });
                }

                image.Dispose();
            }
              );

            return result;
        }
    }
}