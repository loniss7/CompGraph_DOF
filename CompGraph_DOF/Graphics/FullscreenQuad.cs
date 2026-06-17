using System.Runtime.InteropServices;
using System.Numerics;

namespace CompGraph_DOF.Graphics;

internal sealed class FullscreenQuad : IDisposable
{
    public uint VertexArray { get; }
    public uint VertexBuffer { get; }

    public FullscreenQuad()
    {
        QuadVertex[] quadVertices =
        {
            new(new Vector2(-1f, -1f), new Vector2(0f, 0f)),
            new(new Vector2( 1f, -1f), new Vector2(1f, 0f)),
            new(new Vector2( 1f,  1f), new Vector2(1f, 1f)),
            new(new Vector2(-1f, -1f), new Vector2(0f, 0f)),
            new(new Vector2( 1f,  1f), new Vector2(1f, 1f)),
            new(new Vector2(-1f,  1f), new Vector2(0f, 1f))
        };

        GL.GenVertexArrays(1, out uint vao);
        GL.GenBuffers(1, out uint vbo);
        VertexArray = vao;
        VertexBuffer = vbo;

        GL.BindVertexArray(VertexArray);
        GL.BindBuffer(GL.ARRAY_BUFFER, VertexBuffer);
        GCHandle handle = GCHandle.Alloc(quadVertices, GCHandleType.Pinned);
        try
        {
            GL.BufferData(GL.ARRAY_BUFFER, (nint)(quadVertices.Length * Marshal.SizeOf<QuadVertex>()), handle.AddrOfPinnedObject(), GL.STATIC_DRAW);
        }
        finally
        {
            handle.Free();
        }

        int stride = Marshal.SizeOf<QuadVertex>();
        int uvOffset = (int)Marshal.OffsetOf<QuadVertex>(nameof(QuadVertex.TexCoord));

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, GL.FLOAT, 0, stride, IntPtr.Zero);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, GL.FLOAT, 0, stride, (IntPtr)uvOffset);

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(VertexArray);
        GL.DrawArrays(GL.TRIANGLES, 0, 6);
    }

    public void Dispose()
    {
        if (VertexBuffer != 0)
        {
            uint buffer = VertexBuffer;
            GL.DeleteBuffers(1, ref buffer);
        }

        if (VertexArray != 0)
        {
            uint vao = VertexArray;
            GL.DeleteVertexArrays(1, ref vao);
        }
    }
}
