using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using CompGraph_DOF.Graphics;
using CompGraph_DOF.Native;
using CompGraph_DOF.Rendering;
using CompGraph_DOF.Scene;

namespace CompGraph_DOF;

internal sealed class DofApplication : IDisposable
{
    private const string WindowClassName = "CompGraph_DOF_WindowClass";
    private const int InitialClientWidth = 1280;
    private const int InitialClientHeight = 720;

    private static readonly Win32.WndProc WindowProcDelegate = WindowProc;
    private static DofApplication? s_instance;

    private readonly nint _instanceHandle;
    private readonly string _shaderRootDirectory;
    private readonly bool[] _keys = new bool[256];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DemoScene _scene;
    private readonly Camera _camera;
    private readonly DofRenderer _renderer;

    private nint _windowHandle;
    private nint _deviceContext;
    private nint _temporaryContext;
    private nint _glContext;
    private bool _rightMouseDown;
    private int _lastMouseX;
    private int _lastMouseY;
    private int _clientWidth;
    private int _clientHeight;
    private bool _exitRequested;
    private string _pickedLabel = "Background";
    private string _pickDetails = "No pick";
    private PickRequest? _pendingPick;

    public DofApplication()
    {
        s_instance = this;
        _instanceHandle = Win32.GetModuleHandle(null);
        _shaderRootDirectory = Path.Combine(AppContext.BaseDirectory, "Shaders");

        RegisterWindowClass();
        CreateWindow();
        InitializeOpenGl();

        _scene = new DemoScene();
        _camera = new Camera(GetAspectRatio());
        _renderer = new DofRenderer(_shaderRootDirectory, _clientWidth, _clientHeight, _scene.DefaultFocusDistance);
        Win32.ShowWindow(_windowHandle, Win32.SW_SHOW);
        Win32.UpdateWindow(_windowHandle);
        UpdateWindowTitle();
    }

    public void Run()
    {
        var message = default(Win32.MSG);
        double lastTime = _clock.Elapsed.TotalSeconds;

        while (!_exitRequested)
        {
            while (Win32.PeekMessage(out message, 0, 0, 0, Win32.PM_REMOVE))
            {
                if (message.message == 0x0012)
                {
                    _exitRequested = true;
                    break;
                }

                Win32.TranslateMessage(ref message);
                Win32.DispatchMessage(ref message);
            }

            if (_exitRequested)
            {
                break;
            }

            double now = _clock.Elapsed.TotalSeconds;
            float deltaTime = (float)(now - lastTime);
            lastTime = now;

            if (_clientWidth > 0 && _clientHeight > 0)
            {
                UpdateFrame(deltaTime);
                _renderer.Render(_camera, _scene, _clientWidth, _clientHeight, _pendingPick, out PickResult? pickResult);
                _pendingPick = null;

                if (pickResult.HasValue)
                {
                    UpdatePickState(pickResult.Value);

                    if (pickResult.Value.ObjectId != 0u && _renderer.DebugView == DofDebugView.SceneColor)
                    {
                        _renderer.DebugView = DofDebugView.Composite;
                    }
                }
                Win32.SwapBuffers(_deviceContext);
                UpdateWindowTitle();
            }
            else
            {
                Thread.Sleep(16);
            }

            Thread.Sleep(1);
        }
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _scene.Dispose();

        if (_glContext != 0)
        {
            Wgl.wglMakeCurrent(0, 0);
            Wgl.wglDeleteContext(_glContext);
            _glContext = 0;
        }

        if (_temporaryContext != 0)
        {
            Wgl.wglDeleteContext(_temporaryContext);
            _temporaryContext = 0;
        }

        if (_deviceContext != 0 && _windowHandle != 0)
        {
            Win32.ReleaseDC(_windowHandle, _deviceContext);
            _deviceContext = 0;
        }

        Win32.UnregisterClass(WindowClassName, _instanceHandle);
        s_instance = null;
    }

    private void RegisterWindowClass()
    {
        var wndClass = new Win32.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
            style = (uint)(Win32.CS_HREDRAW | Win32.CS_VREDRAW | Win32.CS_OWNDC),
            lpfnWndProc = WindowProcDelegate,
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = _instanceHandle,
            hIcon = 0,
            hCursor = Win32.LoadCursor(0, (nint)Win32.IDC_ARROW),
            hbrBackground = 0,
            lpszMenuName = null,
            lpszClassName = WindowClassName,
            hIconSm = 0
        };

        if (Win32.RegisterClassEx(ref wndClass) == 0)
        {
            throw new InvalidOperationException($"Unable to register window class. Win32 error {Marshal.GetLastWin32Error()}.");
        }
    }

    private void CreateWindow()
    {
        var rect = new Win32.RECT
        {
            Left = 0,
            Top = 0,
            Right = InitialClientWidth,
            Bottom = InitialClientHeight
        };

        if (!Win32.AdjustWindowRectEx(ref rect, Win32.WS_OVERLAPPEDWINDOW, false, Win32.WS_EX_APPWINDOW))
        {
            throw new InvalidOperationException($"Unable to adjust window rectangle. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        _windowHandle = Win32.CreateWindowEx(
            Win32.WS_EX_APPWINDOW,
            WindowClassName,
            "CompGraph DOF Demo",
            Win32.WS_OVERLAPPEDWINDOW,
            Win32.CW_USEDEFAULT,
            Win32.CW_USEDEFAULT,
            rect.Width,
            rect.Height,
            0,
            0,
            _instanceHandle,
            0);

        if (_windowHandle == 0)
        {
            throw new InvalidOperationException($"Unable to create window. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        _deviceContext = Win32.GetDC(_windowHandle);
        if (_deviceContext == 0)
        {
            throw new InvalidOperationException($"Unable to get device context. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        _clientWidth = InitialClientWidth;
        _clientHeight = InitialClientHeight;

        if (!SetupPixelFormat(_deviceContext))
        {
            throw new InvalidOperationException($"Unable to set pixel format. Win32 error {Marshal.GetLastWin32Error()}.");
        }
    }

    private void InitializeOpenGl()
    {
        _temporaryContext = Wgl.wglCreateContext(_deviceContext);
        if (_temporaryContext == 0)
        {
            throw new InvalidOperationException($"Unable to create temporary WGL context. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        if (!Wgl.wglMakeCurrent(_deviceContext, _temporaryContext))
        {
            throw new InvalidOperationException($"Unable to make temporary WGL context current. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        Wgl.WglCreateContextAttribsARB createContextAttribs = GLLoader.LoadCreateContextAttribs();

        int[] attribs =
        {
            Wgl.WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
            Wgl.WGL_CONTEXT_MINOR_VERSION_ARB, 3,
            Wgl.WGL_CONTEXT_FLAGS_ARB, Wgl.WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB,
            Wgl.WGL_CONTEXT_PROFILE_MASK_ARB, Wgl.WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
            0
        };

        GCHandle attribHandle = GCHandle.Alloc(attribs, GCHandleType.Pinned);
        try
        {
            _glContext = createContextAttribs(_deviceContext, 0, attribHandle.AddrOfPinnedObject());
        }
        finally
        {
            attribHandle.Free();
        }

        if (_glContext == 0)
        {
            throw new InvalidOperationException($"Unable to create OpenGL 3.3 core context. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        Wgl.wglMakeCurrent(0, 0);

        if (!Wgl.wglDeleteContext(_temporaryContext))
        {
            throw new InvalidOperationException($"Unable to delete temporary WGL context. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        _temporaryContext = 0;

        if (!Wgl.wglMakeCurrent(_deviceContext, _glContext))
        {
            throw new InvalidOperationException($"Unable to make OpenGL context current. Win32 error {Marshal.GetLastWin32Error()}.");
        }

        GLLoader.LoadAll();
        GL.Enable(GL.DEPTH_TEST);
        GL.DepthFunc(GL.LESS);
        GL.Enable(GL.CULL_FACE);
        GL.CullFace(GL.BACK);
        GL.FrontFace(GL.CCW);
    }

    private void UpdateFrame(float deltaTime)
    {
        float moveAmount = _camera.MovementSpeed * deltaTime;
        if (_keys[Win32.VK_W])
        {
            _camera.MoveForward(moveAmount);
        }

        if (_keys[Win32.VK_S])
        {
            _camera.MoveForward(-moveAmount);
        }

        if (_keys[Win32.VK_A])
        {
            _camera.MoveRight(-moveAmount);
        }

        if (_keys[Win32.VK_D])
        {
            _camera.MoveRight(moveAmount);
        }

        if (_keys[Win32.VK_Q])
        {
            _camera.MoveUp(-moveAmount);
        }

        if (_keys[Win32.VK_E])
        {
            _camera.MoveUp(moveAmount);
        }

        float rotationAmount = 75f * deltaTime;
        if (_keys[Win32.VK_LEFT])
        {
            _camera.Rotate(-rotationAmount, 0f);
        }

        if (_keys[Win32.VK_RIGHT])
        {
            _camera.Rotate(rotationAmount, 0f);
        }

        if (_keys[Win32.VK_UP])
        {
            _camera.Rotate(0f, rotationAmount);
        }

        if (_keys[Win32.VK_DOWN])
        {
            _camera.Rotate(0f, -rotationAmount);
        }
    }

    private void ResetCameraAndDof()
    {
        _camera.Reset();
        _camera.SetAspectRatio(GetAspectRatio());
        _renderer.ResetParameters(_scene.DefaultFocusDistance);
        _pickedLabel = "Background";
        _pickDetails = "No pick";
        _pendingPick = null;
    }

    private void UpdateWindowTitle()
    {
        string title = string.Format(
            CultureInfo.InvariantCulture,
            "CompGraph DOF | View {0} | Focus {1:F2} | Range {2:F2} | Transition {3:F2} | Radius {4:F1} | Sigma {5:F2} | Depth {6:F2} | {7} | {8}",
            DescribeDebugView(_renderer.DebugView),
            _renderer.FocusDistance,
            _renderer.FocusRange,
            _renderer.FocusTransition,
            _renderer.MaxBlurRadius,
            _renderer.Sigma,
            _renderer.DepthSigma,
            _pickedLabel,
            _pickDetails);

        Win32.SetWindowText(_windowHandle, title);
    }

    private float GetAspectRatio()
    {
        if (_clientWidth <= 0 || _clientHeight <= 0)
        {
            return InitialClientWidth / (float)InitialClientHeight;
        }

        return _clientWidth / (float)_clientHeight;
    }

    private bool SetupPixelFormat(nint hdc)
    {
        var pfd = new Win32.PIXELFORMATDESCRIPTOR
        {
            nSize = (ushort)Marshal.SizeOf<Win32.PIXELFORMATDESCRIPTOR>(),
            nVersion = 1,
            dwFlags = Win32.PFD_DRAW_TO_WINDOW | Win32.PFD_SUPPORT_OPENGL | Win32.PFD_DOUBLEBUFFER,
            iPixelType = (byte)Win32.PFD_TYPE_RGBA,
            cColorBits = 32,
            cRedBits = 0,
            cRedShift = 0,
            cGreenBits = 0,
            cGreenShift = 0,
            cBlueBits = 0,
            cBlueShift = 0,
            cAlphaBits = 8,
            cAlphaShift = 0,
            cAccumBits = 0,
            cAccumRedBits = 0,
            cAccumGreenBits = 0,
            cAccumBlueBits = 0,
            cAccumAlphaBits = 0,
            cDepthBits = 24,
            cStencilBits = 8,
            cAuxBuffers = 0,
            iLayerType = (byte)Win32.PFD_MAIN_PLANE,
            bReserved = 0,
            dwLayerMask = 0,
            dwVisibleMask = 0,
            dwDamageMask = 0
        };

        int pixelFormat = Win32.ChoosePixelFormat(hdc, ref pfd);
        if (pixelFormat == 0)
        {
            return false;
        }

        return Win32.SetPixelFormat(hdc, pixelFormat, ref pfd);
    }

    private static nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (s_instance is { } instance)
        {
            return instance.HandleMessage(hWnd, msg, wParam, lParam);
        }

        return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint HandleMessage(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case Win32.WM_SIZE:
                {
                    int width = unchecked((short)(lParam.ToInt64() & 0xFFFF));
                    int height = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
                    _clientWidth = Math.Max(0, width);
                    _clientHeight = Math.Max(0, height);

                    if (_clientWidth > 0 && _clientHeight > 0)
                    {
                        _camera.SetAspectRatio(GetAspectRatio());
                    }

                    _renderer.Resize(_clientWidth, _clientHeight);
                    return 0;
                }

            case Win32.WM_CLOSE:
                Win32.DestroyWindow(hWnd);
                return 0;

            case Win32.WM_DESTROY:
                _exitRequested = true;
                Win32.PostQuitMessage(0);
                return 0;

            case Win32.WM_ERASEBKGND:
                return 1;

            case Win32.WM_KEYDOWN:
            case Win32.WM_SYSKEYDOWN:
                return HandleKeyDown((int)wParam);

            case Win32.WM_KEYUP:
            case Win32.WM_SYSKEYUP:
                return HandleKeyUp((int)wParam);

            case Win32.WM_LBUTTONDOWN:
                _pendingPick = new PickRequest(GetX(lParam), GetY(lParam));
                return 0;

            case Win32.WM_RBUTTONDOWN:
                _rightMouseDown = true;
                _lastMouseX = GetX(lParam);
                _lastMouseY = GetY(lParam);
                Win32.SetCapture(hWnd);
                return 0;

            case Win32.WM_RBUTTONUP:
                _rightMouseDown = false;
                Win32.ReleaseCapture();
                return 0;

            case Win32.WM_MOUSEMOVE:
                if (_rightMouseDown)
                {
                    int currentX = GetX(lParam);
                    int currentY = GetY(lParam);
                    int deltaX = currentX - _lastMouseX;
                    int deltaY = currentY - _lastMouseY;
                    _lastMouseX = currentX;
                    _lastMouseY = currentY;
                    _camera.Rotate(deltaX * _camera.MouseSensitivity, -deltaY * _camera.MouseSensitivity);
                }

                return 0;
        }

        return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint HandleKeyDown(int virtualKey)
    {
        if (virtualKey >= 0 && virtualKey < _keys.Length)
        {
            _keys[virtualKey] = true;
        }

        switch (virtualKey)
        {
            case Win32.VK_ESCAPE:
                Win32.DestroyWindow(_windowHandle);
                break;

            case Win32.VK_R:
                ResetCameraAndDof();
                break;

            case Win32.VK_1:
                _renderer.DebugView = DofDebugView.SceneColor;
                break;

            case Win32.VK_2:
                _renderer.DebugView = DofDebugView.Depth;
                break;

            case Win32.VK_3:
                _renderer.DebugView = DofDebugView.CircleOfConfusion;
                break;

            case Win32.VK_4:
                _renderer.DebugView = DofDebugView.HorizontalBlur;
                break;

            case Win32.VK_5:
                _renderer.DebugView = DofDebugView.VerticalBlur;
                break;

            case Win32.VK_6:
                _renderer.DebugView = DofDebugView.Composite;
                break;

            case Win32.VK_7:
                _renderer.DebugView = DofDebugView.ObjectId;
                break;

            case Win32.VK_OEM_MINUS:
                _renderer.MaxBlurRadius = MathF.Max(0.5f, _renderer.MaxBlurRadius - 1.0f);
                break;

            case Win32.VK_OEM_PLUS:
                _renderer.MaxBlurRadius = MathF.Min(32.0f, _renderer.MaxBlurRadius + 1.0f);
                break;

            case Win32.VK_OEM_4:
                _renderer.FocusRange = MathF.Max(0.05f, _renderer.FocusRange - 0.05f);
                break;

            case Win32.VK_OEM_6:
                _renderer.FocusRange = MathF.Min(10.0f, _renderer.FocusRange + 0.05f);
                break;

            case Win32.VK_OEM_1:
                _renderer.FocusTransition = MathF.Max(0.05f, _renderer.FocusTransition - 0.05f);
                break;

            case Win32.VK_OEM_7:
                _renderer.FocusTransition = MathF.Min(10.0f, _renderer.FocusTransition + 0.05f);
                break;
        }

        return 0;
    }

    private nint HandleKeyUp(int virtualKey)
    {
        if (virtualKey >= 0 && virtualKey < _keys.Length)
        {
            _keys[virtualKey] = false;
        }

        return 0;
    }

    private static int GetX(nint lParam) => unchecked((short)(lParam.ToInt64() & 0xFFFF));

    private static int GetY(nint lParam) => unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));

    private void UpdatePickState(PickResult pickResult)
    {
        _pickDetails = string.Format(
            CultureInfo.InvariantCulture,
            "Raw {0:F4} | Linear {1:F2} | FB {2},{3}",
            pickResult.RawDepth,
            pickResult.LinearDepth,
            pickResult.FramebufferX,
            pickResult.FramebufferY);

        if (pickResult.ObjectId == 0u)
        {
            _pickedLabel = "Background";
            return;
        }

        if (_scene.TryGetObject(pickResult.ObjectId, out SceneObject? sceneObject) && sceneObject is not null)
        {
            _pickedLabel = $"{sceneObject.Name} #{pickResult.ObjectId}";
            return;
        }

        _pickedLabel = $"Object #{pickResult.ObjectId}";
    }

    private static string DescribeDebugView(DofDebugView debugView)
    {
        return debugView switch
        {
            DofDebugView.SceneColor => "Scene",
            DofDebugView.Depth => "Depth",
            DofDebugView.CircleOfConfusion => "CoC",
            DofDebugView.HorizontalBlur => "HBlur",
            DofDebugView.VerticalBlur => "VBlur",
            DofDebugView.Composite => "Composite",
            DofDebugView.ObjectId => "ObjectId",
            _ => "Unknown"
        };
    }
}
