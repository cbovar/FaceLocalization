using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Flow.Serialization;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using Face;
using Face.Tools;
using MoreLinq;
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

        List<Bitmap> currentBoundingBoxes = new List<Bitmap>();

        private void evaluate_Click(object sender, EventArgs e)
        {
            var bitmap = GetImage(this.textBox1.Text);
            var output = (Bitmap)bitmap.Clone();
            bitmap = BitmapTool.ResizeImage(bitmap, 300, 300);

            bitmap.Save("input.jpg");

            var boxes = new List<BoundingBox>();

            int divisor = (int) this.divisorUpDown.Value;

            var allExtracts = BitmapTool.SlideWindow(bitmap, 140, 140 / divisor)
                .Concat(BitmapTool.SlideWindow(bitmap, 100, 100 / divisor))
                .Concat(BitmapTool.SlideWindow(bitmap, 80, 80 / divisor))
                .Concat(BitmapTool.SlideWindow(bitmap, 30, 30/ divisor));

            foreach (var extracts in allExtracts.Batch(20))
            {
                boxes.AddRange(BitmapTool.Evaluate(extracts.ToList(), this._yhat, IMAGE_WIDTH, IMAGE_HEIGHT, (float)this.minProbaUpDown.Value / 100.0f));
            }

            this.currentBoundingBoxes.Clear();
            var nms = new NonMaximumSuppresion();
            var result = nms.Nms(boxes, (float)this.nmsUpDown.Value / 100.0f);
            foreach (var boundingBox in result)
            {
                BitmapTool.DrawBoundingBox(output, boundingBox, Color.FromArgb(50, 0, 0, 255), false);
                this.currentBoundingBoxes.Add(BitmapTool.Capture(bitmap, boundingBox));
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

        private void dumpBoundingBox_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.currentBoundingBoxes.Count; i++)
            {
                this.currentBoundingBoxes[i].Save($"BoundingBox_{DateTime.Now.Ticks}_{i}.jpg");
            }
        }
    }
}