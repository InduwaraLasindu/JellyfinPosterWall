using System.Drawing;

namespace PosterWall
{
    public static class CollageGenerator
    {
        public static void CreateCollage(string[] posterPaths, string outputFile, int cols = 5, int rows = 4, int width = 400, int height = 600)
        {
            using var bmp = new Bitmap(cols * width, rows * height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);

            for (int i = 0; i < Math.Min(cols * rows, posterPaths.Length); i++)
            {
                using var img = Image.FromFile(posterPaths[i]);
                g.DrawImage(img, (i % cols) * width, (i / cols) * height, width, height);
            }

            bmp.Save(outputFile);
        }
    }
}
