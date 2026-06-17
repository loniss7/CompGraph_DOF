using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Numerics;

namespace CompGraph_DOF.Graphics;

internal sealed class ShaderProgram : IDisposable
{
    private readonly Dictionary<string, int> _uniformLocations = new(StringComparer.Ordinal);

    public uint Handle { get; }

    public ShaderProgram(string vertexShaderPath, string fragmentShaderPath)
    {
        uint vertexShader = CompileShader(GL.VERTEX_SHADER, File.ReadAllText(vertexShaderPath));
        uint fragmentShader = CompileShader(GL.FRAGMENT_SHADER, File.ReadAllText(fragmentShaderPath));

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgramiv(Handle, GL.LINK_STATUS, out int linkStatus);
        if (linkStatus == 0)
        {
            throw new InvalidOperationException(
                $"Failed to link shader program '{Path.GetFileNameWithoutExtension(vertexShaderPath)}/{Path.GetFileNameWithoutExtension(fragmentShaderPath)}':\n{GetProgramLog(Handle)}");
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetMatrix4(string name, Matrix4x4 value)
    {
        float[] data = new float[]
        {
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44
        };

        GL.UniformMatrix4fv(GetUniformLocation(name), 1, 1, data);
    }

    public void SetVector2(string name, Vector2 value)
    {
        GL.Uniform2f(GetUniformLocation(name), value.X, value.Y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3f(GetUniformLocation(name), value.X, value.Y, value.Z);
    }

    public void SetFloat(string name, float value)
    {
        GL.Uniform1f(GetUniformLocation(name), value);
    }

    public void SetInt(string name, int value)
    {
        GL.Uniform1i(GetUniformLocation(name), value);
    }

    public void SetUInt(string name, uint value)
    {
        GL.Uniform1ui(GetUniformLocation(name), value);
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            GL.DeleteProgram(Handle);
        }
    }

    private int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out int location))
        {
            return location;
        }

        location = GL.GetUniformLocation(Handle, name);
        _uniformLocations[name] = location;
        return location;
    }

    private static uint CompileShader(uint shaderType, string source)
    {
        uint shader = GL.CreateShader(shaderType);
        IntPtr sourcePtr = Marshal.StringToHGlobalAnsi(source);
        try
        {
            GL.ShaderSource(shader, 1, new[] { sourcePtr }, new[] { source.Length });
            GL.CompileShader(shader);
            GL.GetShaderiv(shader, GL.COMPILE_STATUS, out int status);
            if (status == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to compile {(shaderType == GL.VERTEX_SHADER ? "vertex" : "fragment")} shader:\n{GetShaderLog(shader)}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(sourcePtr);
        }

        return shader;
    }

    private static string GetShaderLog(uint shader)
    {
        GL.GetShaderiv(shader, GL.INFO_LOG_LENGTH, out int length);
        if (length <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length);
        GL.GetShaderInfoLog(shader, length, out _, builder);
        return builder.ToString();
    }

    private static string GetProgramLog(uint program)
    {
        GL.GetProgramiv(program, GL.INFO_LOG_LENGTH, out int length);
        if (length <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length);
        GL.GetProgramInfoLog(program, length, out _, builder);
        return builder.ToString();
    }
}
