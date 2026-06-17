using System.Runtime.InteropServices;
using System.Numerics;

namespace CompGraph_DOF.Graphics;

[StructLayout(LayoutKind.Sequential)]
internal struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;

    public Vertex(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct QuadVertex
{
    public Vector2 Position;
    public Vector2 TexCoord;

    public QuadVertex(Vector2 position, Vector2 texCoord)
    {
        Position = position;
        TexCoord = texCoord;
    }
}

internal sealed class Mesh : IDisposable
{
    public uint VertexArray { get; }
    public uint VertexBuffer { get; }
    public uint IndexBuffer { get; }
    public int IndexCount { get; }

    public Mesh(Vertex[] vertices, uint[] indices)
    {
        IndexCount = indices.Length;

        GL.GenVertexArrays(1, out uint vao);
        GL.GenBuffers(1, out uint vbo);
        GL.GenBuffers(1, out uint ebo);
        VertexArray = vao;
        VertexBuffer = vbo;
        IndexBuffer = ebo;

        GL.BindVertexArray(VertexArray);

        GL.BindBuffer(GL.ARRAY_BUFFER, VertexBuffer);
        GCHandle vertexHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        try
        {
            GL.BufferData(GL.ARRAY_BUFFER, (nint)(vertices.Length * Marshal.SizeOf<Vertex>()), vertexHandle.AddrOfPinnedObject(), GL.STATIC_DRAW);
        }
        finally
        {
            vertexHandle.Free();
        }

        GL.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, IndexBuffer);
        GCHandle indexHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        try
        {
            GL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (nint)(indices.Length * sizeof(uint)), indexHandle.AddrOfPinnedObject(), GL.STATIC_DRAW);
        }
        finally
        {
            indexHandle.Free();
        }

        int stride = Marshal.SizeOf<Vertex>();
        int normalOffset = (int)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal));

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, GL.FLOAT, 0, stride, IntPtr.Zero);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, GL.FLOAT, 0, stride, (IntPtr)normalOffset);

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(VertexArray);
        GL.DrawElements(GL.TRIANGLES, IndexCount, GL.UNSIGNED_INT, IntPtr.Zero);
    }

    public void Dispose()
    {
        if (IndexBuffer != 0)
        {
            uint buffer = IndexBuffer;
            GL.DeleteBuffers(1, ref buffer);
        }

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

    public static Mesh CreatePlane()
    {
        Vertex[] vertices =
        {
            new(new Vector3(-0.5f, 0f, -0.5f), Vector3.UnitY),
            new(new Vector3( 0.5f, 0f, -0.5f), Vector3.UnitY),
            new(new Vector3( 0.5f, 0f,  0.5f), Vector3.UnitY),
            new(new Vector3(-0.5f, 0f,  0.5f), Vector3.UnitY)
        };

        uint[] indices = { 0, 2, 1, 0, 3, 2 };
        return new Mesh(vertices, indices);
    }

    public static Mesh CreateCube()
    {
        Vector3 p0 = new(-0.5f, -0.5f, 0.5f);
        Vector3 p1 = new(0.5f, -0.5f, 0.5f);
        Vector3 p2 = new(0.5f, 0.5f, 0.5f);
        Vector3 p3 = new(-0.5f, 0.5f, 0.5f);
        Vector3 p4 = new(-0.5f, -0.5f, -0.5f);
        Vector3 p5 = new(0.5f, -0.5f, -0.5f);
        Vector3 p6 = new(0.5f, 0.5f, -0.5f);
        Vector3 p7 = new(-0.5f, 0.5f, -0.5f);

        Vertex[] vertices =
        {
            new(p0, Vector3.UnitZ), new(p1, Vector3.UnitZ), new(p2, Vector3.UnitZ), new(p3, Vector3.UnitZ),
            new(p5, -Vector3.UnitZ), new(p4, -Vector3.UnitZ), new(p7, -Vector3.UnitZ), new(p6, -Vector3.UnitZ),
            new(p4, -Vector3.UnitX), new(p0, -Vector3.UnitX), new(p3, -Vector3.UnitX), new(p7, -Vector3.UnitX),
            new(p1, Vector3.UnitX), new(p5, Vector3.UnitX), new(p6, Vector3.UnitX), new(p2, Vector3.UnitX),
            new(p3, Vector3.UnitY), new(p2, Vector3.UnitY), new(p6, Vector3.UnitY), new(p7, Vector3.UnitY),
            new(p4, -Vector3.UnitY), new(p5, -Vector3.UnitY), new(p1, -Vector3.UnitY), new(p0, -Vector3.UnitY)
        };

        uint[] indices =
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        };

        return new Mesh(vertices, indices);
    }

    public static Mesh CreateSphere(int slices = 32, int stacks = 16)
    {
        if (slices < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(slices));
        }

        if (stacks < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(stacks));
        }

        var vertices = new List<Vertex>((slices + 1) * (stacks + 1));
        var indices = new List<uint>(slices * stacks * 6);

        for (int stack = 0; stack <= stacks; stack++)
        {
            float v = stack / (float)stacks;
            float phi = v * MathF.PI;
            float y = MathF.Cos(phi);
            float ringRadius = MathF.Sin(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                float u = slice / (float)slices;
                float theta = u * MathF.PI * 2f;

                Vector3 normal = new(
                    ringRadius * MathF.Cos(theta),
                    y,
                    ringRadius * MathF.Sin(theta));

                vertices.Add(new Vertex(normal * 0.5f, Vector3.Normalize(normal)));
            }
        }

        int stride = slices + 1;
        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                uint first = (uint)(stack * stride + slice);
                uint second = first + (uint)stride;

                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(second);

                indices.Add(first + 1);
                indices.Add(second + 1);
                indices.Add(second);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}
