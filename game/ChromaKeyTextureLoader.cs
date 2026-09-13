using System.Drawing;
using System.Drawing.Imaging;

public static class ChromaKeyTextureLoader
{
    public static Bitmap Load(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            throw new FileNotFoundException("Texture was not found.", assetPath);
        }

        using Bitmap source = new(assetPath);
        Bitmap texture = new(source.Width, source.Height, PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(texture))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        for (int y = 0; y < texture.Height; y++)
        for (int x = 0; x < texture.Width; x++)
        {
            Color color = texture.GetPixel(x, y);
            if (color.G > 220 && color.R < 50 && color.B < 50)
            {
                texture.SetPixel(x, y, Color.FromArgb(0, color.R, color.G, color.B));
            }
        }

        return texture;
    }
}
