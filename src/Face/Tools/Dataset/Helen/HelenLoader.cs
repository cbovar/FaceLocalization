using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Face.Tools.Dataset.Helen
{
    public static class HelenLoader
    {
        public static FaceLocalizationDataset LoadDataset(string datasetPath, int width, int height, int n = -1)
        {
            Console.Write("Loading Helen dataset");

            var result = new FaceLocalizationDataset(width, height);

            // Annotations
            var annotations = new Dictionary<string, BoundingBox>();
            foreach (var file in Directory.EnumerateFiles(Path.Combine(datasetPath, "annotation")))
            {
                using (var sr = new StreamReader(file))
                {
                    var key = sr.ReadLine();
                    var boundingBox = new BoundingBox();

                    // Compute bounding box
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine().Replace(" ", String.Empty).Split(',');
                        var x = Single.Parse(line[0]);
                        var y = Single.Parse(line[1]);

                        if (x < boundingBox.x1)
                        {
                            boundingBox.x1 = x;
                        }

                        if (x > boundingBox.x2)
                        {
                            boundingBox.x2 = x;
                        }

                        if (y < boundingBox.y1)
                        {
                            boundingBox.y1 = y;
                        }

                        if (y > boundingBox.y2)
                        {
                            boundingBox.y2 = y;
                        }
                    }

                    annotations[key] = boundingBox;
                }
            }

            // Images
            var fileNames = Directory.EnumerateFiles(Path.Combine(datasetPath, "helen_1"))
                .Concat(Directory.EnumerateFiles(Path.Combine(datasetPath, "helen_2")))
                .Concat(Directory.EnumerateFiles(Path.Combine(datasetPath, "helen_3")))
                .Concat(Directory.EnumerateFiles(Path.Combine(datasetPath, "helen_4")))
                .Concat(Directory.EnumerateFiles(Path.Combine(datasetPath, "helen_5")));
            if (n != -1)
            {
                fileNames = fileNames.Take(n);
            }

            Parallel.ForEach(fileNames, filename =>
                //  foreach (var filename in fileNames)
            {
                Console.Write(".");
                var data = new float[width * height];

                // Load image
                var image = (Bitmap)Image.FromFile(filename);
                var originalWidth = image.Width;
                var originalHeight = image.Height;

                BitmapTool.ExtractData(width, height, data, image);

                // normalize box
                var boundingBox = annotations[Path.GetFileName(filename).Replace(".jpg", String.Empty)];
                boundingBox.x1 = boundingBox.x1 / originalWidth;
                boundingBox.y1 = boundingBox.y1 / originalHeight;
                boundingBox.x2 = boundingBox.x2 / originalWidth;
                boundingBox.y2 = boundingBox.y2 / originalHeight;

                result.TrainSet.Add(new FaceLocalizationEntry { ImageData = data, BoundingBox = boundingBox, Filename = filename });
                //}
            });

            return result;
        }
    }
}