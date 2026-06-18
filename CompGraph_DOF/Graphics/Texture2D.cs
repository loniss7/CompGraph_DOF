using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompGraph_DOF.Graphics;

internal sealed class Texture2D : IDisposable
{
    public uint Handle { get; }
    public int Width { get; }
    public int Height { get; }

    private Texture2D(uint handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    public static Texture2D LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Texture file was not found: {path}", path);
        }

        var decoder = BitmapDecoder.Create(
            new Uri(path, UriKind.Absolute),
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        BitmapSource source = decoder.Frames[0];
        if (source.Format != PixelFormats.Bgra32)
        {
            source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        }

        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        byte[] flippedPixels = new byte[pixels.Length];
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(pixels, y * stride, flippedPixels, (height - 1 - y) * stride, stride);
        }

        GCHandle pinnedPixels = GCHandle.Alloc(flippedPixels, GCHandleType.Pinned);
        try
        {
            GL.GenTextures(1, out uint texture);
            GL.BindTexture(GL.TEXTURE_2D, texture);
            SetTextureParameters();
            GL.TexImage2D(
                GL.TEXTURE_2D,
                0,
                (int)GL.RGBA8,
                width,
                height,
                0,
                GL.BGRA,
                GL.UNSIGNED_BYTE,
                pinnedPixels.AddrOfPinnedObject());

            return new Texture2D(texture, width, height);
        }
        finally
        {
            pinnedPixels.Free();
        }
    }

    public void Bind(uint textureUnit = 0)
    {
        GL.ActiveTexture(GL.TEXTURE0 + textureUnit);
        GL.BindTexture(GL.TEXTURE_2D, Handle);
    }

    public void Dispose()
    {
        if (Handle == 0)
        {
            return;
        }

        uint texture = Handle;
        GL.DeleteTextures(1, ref texture);
    }

    private static void SetTextureParameters()
    {
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, (int)GL.LINEAR);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, (int)GL.LINEAR);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, (int)GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, (int)GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_BASE_LEVEL, 0);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, 0);
    }
}
