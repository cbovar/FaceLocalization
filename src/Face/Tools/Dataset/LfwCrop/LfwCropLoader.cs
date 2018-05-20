using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Face.Tools.Dataset.LfwCrop
{
    public static class LfwCropLoader
    {
        public static IEnumerable<FaceDetectionEntry> LoadDataset(string datasetPath, int width, int height, int n = -1)
        {
            var result = new ConcurrentBag<FaceDetectionEntry>();

            var fileNames = Directory.EnumerateFiles(Path.Combine(datasetPath, "faces"), "*.pgm");
            if (n != -1)
            {
                fileNames = fileNames.Take(n);
            }

            Parallel.ForEach(fileNames, filename =>
                    // foreach (var filename in fileNames)
                {
                    Console.Write(".");
                    var data = new float[width * height];
                    var pgm = PgmImage.LoadImage(filename);
                    var image = PgmImage.MakeBitmap(pgm);

                    BitmapTool.ExtractData(width, height, data, image);

                    result.Add(new FaceDetectionEntry {IsFace = true, ImageData = data, Filename = filename});
                }
            );

            return result;
        }
    }
}