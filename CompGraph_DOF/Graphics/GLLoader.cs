using System.Runtime.InteropServices;
using CompGraph_DOF.Native;

namespace CompGraph_DOF.Graphics;

internal static class GLLoader
{
    private static nint s_opengl32Module;

    public static void LoadAll()
    {
        EnsureModule();

        GL.Viewport = Load<GL.GlViewport>("glViewport");
        GL.ClearBufferfv = Load<GL.GlClearBufferfv>("glClearBufferfv");
        GL.ClearBufferuiv = Load<GL.GlClearBufferuiv>("glClearBufferuiv");
        GL.Enable = Load<GL.GlEnable>("glEnable");
        GL.Disable = Load<GL.GlDisable>("glDisable");
        GL.DepthFunc = Load<GL.GlDepthFunc>("glDepthFunc");
        GL.CullFace = Load<GL.GlCullFace>("glCullFace");
        GL.FrontFace = Load<GL.GlFrontFace>("glFrontFace");
        GL.GenVertexArrays = Load<GL.GlGenVertexArrays>("glGenVertexArrays");
        GL.BindVertexArray = Load<GL.GlBindVertexArray>("glBindVertexArray");
        GL.DeleteVertexArrays = Load<GL.GlDeleteVertexArrays>("glDeleteVertexArrays");
        GL.GenBuffers = Load<GL.GlGenBuffers>("glGenBuffers");
        GL.BindBuffer = Load<GL.GlBindBuffer>("glBindBuffer");
        GL.BufferData = Load<GL.GlBufferData>("glBufferData");
        GL.DeleteBuffers = Load<GL.GlDeleteBuffers>("glDeleteBuffers");
        GL.EnableVertexAttribArray = Load<GL.GlEnableVertexAttribArray>("glEnableVertexAttribArray");
        GL.VertexAttribPointer = Load<GL.GlVertexAttribPointer>("glVertexAttribPointer");
        GL.VertexAttribIPointer = Load<GL.GlVertexAttribIPointer>("glVertexAttribIPointer");
        GL.CreateShader = Load<GL.GlCreateShader>("glCreateShader");
        GL.ShaderSource = Load<GL.GlShaderSource>("glShaderSource");
        GL.CompileShader = Load<GL.GlCompileShader>("glCompileShader");
        GL.GetShaderiv = Load<GL.GlGetShaderiv>("glGetShaderiv");
        GL.GetShaderInfoLog = Load<GL.GlGetShaderInfoLog>("glGetShaderInfoLog");
        GL.DeleteShader = Load<GL.GlDeleteShader>("glDeleteShader");
        GL.CreateProgram = Load<GL.GlCreateProgram>("glCreateProgram");
        GL.AttachShader = Load<GL.GlAttachShader>("glAttachShader");
        GL.LinkProgram = Load<GL.GlLinkProgram>("glLinkProgram");
        GL.GetProgramiv = Load<GL.GlGetProgramiv>("glGetProgramiv");
        GL.GetProgramInfoLog = Load<GL.GlGetProgramInfoLog>("glGetProgramInfoLog");
        GL.DeleteProgram = Load<GL.GlDeleteProgram>("glDeleteProgram");
        GL.UseProgram = Load<GL.GlUseProgram>("glUseProgram");
        GL.GetUniformLocation = Load<GL.GlGetUniformLocation>("glGetUniformLocation");
        GL.UniformMatrix4fv = Load<GL.GlUniformMatrix4fv>("glUniformMatrix4fv");
        GL.Uniform1i = Load<GL.GlUniform1i>("glUniform1i");
        GL.Uniform1ui = Load<GL.GlUniform1ui>("glUniform1ui");
        GL.Uniform1f = Load<GL.GlUniform1f>("glUniform1f");
        GL.Uniform2f = Load<GL.GlUniform2f>("glUniform2f");
        GL.Uniform3f = Load<GL.GlUniform3f>("glUniform3f");
        GL.ActiveTexture = Load<GL.GlActiveTexture>("glActiveTexture");
        GL.GenTextures = Load<GL.GlGenTextures>("glGenTextures");
        GL.BindTexture = Load<GL.GlBindTexture>("glBindTexture");
        GL.TexParameteri = Load<GL.GlTexParameteri>("glTexParameteri");
        GL.TexImage2D = Load<GL.GlTexImage2D>("glTexImage2D");
        GL.DeleteTextures = Load<GL.GlDeleteTextures>("glDeleteTextures");
        GL.GenFramebuffers = Load<GL.GlGenFramebuffers>("glGenFramebuffers");
        GL.BindFramebuffer = Load<GL.GlBindFramebuffer>("glBindFramebuffer");
        GL.FramebufferTexture2D = Load<GL.GlFramebufferTexture2D>("glFramebufferTexture2D");
        GL.CheckFramebufferStatus = Load<GL.GlCheckFramebufferStatus>("glCheckFramebufferStatus");
        GL.DeleteFramebuffers = Load<GL.GlDeleteFramebuffers>("glDeleteFramebuffers");
        GL.DrawBuffers = Load<GL.GlDrawBuffers>("glDrawBuffers");
        GL.ReadBuffer = Load<GL.GlReadBuffer>("glReadBuffer");
        GL.ReadPixels = Load<GL.GlReadPixels>("glReadPixels");
        GL.BlitFramebuffer = Load<GL.GlBlitFramebuffer>("glBlitFramebuffer");
        GL.DrawArrays = Load<GL.GlDrawArrays>("glDrawArrays");
        GL.DrawElements = Load<GL.GlDrawElements>("glDrawElements");
        GL.GetString = Load<GL.GlGetString>("glGetString");
    }

    public static Wgl.WglCreateContextAttribsARB LoadCreateContextAttribs()
    {
        return Load<Wgl.WglCreateContextAttribsARB>("wglCreateContextAttribsARB");
    }

    public static T Load<T>(string name) where T : Delegate
    {
        nint proc = Wgl.wglGetProcAddress(name);
        if (!IsInvalidProc(proc))
        {
            return Marshal.GetDelegateForFunctionPointer<T>(proc);
        }

        EnsureModule();
        if (NativeLibrary.TryGetExport(s_opengl32Module, name, out nint export))
        {
            return Marshal.GetDelegateForFunctionPointer<T>(export);
        }

        throw new MissingMethodException($"Unable to load OpenGL entry point '{name}'.");
    }

    private static void EnsureModule()
    {
        if (s_opengl32Module == 0)
        {
            s_opengl32Module = NativeLibrary.Load("opengl32.dll");
        }
    }

    private static bool IsInvalidProc(nint proc)
    {
        return proc == 0 || proc == (nint)1 || proc == (nint)2 || proc == (nint)3 || proc == (nint)(-1);
    }
}
