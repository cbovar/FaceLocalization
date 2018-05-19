using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face;
using Face.Tools;
using Photo.Properties;

namespace Photo
{
    public partial class Form1 : Form
    {
        public const int IMAGE_WIDTH = 32; // Must be the same as when training model
        public const int IMAGE_HEIGHT = 32;
        private readonly Op<float> _yhat;

        public Form1()
        {
            InitializeComponent();

            this.openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();

            if (!string.IsNullOrEmpty(Settings.Default.LastFilename))
            {
                SetFilename(Settings.Default.LastFilename);
            }

            BuilderInstance<float>.Volume = new VolumeBuilder();
            ; // Needed for GPU, must be done on AI thread

            // Must be done after setting BuilderInstance<float>.Volume
            this._yhat = SerializationExtensions.Load<float>("FaceDetection", false)[0];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = this.openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                SetFilename(this.openFileDialog1.FileName);
            }
        }

        private void evaluate_Click(object sender, EventArgs e)
        {
            var bitmap = GetImage(this.textBox1.Text);
            var output = (Bitmap)bitmap.Clone();
            bitmap = BitmapTool.ResizeImage(bitmap, 300, 300);

            bitmap.Save("input.jpg");

            using (var session = new Session<float>())
            {
                var boxes = new List<BoundingBox>();

                boxes.AddRange(SlideWindow(40, bitmap, session, output));
                boxes.AddRange(SlideWindow(80, bitmap, session, output));
                boxes.AddRange(SlideWindow(120, bitmap, session, output));

                var nms = new NonMaximumSuppresion();
                var test = nms.Nms(boxes, 0.93f);
                foreach (var boundingBox in test)
                {
                    BitmapTool.DrawBoundingBox(output, boundingBox, Color.FromArgb(50, 0, 0, 255));
                }
            }

            this.pictureBox1.Image = output;
        }

        private Bitmap GetImage(string filename)
        {
            if (filename.Contains("pgm"))
            {
                return PgmImage.MakeBitmap(PgmImage.LoadImage(filename));
            }

            return (Bitmap)Image.FromFile(filename);
        }

        private void SetFilename(string filename)
        {
            this.openFileDialog1.InitialDirectory = Path.GetDirectoryName(Settings.Default.LastFilename);

            try
            {
                this.textBox1.Text = filename;
                this.pictureBox1.Image = GetImage(filename);

                Settings.Default.LastFilename = filename;
                Settings.Default.Save();
            }
            catch
            {
            }
        }

        private List<BoundingBox> SlideWindow(int windowSize, Bitmap bitmap, Session<float> session, Bitmap output)
        {
            var stopwatch = Stopwatch.StartNew();
            var stride = 5;
            var batchSize = (300 - windowSize) / stride * (300 - windowSize) / stride;
            var shape = new Shape(IMAGE_WIDTH, IMAGE_HEIGHT, 1, batchSize);
            var data = new float[IMAGE_WIDTH * IMAGE_HEIGHT * batchSize];
            var dataLocal = new float[IMAGE_WIDTH * IMAGE_HEIGHT];
            var input = BuilderInstance.Volume.From(data.ToArray(), shape);
            var boxes = new List<BoundingBox>();

            var batch = 0;
            for (var y = 0; y < 300 - windowSize; y += stride)
            {
                for (var x = 0; x < 300 - windowSize; x += stride)
                {
                    // Clone a portion of the Bitmap object.
                    var cloneRect = new Rectangle(x, y, windowSize, windowSize);
                    var cloneBitmap = bitmap.Clone(cloneRect, bitmap.PixelFormat);
                    BitmapTool.ExtractData(IMAGE_WIDTH, IMAGE_HEIGHT, dataLocal, cloneBitmap);

                    for (var j = 0; j < IMAGE_HEIGHT; j++)
                    {
                        for (var i = 0; i < IMAGE_WIDTH; i++)
                        {
                            input.Set(i, j, 0, batch, dataLocal[i + j * IMAGE_WIDTH]);
                        }
                    }

                    var boundingBox = new BoundingBox
                    {
                        x1 = x / (float)bitmap.Width,
                        y1 = y / (float)bitmap.Height,
                        x2 = Math.Min(1.0f, (x + windowSize) / (float)bitmap.Width),
                        y2 = Math.Min(1.0f, (y + windowSize) / (float)bitmap.Height)
                    };
                    boxes.Add(boundingBox);

                    cloneBitmap.Dispose();
                    batch++;
                }
            }

            stopwatch.Stop();
            Debug.WriteLine($"Create - {stopwatch.ElapsedMilliseconds} ms");

            stopwatch = Stopwatch.StartNew();
            var dico = new Dictionary<string, Volume<float>> { { "x", input }, { "dropProb", 0.0f } };
            var result = session.Run(this._yhat, dico);
            Debug.WriteLine($"Evaluate - {stopwatch.ElapsedMilliseconds} ms");


            var okBoxes = new List<BoundingBox>();

            for (var i = 0; i < result.Shape.Dimensions[3]; i++)
            {
                var probFace = result.Get(0, 0, 1, i);
                if (probFace > 0.98f)
                {
                    okBoxes.Add(boxes[i]);
                    //  BitmapTool.DrawBoundingBox(output, boxes[i], Color.FromArgb(50, 0, 0, 255));
                }
            }

            return okBoxes;
        }
    }
}