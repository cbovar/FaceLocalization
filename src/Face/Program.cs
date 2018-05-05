using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Flow.Training;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;

namespace Face
{
    internal class Program
    {
        public const int IMAGE_WIDTH = 64;
        public const int IMAGE_HEIGHT = 64;

        private static DataSet LoadHelenDataset(string datasetPath, int n = -1)
        {
            Console.Write("Loading Helen dataset");

            var result = new DataSet(IMAGE_WIDTH, IMAGE_HEIGHT);

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
                        var line = sr.ReadLine().Replace(" ", string.Empty).Split(',');
                        var x = float.Parse(line[0]);
                        var y = float.Parse(line[1]);

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
            {
                Console.Write(".");
                var data = new float[IMAGE_WIDTH * IMAGE_HEIGHT];

                // Load image
                var image = (Bitmap) Image.FromFile(filename);
                var originalWidth = image.Width;
                var originalHeight = image.Height;

                BitmapTool.ExtractData(IMAGE_WIDTH, IMAGE_HEIGHT, data, image);

                // normalize box
                var boundingBox = annotations[Path.GetFileName(filename).Replace(".jpg", string.Empty)];
                boundingBox.x1 = boundingBox.x1 / originalWidth;
                boundingBox.y1 = boundingBox.y1 / originalHeight;
                boundingBox.x2 = boundingBox.x2 / originalWidth;
                boundingBox.y2 = boundingBox.y2 / originalHeight;

                result.TrainSet.Add(new HelenEntry {ImageData = data, BoundingBox = boundingBox, Filename = filename});
            });

            return result;
        }

        private static void Main()
        {
            BuilderInstance<float>.Volume = new VolumeBuilder();

            var datasetPath = @"C:\Pro\Github\FaceLocalisation\Face\Dataset"; // contains folders from helen dataset (annotation, helen_1 ,..)
            var batchSize = 128;
            var dataSet = LoadHelenDataset(datasetPath);

            var cns = new ConvNetSharp<float>();

            // Model
            var x = cns.PlaceHolder("x");
            var layer1 = cns.Pool(cns.Conv(x, 3, 3, 4), 2, 2, 0, 0, 1, 1);
            var layer2 = cns.Pool(cns.Conv(layer1, 3, 3, 4), 2, 2, 0, 0, 1, 1);
            //var layer3 = cns.Pool(cns.Conv(layer2, 3, 3, 4), 2, 2, 0, 0, 1, 1);
            var flat = cns.Flatten(layer2);
            var yhat = cns.Conv(flat, 1, 1, 4);

            var y = cns.PlaceHolder("y");

            // Cost
            var cost = cns.Sum((yhat - y) * (yhat - y), Shape.From(1));

            // Optimizer
            var optimizer = new AdamOptimizer<float>(cns, 0.001f, 0.9f, 0.999f, 1e-08f);

            if (File.Exists("loss.csv"))
            {
                File.Delete("loss.csv");
            }

            using (var session = new Session<float>())
            {
                session.Differentiate(cost); // computes dCost/dW at every node of the graph

                var iteration = 0;
                double currentCost;
                do
                {
                    var batch = dataSet.GetBatch(batchSize);
                    var input = batch.Item1;
                    var output = batch.Item2;

                    var dico = new Dictionary<string, Volume<float>> {{"x", input}, {"y", output}};

                    currentCost = session.Run(cost, dico);
                    Console.WriteLine($"cost: {currentCost}");
                    File.AppendAllLines("loss.csv", new[] {currentCost.ToString(CultureInfo.InvariantCulture)});

                    session.Run(optimizer, dico);

                    if (iteration++ % 100 == 0)
                    {
                        // Test on a on picture
                        var test = dataSet.GetBatch(1);
                        dico = new Dictionary<string, Volume<float>> {{"x", test.Item1}};
                        var result = session.Run(yhat, dico);

                        var image = (Bitmap) Image.FromFile(test.Item3[0].Filename);
                        BitmapTool.DrawBoundingBox(image, new BoundingBox {x1 = result.Get(0), y1 = result.Get(1), x2 = result.Get(2), y2 = result.Get(3)}, Color.Blue);
                        BitmapTool.DrawBoundingBox(image, test.Item3[0].BoundingBox, Color.Green); // correct answer
                        image.Save($"iteration_{iteration}.jpg");

                        yhat.Save("FaceDetection");
                    }
                } while (currentCost > 1e-5 && !Console.KeyAvailable);

                yhat.Save("FaceDetection");
            }
        }
    }
}