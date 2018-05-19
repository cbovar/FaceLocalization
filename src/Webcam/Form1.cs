using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face;
using Face.Tools;

namespace Webcam
{
    /// <summary>
    ///     Initial webcam program found here: https://www.youtube.com/watch?v=A4Qcq9GOvGQ
    /// </summary>
    public partial class Form1 : Form
    {
        public const int IMAGE_WIDTH = 32; // Must be the same as when training model
        public const int IMAGE_HEIGHT = 32;

        private readonly object locker = new object();

        private readonly ConcurrentQueue<Bitmap> queueIn = new ConcurrentQueue<Bitmap>();
        private readonly ConcurrentQueue<List<BoundingBox>> queueOut = new ConcurrentQueue<List<BoundingBox>>();

        private FilterInfoCollection _devices;
        private VideoCaptureDevice _frame;
        private string _output;
        private Op<float> _yhat;
        private int evaluationCount;
        private List<BoundingBox> last_boundingBoxes;

        public Form1()
        {
            InitializeComponent();

            var currentDirectory = Directory.GetCurrentDirectory();
            this.textBox.Text = currentDirectory;
            this.folderBrowserDialog1.SelectedPath = currentDirectory;
            this._output = currentDirectory;

            Task.Run(() => StartAI());
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog1.ShowDialog();
            this.textBox.Text = this.folderBrowserDialog1.SelectedPath;
            this._output = this.folderBrowserDialog1.SelectedPath;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._frame?.Stop();
        }

        private void NewFrame_event(object send, NewFrameEventArgs e)
        {
            try
            {
                lock (this.locker)
                {
                    var webcamFrame = e.Frame;

                    if (this.queueIn.Count == 0)
                    {
                        this.queueIn.Enqueue((Bitmap)webcamFrame.Clone());
                    }

                    if (this.queueOut.TryDequeue(out var boundingBox))
                    {
                        this.last_boundingBoxes = boundingBox;

                        BeginInvoke(new Action(() => { this.Text = (++this.evaluationCount).ToString(); }));
                    }

                    if (this.last_boundingBoxes != null)
                    {
                        var nms = new NonMaximumSuppresion();
                        var test = nms.Nms(this.last_boundingBoxes, 0.99f);
                        foreach (var box in test)
                        {
                            BitmapTool.DrawBoundingBox(webcamFrame, box, Color.FromArgb(50, 0, 0, 255)); //Color.Blue);
                        }
                    }

                    this.pictureBox.Image = (Image)webcamFrame.Clone();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void SlideWindow(int windowSize, Bitmap bitmap, List<BoundingBox> boxes)
        {
            using (var session = new Session<float>())
            {
                var stopwatch = Stopwatch.StartNew();

                var batchSize = (300 - windowSize) / 10 * (300 - windowSize) / 10;
                var shape = new Shape(IMAGE_WIDTH, IMAGE_HEIGHT, 1, batchSize);
                var data = new float[IMAGE_WIDTH * IMAGE_HEIGHT * batchSize];
                var dataLocal = new float[IMAGE_WIDTH * IMAGE_HEIGHT];
                var input = BuilderInstance.Volume.From(data.ToArray(), shape);
                var allBoxes = new List<BoundingBox>();

                var batch = 0;
                for (var y = 0; y < 300 - windowSize; y += 10)
                {
                    for (var x = 0; x < 300 - windowSize; x += 10)
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
                        allBoxes.Add(boundingBox);

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

                for (var i = 0; i < result.Shape.Dimensions[3]; i++)
                {
                    var probFace = result.Get(0, 0, 1, i);
                    if (probFace > 0.9f)
                    {
                        boxes.Add(allBoxes[i]);
                    }
                }
            }
        }

        private void Start_cam()
        {
            this._devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            this._frame = new VideoCaptureDevice(this._devices[0].MonikerString);

            this._frame.NewFrame += NewFrame_event;
            this._frame.Start();
        }

        private void StartAI()
        {
            BuilderInstance<float>.Volume = new VolumeBuilder(); // Needed for GPU, must be done on AI thread

            // Must be done after setting BuilderInstance<float>.Volume
            this._yhat = SerializationExtensions.Load<float>("FaceDetection", false)[0];

            while (true)
            {
                if (this.queueIn.TryDequeue(out var bitmap))
                {
                    bitmap = BitmapTool.ResizeImage(bitmap, 300, 300);
                    var boxes = new List<BoundingBox>();

                    SlideWindow(40, bitmap, boxes);
                    SlideWindow(80, bitmap, boxes);
                    SlideWindow(120, bitmap, boxes);

                    this.queueOut.Enqueue(boxes);
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Start_cam();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            this._frame.Stop();
            this.pictureBox.Image = null;
        }

        private void takeImageButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this._output))
            {
                this.pictureBox.Image?.Save(this._output + "\\Image.png");
            }
        }
    }
}