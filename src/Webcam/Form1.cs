using System;
using System.Collections.Generic;
using System.Drawing;
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
        public const int IMAGE_WIDTH = 64; // Must be the same as when training model
        public const int IMAGE_HEIGHT = 64;

        private FilterInfoCollection _devices;
        private VideoCaptureDevice _frame;
        private string _output;
        private readonly Op<float> _yhat;

        public Form1()
        {
            InitializeComponent();

            this._yhat = ConvNetSharp.Flow.Serialization.SerializationExtensions.Load<float>("FaceDetection", false)[0];
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog1.ShowDialog();
            this.textBox.Text = this.folderBrowserDialog1.SelectedPath;
            this._output = this.folderBrowserDialog1.SelectedPath;
        }

        private void takeImageButton_Click(object sender, EventArgs e)
        {
            if (this._output != "")
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
            this._frame.Stop();
        }

        private void NewFrame_event(object send, NewFrameEventArgs e)
        {
            try
            {
                var data = new float[IMAGE_WIDTH * IMAGE_HEIGHT];
                BitmapTool.ExtractData(IMAGE_WIDTH, IMAGE_HEIGHT, data, e.Frame);

                var input = BuilderInstance.Volume.From(data, new Shape(IMAGE_WIDTH, IMAGE_HEIGHT, 1, 1));

                using (var session = new Session<float>())
                {
                    var dico = new Dictionary<string, Volume<float>> {{"x", input}};
                    var result = session.Run(this._yhat, dico);

                    BitmapTool.DrawBoundingBox(e.Frame, new BoundingBox { x1 = result.Get(0), y1 = result.Get(1), x2 = result.Get(2), y2 = result.Get(3) }, Color.Blue);
                }

                this.pictureBox.Image = (Image)e.Frame.Clone();

            }
            catch (Exception ex)
            {
            }
        }

        private void Start_cam()
        {
            this._devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            this._frame = new VideoCaptureDevice(this._devices[0].MonikerString);
            this._frame.NewFrame += NewFrame_event;
            this._frame.Start();
        }
    }
}