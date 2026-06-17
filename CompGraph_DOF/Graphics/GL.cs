using System.Runtime.InteropServices;

namespace CompGraph_DOF.Graphics;

internal static class GL
{
    public const uint FALSE = 0;
    public const uint TRUE = 1;

    public const uint BYTE = 0x1400;
    public const uint UNSIGNED_BYTE = 0x1401;
    public const uint SHORT = 0x1402;
    public const uint UNSIGNED_SHORT = 0x1403;
    public const uint INT = 0x1404;
    public const uint UNSIGNED_INT = 0x1405;
    public const uint FLOAT = 0x1406;

    public const uint DEPTH_COMPONENT = 0x1902;
    public const uint RGBA = 0x1908;
    public const uint RED_INTEGER = 0x8D94;
    public const uint RGBA8 = 0x8058;
    public const uint RGBA16F = 0x881A;
    public const uint R32UI = 0x8236;
    public const uint DEPTH_COMPONENT24 = 0x81A6;
    public const uint DEPTH_COMPONENT32F = 0x8CAC;

    public const uint COLOR_BUFFER_BIT = 0x00004000;
    public const uint DEPTH_BUFFER_BIT = 0x00000100;

    public const uint NONE = 0;

    public const uint TRIANGLES = 0x0004;

    public const uint ARRAY_BUFFER = 0x8892;
    public const uint ELEMENT_ARRAY_BUFFER = 0x8893;
    public const uint STATIC_DRAW = 0x88E4;

    public const uint TEXTURE_2D = 0x0DE1;
    public const uint TEXTURE0 = 0x84C0;
    public const uint TEXTURE_MIN_FILTER = 0x2801;
    public const uint TEXTURE_MAG_FILTER = 0x2800;
    public const uint TEXTURE_WRAP_S = 0x2802;
    public const uint TEXTURE_WRAP_T = 0x2803;
    public const uint TEXTURE_BASE_LEVEL = 0x813C;
    public const uint TEXTURE_MAX_LEVEL = 0x813D;
    public const uint LINEAR = 0x2601;
    public const uint NEAREST = 0x2600;
    public const uint CLAMP_TO_EDGE = 0x812F;

    public const uint FRAMEBUFFER = 0x8D40;
    public const uint READ_FRAMEBUFFER = 0x8CA8;
    public const uint DRAW_FRAMEBUFFER = 0x8CA9;
    public const uint COLOR_ATTACHMENT0 = 0x8CE0;
    public const uint COLOR_ATTACHMENT1 = 0x8CE1;
    public const uint DEPTH_ATTACHMENT = 0x8D00;
    public const uint FRAMEBUFFER_COMPLETE = 0x8CD5;

    public const uint VERTEX_SHADER = 0x8B31;
    public const uint FRAGMENT_SHADER = 0x8B30;
    public const uint COMPILE_STATUS = 0x8B81;
    public const uint LINK_STATUS = 0x8B82;
    public const uint INFO_LOG_LENGTH = 0x8B84;

    public const uint DEPTH_TEST = 0x0B71;
    public const uint CULL_FACE = 0x0B44;
    public const uint BACK = 0x0405;
    public const uint CCW = 0x0901;
    public const uint LESS = 0x0201;

    public const uint COLOR = 0x1800;
    public const uint DEPTH = 0x1801;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlViewport(int x, int y, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlClearBufferfv(uint buffer, int drawbuffer, float[] value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlClearBufferuiv(uint buffer, int drawbuffer, uint[] value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlEnable(uint cap);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDisable(uint cap);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDepthFunc(uint func);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlCullFace(uint mode);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlFrontFace(uint mode);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGenVertexArrays(int n, out uint arrays);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBindVertexArray(uint array);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteVertexArrays(int n, ref uint arrays);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGenBuffers(int n, out uint buffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBindBuffer(uint target, uint buffer);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBufferData(uint target, nint size, IntPtr data, uint usage);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteBuffers(int n, ref uint buffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlEnableVertexAttribArray(uint index);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlVertexAttribPointer(uint index, int size, uint type, int normalized, int stride, IntPtr pointer);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlVertexAttribIPointer(uint index, int size, uint type, int stride, IntPtr pointer);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate uint GlCreateShader(uint type);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlShaderSource(uint shader, int count, IntPtr[] strings, int[] lengths);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlCompileShader(uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGetShaderiv(uint shader, uint pname, out int param);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGetShaderInfoLog(uint shader, int bufSize, out int length, System.Text.StringBuilder infoLog);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteShader(uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate uint GlCreateProgram();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlAttachShader(uint program, uint shader);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlLinkProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGetProgramiv(uint program, uint pname, out int param);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGetProgramInfoLog(uint program, int bufSize, out int length, System.Text.StringBuilder infoLog);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUseProgram(uint program);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi)]
    internal delegate int GlGetUniformLocation(uint program, string name);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniformMatrix4fv(int location, int count, int transpose, float[] value);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniform1i(int location, int v0);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniform1ui(int location, uint v0);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniform1f(int location, float v0);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniform2f(int location, float v0, float v1);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlUniform3f(int location, float v0, float v1, float v2);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlActiveTexture(uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGenTextures(int n, out uint textures);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBindTexture(uint target, uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlTexParameteri(uint target, uint pname, int param);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteTextures(int n, ref uint textures);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlGenFramebuffers(int n, out uint framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBindFramebuffer(uint target, uint framebuffer);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate uint GlCheckFramebufferStatus(uint target);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDeleteFramebuffers(int n, ref uint framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDrawBuffers(int n, uint[] buffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlReadBuffer(uint src);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlBlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDrawArrays(uint mode, int first, int count);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GlDrawElements(uint mode, int count, uint type, IntPtr indices);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IntPtr GlGetString(uint name);

    public static GlViewport Viewport = null!;
    public static GlClearBufferfv ClearBufferfv = null!;
    public static GlClearBufferuiv ClearBufferuiv = null!;
    public static GlEnable Enable = null!;
    public static GlDisable Disable = null!;
    public static GlDepthFunc DepthFunc = null!;
    public static GlCullFace CullFace = null!;
    public static GlFrontFace FrontFace = null!;
    public static GlGenVertexArrays GenVertexArrays = null!;
    public static GlBindVertexArray BindVertexArray = null!;
    public static GlDeleteVertexArrays DeleteVertexArrays = null!;
    public static GlGenBuffers GenBuffers = null!;
    public static GlBindBuffer BindBuffer = null!;
    public static GlBufferData BufferData = null!;
    public static GlDeleteBuffers DeleteBuffers = null!;
    public static GlEnableVertexAttribArray EnableVertexAttribArray = null!;
    public static GlVertexAttribPointer VertexAttribPointer = null!;
    public static GlVertexAttribIPointer VertexAttribIPointer = null!;
    public static GlCreateShader CreateShader = null!;
    public static GlShaderSource ShaderSource = null!;
    public static GlCompileShader CompileShader = null!;
    public static GlGetShaderiv GetShaderiv = null!;
    public static GlGetShaderInfoLog GetShaderInfoLog = null!;
    public static GlDeleteShader DeleteShader = null!;
    public static GlCreateProgram CreateProgram = null!;
    public static GlAttachShader AttachShader = null!;
    public static GlLinkProgram LinkProgram = null!;
    public static GlGetProgramiv GetProgramiv = null!;
    public static GlGetProgramInfoLog GetProgramInfoLog = null!;
    public static GlDeleteProgram DeleteProgram = null!;
    public static GlUseProgram UseProgram = null!;
    public static GlGetUniformLocation GetUniformLocation = null!;
    public static GlUniformMatrix4fv UniformMatrix4fv = null!;
    public static GlUniform1i Uniform1i = null!;
    public static GlUniform1ui Uniform1ui = null!;
    public static GlUniform1f Uniform1f = null!;
    public static GlUniform2f Uniform2f = null!;
    public static GlUniform3f Uniform3f = null!;
    public static GlActiveTexture ActiveTexture = null!;
    public static GlGenTextures GenTextures = null!;
    public static GlBindTexture BindTexture = null!;
    public static GlTexParameteri TexParameteri = null!;
    public static GlTexImage2D TexImage2D = null!;
    public static GlDeleteTextures DeleteTextures = null!;
    public static GlGenFramebuffers GenFramebuffers = null!;
    public static GlBindFramebuffer BindFramebuffer = null!;
    public static GlFramebufferTexture2D FramebufferTexture2D = null!;
    public static GlCheckFramebufferStatus CheckFramebufferStatus = null!;
    public static GlDeleteFramebuffers DeleteFramebuffers = null!;
    public static GlDrawBuffers DrawBuffers = null!;
    public static GlReadBuffer ReadBuffer = null!;
    public static GlReadPixels ReadPixels = null!;
    public static GlBlitFramebuffer BlitFramebuffer = null!;
    public static GlDrawArrays DrawArrays = null!;
    public static GlDrawElements DrawElements = null!;
    public static GlGetString GetString = null!;
}
