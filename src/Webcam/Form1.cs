using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face;
using Face.Tools;
using MoreLinq;

namespace Webcam
{
    /// <summary>
    ///     Initial webcam program found here: https://www.youtube.com/watch?v=A4Qcq9GOvGQ
    /// </summary>
    public partial class Form1 : Form
    {
        public const int IMAGE_WIDTH = 32; // Must be the same as when training model
        public const int IMAGE_HEIGHT = 32;

        private readonly object _locker = new object();

        private readonly ConcurrentQueue<Bitmap> _queueIn = new ConcurrentQueue<Bitmap>();
        private readonly ConcurrentQueue<List<BoundingBox>> _queueOut = new ConcurrentQueue<List<BoundingBox>>();
        private int _evaluationCount;

        private VideoCaptureDevice _frame;
        private List<BoundingBox> _lastBoundingBoxes;
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._frame?.Stop();
        }

        private void NewFrame_event(object send, NewFrameEventArgs e)
        {
            try
            {
                lock (this._locker)
                {
                    var webcamFrame = e.Frame;

                    if (this._queueIn.Count == 0)
                    {
                        this._queueIn.Enqueue((Bitmap)webcamFrame.Clone());
                    }

                    if (this._queueOut.TryDequeue(out var boundingBox))
                    {
                        this._lastBoundingBoxes = boundingBox;

                        BeginInvoke(new Action(() => { this.Text = (++this._evaluationCount).ToString(); }));
                    }

                    if (this._lastBoundingBoxes != null)
                    {
                        foreach (var box in this._lastBoundingBoxes)
                        {
                            BitmapTool.DrawBoundingBox(webcamFrame, box, Color.FromArgb(50, 0, 0, 255), false);
                        }

                        var nms = new NonMaximumSuppresion();
                        var result = nms.Nms(this._lastBoundingBoxes, 0.20f);
                        foreach (var box in result)
                        {
                            BitmapTool.DrawBoundingBox(webcamFrame, box, Color.FromArgb(50, 0, 255, 0), false);
                        }

                    }

                    this.pictureBox.Image = (Image)webcamFrame.Clone();
                }
            }
            catch
            {
            }
        }

        private void Start_cam()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            this._frame = new VideoCaptureDevice(devices[0].MonikerString);

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
                if (this._queueIn.TryDequeue(out var bitmap))
                {
                    bitmap = BitmapTool.ResizeImage(bitmap, 300, 300);
                    var boxes = new List<BoundingBox>();


                    var allExtracts =
                        BitmapTool.SlideWindow(bitmap, 100, 100 / 9);
                       // .Concat(BitmapTool.SlideWindow(bitmap, 40, 40 / 3));
                        //.Concat(BitmapTool.SlideWindow(bitmap, 50, 50/3));

                    foreach (var extracts in allExtracts.Batch(10))
                    {
                        boxes.AddRange(BitmapTool.Evaluate(extracts.ToList(), this._yhat, IMAGE_WIDTH, IMAGE_HEIGHT, 0.8f));
                    }
                    this._queueOut.Enqueue(boxes);
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