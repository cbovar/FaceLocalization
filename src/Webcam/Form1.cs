using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face;

namespace Webcam
{
    /// <summary>
    /// Initial webcam program found here: https://www.youtube.com/watch?v=A4Qcq9GOvGQ
    /// </summary>
    public partial class Form1 : Form
    {
        public const int IMAGE_WIDTH = 256; // Must be the same as when training model
        public const int IMAGE_HEIGHT = 256;

        private FilterInfoCollection _devices;
        private VideoCaptureDevice _frame;
        private string _output;
        private Op<float> _yhat;

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

        private void takeImageButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this._output))
            {
                this.pictureBox.Image?.Save(this._output + "\\Image.png");
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            this._frame.Stop();
            this.pictureBox.Image = null;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Start_cam();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._frame?.Stop();
        }

        private void NewFrame_event(object send, NewFrameEventArgs e)
        {
            try
            {
                var webcamFrame = e.Frame;

                //webcamFrame = (Bitmap)Image.FromFile(@"C:\Pro\Github\FaceLocalisation\Face\Dataset\helen_5\2947090433_1.jpg");

                if (this.queueIn.Count == 0)
                {
                    this.queueIn.Enqueue((Bitmap)webcamFrame.Clone());
                }

                if (this.queueOut.TryDequeue(out var boundingBox))
                {
                    this.last_boundingBox = boundingBox;
                }

                if (this.last_boundingBox != null)
                {
                    BitmapTool.DrawBoundingBox(webcamFrame, this.last_boundingBox, Color.Blue);
                }

                this.pictureBox.Image = (Image)webcamFrame.Clone();
            }
            catch (Exception ex)
            {
            }
        }

        private void StartAI()
        {
            BuilderInstance<float>.Volume = new VolumeBuilder(); // Needed for GPU, must be done on AI thread

            // Must be done after setting BuilderInstance<float>.Volume
            this._yhat = ConvNetSharp.Flow.Serialization.SerializationExtensions.Load<float>("FaceDetection", false)[0];

            var data = new float[IMAGE_WIDTH * IMAGE_HEIGHT];
            var stopwatch = new Stopwatch();

            while (true)
            {
                if (this.queueIn.TryDequeue(out var bitmap))
                {
                    BitmapTool.ExtractData(IMAGE_WIDTH, IMAGE_HEIGHT, data, bitmap);
                    var input = BuilderInstance.Volume.From(data, new Shape(IMAGE_WIDTH, IMAGE_HEIGHT, 1, 1));

                    using (var session = new Session<float>())
                    {
                        var dico = new Dictionary<string, Volume<float>> { { "x", input } };

                        stopwatch.Restart();
                        var result = session.Run(this._yhat, dico);
                        stopwatch.Stop();
                        Debug.WriteLine(stopwatch.ElapsedMilliseconds);

                        //var boundingBox = new BoundingBox { x1 =0.2f + DateTime.Now.Second / 60.0f * 0.5f, y1 = 0.2f, x2 = 0.6f, y2 = 0.6f };
                        var boundingBox = new BoundingBox { x1 = result.Get(0), y1 = result.Get(1), x2 = result.Get(2), y2 = result.Get(3) };
                        this.queueOut.Enqueue(boundingBox);
                    }
                }

            }
        }

        readonly ConcurrentQueue<Bitmap> queueIn = new ConcurrentQueue<Bitmap>();
        readonly ConcurrentQueue<BoundingBox> queueOut = new ConcurrentQueue<BoundingBox>();
        private BoundingBox last_boundingBox;

        private void Start_cam()
        {
            this._devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            this._frame = new VideoCaptureDevice(this._devices[0].MonikerString);
            this._frame.NewFrame += NewFrame_event;
            this._frame.Start();
        }
    }
}