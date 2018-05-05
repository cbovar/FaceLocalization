using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Face
{
    public static class BitmapTool
    {
        public static void ExtractData(int width, int height, float[] data, Bitmap image)
        {
            // Resize image
            image = ResizeImage(image, width, height);

            var imageData = image.LockBits(new Rectangle(0, 0, image.Width,
                image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

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

                    var pixel = (pixelG + pixelB + pixelR) / 3.0f / 255.0f; // Black and white normalized [0.0 - 1.0]
                    data[i++] = pixel;
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

        public static void DrawBoundingBox(Bitmap image, BoundingBox boundingBox, Color color)
        {
            var pen = new Pen(color, 2);

            using (var g = Graphics.FromImage(image))
            {
                var x1 = Math.Min(Math.Max(0, boundingBox.x1 * image.Width), image.Width);
                var x2 = Math.Min(Math.Max(0, boundingBox.x2 * image.Width), image.Width);
                var y1 = Math.Min(Math.Max(0, boundingBox.y1 * image.Height), image.Height);
                var y2 = Math.Min(Math.Max(0, boundingBox.y2 * image.Height), image.Height);

                g.DrawLine(pen, x1, y1, x1, y2);
                g.DrawLine(pen, x2, y1, x2, y2);
                g.DrawLine(pen, x1, y1, x2, y1);
                g.DrawLine(pen, x1, y2, x2, y2);
            }
        }
    }
}