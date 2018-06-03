using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Face.Tools
{
    public class PgmImage
    {
        public int Height;

        public int MaxVal;

        public byte[][] Pixels;

        public int Width;

        public PgmImage(int width, int height, int maxVal, byte[][] pixels)
        {
            this.Width = width;
            this.Height = height;
            this.MaxVal = maxVal;
            this.Pixels = pixels;
        }

        public static PgmImage LoadImage(string file)
        {
            var ifs = new FileStream(file, FileMode.Open);
            var br = new BinaryReader(ifs);

            var magic = NextNonCommentLine(br);
            if (magic != "P5")
            {
                throw new Exception("Unknown magic number: " + magic);
            }

            var widthHeight = NextNonCommentLine(br);
            var tokens = widthHeight.Split(' ');
            var width = int.Parse(tokens[0]);
            var height = int.Parse(tokens[1]);

            var sMaxVal = NextNonCommentLine(br);
            var maxVal = int.Parse(sMaxVal);

            // read width * height pixel values . . .
            var pixels = new byte[height][];
            for (var i = 0; i < height; ++i)
            {
                pixels[i] = new byte[width];
            }

            for (var i = 0; i < height; ++i)
            {
                for (var j = 0; j < width; ++j)
                {
                    pixels[i][j] = br.ReadByte();
                }
            }

            br.Close();
            ifs.Close();

            var result = new PgmImage(width, height, maxVal, pixels);
            return result;
        }

        public static Bitmap MakeBitmap(PgmImage pgmImage)
        {
            var width = pgmImage.Width;
            var height = pgmImage.Height;

            var image = new Bitmap(width, height);

            var imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            var imageBytes = new byte[Math.Abs(imageData.Stride) * image.Height];
            var scan0 = imageData.Scan0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    byte pixelColor = pgmImage.Pixels[y][x];

                    imageBytes[x * 3 + y * imageData.Stride] = pixelColor;
                    imageBytes[x * 3 + y * imageData.Stride + 2]  = pixelColor;
                    imageBytes[x * 3 + y * imageData.Stride + 1] = pixelColor;
                }
            }

            Marshal.Copy(imageBytes, 0, scan0, imageBytes.Length);

            image.UnlockBits(imageData);

            return image;
        }

        private static string NextAnyLine(BinaryReader br)
        {
            var s = "";
            byte b = 0; // dummy
            while (b != 10) // newline
            {
                b = br.ReadByte();
                var c = (char)b;
                s += c;
            }

            return s.Trim();
        }

        private static string NextNonCommentLine(BinaryReader br)
        {
            var s = NextAnyLine(br);
            while (s.StartsWith("#") || s == "")
            {
                s = NextAnyLine(br);
            }

            return s;
        }
    }
}