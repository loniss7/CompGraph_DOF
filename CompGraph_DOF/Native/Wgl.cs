using System.Runtime.InteropServices;

namespace CompGraph_DOF.Native;

internal static class Wgl
{
    public const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
    public const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
    public const int WGL_CONTEXT_FLAGS_ARB = 0x2094;
    public const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
    public const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;
    public const int WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB = 0x00000002;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate nint WglCreateContextAttribsARB(nint hDC, nint hShareContext, IntPtr attribList);

    [DllImport("opengl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint wglCreateContext(nint hdc);

    [DllImport("opengl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool wglDeleteContext(nint hglrc);

    [DllImport("opengl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool wglMakeCurrent(nint hdc, nint hglrc);

    [DllImport("opengl32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern nint wglGetProcAddress(string lpszProc);
}
