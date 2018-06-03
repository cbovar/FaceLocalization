using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using ConvNetSharp.Flow;
using ConvNetSharp.Flow.Ops;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;

namespace Face.Tools
{
    public class BitmapExtract
    {
        public Bitmap Bitmap;
        public BoundingBox BoundingBox;
        public int WindowSize;
    }

    public static class BitmapTool
    {
        public static Bitmap Capture(Bitmap image, BoundingBox box)
        {
            // Clone a portion of the Bitmap object.
            var cloneRect = new Rectangle((int) (image.Width * box.x1), (int) (image.Height * box.y1), (int) (image.Width * (box.x2 - box.x1)),
                (int) (image.Height * (box.y2 - box.y1)));
            var cloneBitmap = image.Clone(cloneRect, image.PixelFormat);
            return cloneBitmap;
        }

        /// <summary>
        ///     Draw bounding box over the provided image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="boundingBox"></param>
        /// <param name="color"></param>
        public static void DrawBoundingBox(Bitmap image, BoundingBox boundingBox, Color color, bool fill = true)
        {
            var noAlpha = Color.FromArgb(color.R,color.G,color.B);
            var pen = new Pen(noAlpha, 3);
            var brush = new SolidBrush(color);
            using (var g = Graphics.FromImage(image))
            {
                var x1 = (int) Math.Min(Math.Max(0, boundingBox.x1 * image.Width), image.Width);
                var x2 = (int) Math.Min(Math.Max(0, boundingBox.x2 * image.Width), image.Width);
                var y1 = (int) Math.Min(Math.Max(0, boundingBox.y1 * image.Height), image.Height);
                var y2 = (int) Math.Min(Math.Max(0, boundingBox.y2 * image.Height), image.Height);

                if (fill)
                {
                    g.FillRectangle(brush, new Rectangle(x1, y1, x2 - x1, y2 - y1));
                }

                g.DrawLine(pen, x1, y1, x1, y2);
                g.DrawLine(pen, x2, y1, x2, y2);
                g.DrawLine(pen, x1, y1, x2, y1);
                g.DrawLine(pen, x1, y2, x2, y2);

                g.DrawString(Math.Round(boundingBox.Confidence, 3).ToString(), new Font(FontFamily.GenericSansSerif, 16.0f, FontStyle.Regular), new SolidBrush(Color.Black), x1,
                    y2 - 20.0f);
            }
        }

        public static List<BoundingBox> Evaluate(List<BitmapExtract> extracts, Op<float> model, int imageWidth, int imageHeight, float minProba)
        {
            var boxes = new List<BoundingBox>();

            using (var session = new Session<float>())
            {
                // Create input 

                var batchSize = extracts.Count;
                var data = new float[imageWidth * imageHeight * batchSize];
                var shape = new Shape(imageWidth, imageHeight, 1, batchSize);
                var input = BuilderInstance.Volume.From(data.ToArray(), shape);

                var dataLocal = new float[imageWidth * imageHeight];

                for (var b = 0; b < batchSize; b++)
                {
                    ExtractData(imageWidth, imageHeight, dataLocal, extracts[b].Bitmap);

                    for (var j = 0; j < imageHeight; j++)
                    {
                        for (var i = 0; i < imageWidth; i++)
                        {
                            input.Set(i, j, 0, b, dataLocal[i + j * imageWidth]);
                        }
                    }
                }

                // Evaluate network

                var dico = new Dictionary<string, Volume<float>> {{"x", input}, {"dropProb", 0.0f}};
                var result = session.Run(model, dico);

                // Get result

                for (var b = 0; b < batchSize; b++)
                {
                    var probFace = result.Get(0, 0, 1, b);
                    if (probFace > minProba) // Face detected
                    {
                        extracts[b].BoundingBox.Confidence = probFace;
                        boxes.Add(extracts[b].BoundingBox);
                    }
                }
            }

            return boxes;
        }

        public static void ExtractData(int width, int height, float[] data, Bitmap image)
        {
            // Resize image
            image = ResizeImage(image, width, height);

            var imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            var imageBytes = new byte[Math.Abs(imageData.Stride) * image.Height];
            var scan0 = imageData.Scan0;

            Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

            var i = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixelB = imageBytes[x + y * imageData.Stride];
                    var pixelR = imageBytes[x + y * imageData.Stride + 2];
                    var pixelG = imageBytes[x + y * imageData.Stride + 1];

                    var pixel = (byte) ((pixelG + pixelB + pixelR) / 3.0f); // Black and white normalized [0.0 - 1.0]
                    data[i++] = pixel / 255.0f;
                }
            }

            image.UnlockBits(imageData);
        }

        /// <summary>
        ///     Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        /// from https://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            if (image.Width == width && image.Height == height)
            {
                return image as Bitmap;
            }

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static IEnumerable<BitmapExtract> SlideWindow(Bitmap bitmap, int windowSize, int stride)
        {
            for (var y = 0; y < bitmap.Height - windowSize; y += stride)
            {
                for (var x = 0; x < bitmap.Width - windowSize; x += stride)
                {
                    // Clone a portion of the Bitmap object.
                    var cloneRect = new Rectangle(x, y, windowSize, windowSize);
                    var cloneBitmap = bitmap.Clone(cloneRect, bitmap.PixelFormat);

                    yield return new BitmapExtract
                    {
                        BoundingBox = new BoundingBox
                        {
                            x1 = x / (float) bitmap.Width,
                            y1 = y / (float) bitmap.Height,
                            x2 = (x + windowSize) / (float) bitmap.Width,
                            y2 = (y + windowSize) / (float) bitmap.Width
                        },
                        Bitmap = cloneBitmap,
                        WindowSize = windowSize
                    };
                }
            }
        }
    }
}