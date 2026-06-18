namespace CompGraph_DOF.Graphics;

internal enum FramebufferKind
{
    Scene,
    Blur
}

internal sealed class Framebuffer : IDisposable
{
    private readonly FramebufferKind _kind;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public uint Handle { get; private set; }
    public uint ColorTexture { get; private set; }
    public uint DepthTexture { get; private set; }
    public uint ObjectIdTexture { get; private set; }

    public Framebuffer(int width, int height, FramebufferKind kind)
    {
        _kind = kind;
        Recreate(width, height);
    }

    public void Recreate(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Framebuffer dimensions must be positive.");
        }

        DisposeInternal();

        Width = width;
        Height = height;

        GL.GenFramebuffers(1, out uint framebuffer);
        Handle = framebuffer;
        GL.BindFramebuffer(GL.FRAMEBUFFER, Handle);

        ColorTexture = CreateColorTexture(width, height);
        GL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT0, GL.TEXTURE_2D, ColorTexture, 0);

        if (_kind == FramebufferKind.Scene)
        {
            DepthTexture = CreateDepthTexture(width, height);
            ObjectIdTexture = CreateObjectIdTexture(width, height);
            GL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.DEPTH_ATTACHMENT, GL.TEXTURE_2D, DepthTexture, 0);
            GL.FramebufferTexture2D(GL.FRAMEBUFFER, GL.COLOR_ATTACHMENT1, GL.TEXTURE_2D, ObjectIdTexture, 0);
            GL.DrawBuffers(2, new[] { GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT1 });
        }
        else
        {
            DepthTexture = 0;
            ObjectIdTexture = 0;
            GL.DrawBuffers(1, new[] { GL.COLOR_ATTACHMENT0 });
        }

        uint status = GL.CheckFramebufferStatus(GL.FRAMEBUFFER);
        if (status != GL.FRAMEBUFFER_COMPLETE)
        {
            throw new InvalidOperationException($"Framebuffer is incomplete: 0x{status:X8}");
        }

        GL.BindFramebuffer(GL.FRAMEBUFFER, 0);
    }

    public void Bind()
    {
        GL.BindFramebuffer(GL.FRAMEBUFFER, Handle);
    }

    public void Dispose()
    {
        DisposeInternal();
    }

    private uint CreateColorTexture(int width, int height)
    {
        GL.GenTextures(1, out uint texture);
        GL.BindTexture(GL.TEXTURE_2D, texture);
        SetTextureParameters(GL.LINEAR);
        GL.TexImage2D(GL.TEXTURE_2D, 0, (int)GL.RGBA16F, width, height, 0, GL.RGBA, GL.FLOAT, IntPtr.Zero);
        return texture;
    }

    private uint CreateDepthTexture(int width, int height)
    {
        GL.GenTextures(1, out uint texture);
        GL.BindTexture(GL.TEXTURE_2D, texture);
        SetTextureParameters(GL.NEAREST);
        GL.TexImage2D(GL.TEXTURE_2D, 0, (int)GL.DEPTH_COMPONENT32F, width, height, 0, GL.DEPTH_COMPONENT, GL.FLOAT, IntPtr.Zero);
        return texture;
    }

    private uint CreateObjectIdTexture(int width, int height)
    {
        GL.GenTextures(1, out uint texture);
        GL.BindTexture(GL.TEXTURE_2D, texture);
        SetTextureParameters(GL.NEAREST);
        GL.TexImage2D(GL.TEXTURE_2D, 0, (int)GL.R32UI, width, height, 0, GL.RED_INTEGER, GL.UNSIGNED_INT, IntPtr.Zero);
        return texture;
    }

    private static void SetTextureParameters(uint filter)
    {
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, (int)filter);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, (int)filter);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, (int)GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, (int)GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_BASE_LEVEL, 0);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, 0);
    }

    private void DisposeInternal()
    {
        if (ObjectIdTexture != 0)
        {
            uint texture = ObjectIdTexture;
            GL.DeleteTextures(1, ref texture);
            ObjectIdTexture = 0;
        }

        if (DepthTexture != 0)
        {
            uint texture = DepthTexture;
            GL.DeleteTextures(1, ref texture);
            DepthTexture = 0;
        }

        if (ColorTexture != 0)
        {
            uint texture = ColorTexture;
            GL.DeleteTextures(1, ref texture);
            ColorTexture = 0;
        }

        if (Handle != 0)
        {
            uint framebuffer = Handle;
            GL.DeleteFramebuffers(1, ref framebuffer);
            Handle = 0;
        }
    }
}
