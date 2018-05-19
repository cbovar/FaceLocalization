using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Flow.Training;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face.Tools;
using Face.Tools.Dataset;
using Face.Tools.Dataset.Helen;
using Face.Tools.Dataset.LfwCrop;

namespace Face
{
    internal class Program
    {

        private static void FacePresence()
        {
            var batchSize = 1000;
            int width = 32;
            int height = 32;

            BuilderInstance<float>.Volume = new VolumeBuilder(); // For GPU

            // Load Dataset - Faces
            var faces = LfwCropLoader.LoadDataset(@"..\..\..\Dataset\lfwcrop_grey", width, height);

            // Load Dataset - Non-faces
            var imageLoader = new ImageLoader(true, 2);
            var nonFaces1 = imageLoader.LoadDataset(@"..\..\..\Dataset\scene_categories", width, height);
            var nonFaces2 = imageLoader.LoadDataset(@"..\..\..\Dataset\TextureDatabase", width, height);
            var nonFaces3 = imageLoader.LoadDataset(@"..\..\..\Dataset\cars_brad_bg", width, height);
            var nonFaces4 = imageLoader.LoadDataset(@"..\..\..\Dataset\houses", width, height);

            var facesDataset = new FaceDetectionDataset(width, height);
            facesDataset.TrainSet.AddRange(faces);
            facesDataset.TrainSet.AddRange(nonFaces1);
            facesDataset.TrainSet.AddRange(nonFaces2);
            facesDataset.TrainSet.AddRange(nonFaces3);
            facesDataset.TrainSet.AddRange(nonFaces4);

            Console.WriteLine(" Done.");
            ConvNetSharp<float> cns;

            // Model
            Op<float> softmax = null;
            if (File.Exists("FaceDetection.json"))
            {
                Console.WriteLine("Loading model from disk...");
                softmax = SerializationExtensions.Load<float>("FaceDetection", false)[0]; // first element is the model (second element is the cost if it was saved along)
                cns = softmax.Graph; // Deserialization creates its own graph that we have to use. TODO: make it simplier in ConvNetSharp
            }
            else
            {
                cns = new ConvNetSharp<float>();
            }

            var x = cns.PlaceHolder("x");
            var dropProb = cns.PlaceHolder("dropProb");

            if (softmax == null)
            {
                // Inspired by https://github.com/PCJohn/FaceDetect
                var layer1 = cns.Relu(cns.Conv(x, 5, 5, 4, 2) + cns.Variable(new Shape(1, 1, 4, 1), "bias1", true));
                var layer2 = cns.Relu(cns.Conv(layer1, 3, 3, 16, 2) + cns.Variable(new Shape(1, 1, 16, 1), "bias2", true));
                var layer3 = cns.Relu(cns.Conv(layer2, 3, 3, 32) + cns.Variable(new Shape(1, 1, 32, 1), "bias3", true));

                var flatten = cns.Flatten(layer3);
                var dense1 = cns.Dropout(cns.Relu(cns.Dense(flatten, 600)) + cns.Variable(new Shape(1, 1, 600, 1), "bias4", true), dropProb);
                var dense2 = cns.Dense(dense1, 2) + cns.Variable(new Shape(1, 1, 2, 1), "bias5", true);
                softmax = cns.Softmax(dense2);
            }

            var y = cns.PlaceHolder("y");

            // Cost
            var cost = new SoftmaxCrossEntropy<float>(cns, softmax, y);

            // Optimizer
            var optimizer = new AdamOptimizer<float>(cns, 1e-4f, 0.9f, 0.999f, 1e-16f);

            //if (File.Exists("loss.csv"))
            //{
            //    File.Delete("loss.csv");
            //}

            Volume<float> trainingProb = 0.5f;
            Volume<float> testingProb = 0.0f;

            // Training
            using (var session = new Session<float>())
            {
                session.Differentiate(cost); // computes dCost/dW at every node of the graph

                var iteration = 0;
                double currentCost;
                do
                {
                    var batch = facesDataset.GetBatch(batchSize);
                    var input = batch.Item1;
                    var output = batch.Item2;

                    var dico = new Dictionary<string, Volume<float>> { { "x", input }, { "y", output }, { "dropProb" , trainingProb } };


                    var stopwatch = Stopwatch.StartNew();
                   // session.Run(softmax, dico);
                    Debug.WriteLine(stopwatch.ElapsedMilliseconds);

                    currentCost = session.Run(cost, dico);
                    Console.WriteLine($"cost: {currentCost}");
                    File.AppendAllLines("loss.csv", new[] { currentCost.ToString(CultureInfo.InvariantCulture) });

                    session.Run(optimizer, dico);

                    if (iteration++ % 100 == 0)
                    {
                        // Test on a on random picture
                        var test = facesDataset.GetBatch(100);
                        dico = new Dictionary<string, Volume<float>> { { "x", test.Item1 }, { "dropProb", testingProb } };
                        var result = session.Run(softmax, dico);

                        int correct = 0;
                        for (int i = 0; i < 100; i++)
                        {
                            var class0Prob = result.Get(0, 0, 0, i);
                            var class1Prob = result.Get(0, 0, 1, i);

                            if ((test.Item3[i].IsFace && class1Prob > class0Prob) || (!test.Item3[i].IsFace && class0Prob > class1Prob))
                            {
                                correct++;
                            }
                        }

                        Console.WriteLine($"Test: {correct}%");
                        File.AppendAllLines("accuracy.csv", new[] { correct.ToString() });
                        var filename = test.Item3[0].Filename;

                        softmax.Save("FaceDetection");
                    }
                } while (currentCost > 1e-5 && !Console.KeyAvailable);

                softmax.Save("FaceDetection");
            }
        }

        private static void FaceLocalization()
        {
            BuilderInstance<float>.Volume = new VolumeBuilder(); // For GPU

            // Load Dataset
            var datasetPath = @"C:\Pro\Github\FaceLocalisation\Face\Dataset\Helen"; // contains folders from helen dataset (annotation, helen_1 ,..)
            var batchSize = 5; // my GTX 760 cannot take more...
            var dataSet = HelenLoader.LoadDataset(datasetPath, 256, 256, 50);
            Console.WriteLine(" Done.");
            ConvNetSharp<float> cns;

            // Model
            Op<float> yhat = null;
            if (File.Exists("FaceDetection.json"))
            {
                Console.WriteLine("Loading model from disk...");
                yhat = SerializationExtensions.Load<float>("FaceDetection", false)[0]; // first element is the model (second element is the cost if it was saved along)
                cns = yhat.Graph; // Deserialization creates its own graph that we have to use. TODO: make it simplier in ConvNetSharp
            }
            else
            {
                cns = new ConvNetSharp<float>();
            }

            var x = cns.PlaceHolder("x");

            if (yhat == null)
            {
                // Inspired by http://cs231n.stanford.edu/reports/2017/pdfs/222.pdf
                var alpha = 0.2f;
                var layer1 = cns.Dropout(cns.Pool(cns.LeakyRelu(cns.Conv(x, 3, 3, 32, 1, 1) + cns.Variable(new Shape(1, 1, 32, 1), "bias1", true), alpha), 2, 2, 0, 0, 2, 2), 0.1f);
                var layer2 = cns.Pool(cns.LeakyRelu(cns.Conv(layer1, 3, 3, 64, 1, 1) + cns.Variable(new Shape(1, 1, 64, 1), "bias2", true), alpha), 2, 2, 0, 0, 2, 2);
                var layer3 = cns.Dropout(cns.Pool(cns.LeakyRelu(cns.Conv(layer2, 3, 3, 128, 1, 1) + cns.Variable(new Shape(1, 1, 128, 1), "bias3", true), alpha), 2, 2, 0, 0, 2, 2),
                    0.1f);
                var layer4 = cns.Pool(cns.LeakyRelu(cns.Conv(layer3, 3, 3, 64, 1, 1) + cns.Variable(new Shape(1, 1, 64, 1), "bias4", true), alpha), 2, 2, 0, 0, 2, 2);
                var layer5 = cns.Pool(cns.LeakyRelu(cns.Conv(layer4, 3, 3, 16, 1, 1) + cns.Variable(new Shape(1, 1, 16, 1), "bias5", true), alpha), 2, 2, 0, 0, 2, 2);

                var flatten = cns.Flatten(layer5);
                var dense1 = cns.Conv(flatten, 1, 1, 128);
                yhat = cns.Conv(dense1, 1, 1, 4);

                //x.Evaluated += (sender, args) => { }; // I use this to place a break point and check Volume dimensions / debug
            }

            var y = cns.PlaceHolder("y");

            // Cost
            var cost = cns.Sum((yhat - y) * (yhat - y), Shape.From(1));

            // Optimizer
            var optimizer = new AdamOptimizer<float>(cns, 0.01f, 0.9f, 0.999f, 1e-08f);

            if (File.Exists("loss.csv"))
            {
                File.Delete("loss.csv");
            }

            // Training
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

                    var dico = new Dictionary<string, Volume<float>> { { "x", input }, { "y", output } };

                    currentCost = session.Run(cost, dico);
                    Console.WriteLine($"cost: {currentCost}");
                    File.AppendAllLines("loss.csv", new[] { currentCost.ToString(CultureInfo.InvariantCulture) });

                    session.Run(optimizer, dico);

                    if (iteration++ % 100 == 0)
                    {
                        // Test on a on random picture
                        var test = dataSet.GetBatch(1);
                        dico = new Dictionary<string, Volume<float>> { { "x", test.Item1 } };
                        var result = session.Run(yhat, dico);

                        var image = (Bitmap)Image.FromFile(test.Item3[0].Filename);
                        BitmapTool.DrawBoundingBox(image, new BoundingBox { x1 = result.Get(0), y1 = result.Get(1), x2 = result.Get(2), y2 = result.Get(3) }, Color.Blue);
                        BitmapTool.DrawBoundingBox(image, test.Item3[0].BoundingBox, Color.Green); // correct answer
                        image.Save($"iteration_{iteration}.jpg");

                        yhat.Save("FaceDetection");
                    }
                } while (currentCost > 1e-5 && !Console.KeyAvailable);

                yhat.Save("FaceDetection");
            }
        }


        private static void Main()
        {
            FacePresence();
        }
    }
}