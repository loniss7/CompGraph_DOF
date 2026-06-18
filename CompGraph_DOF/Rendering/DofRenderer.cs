using System.Numerics;
using System.IO;
using System.Runtime.InteropServices;
using CompGraph_DOF.Graphics;
using CompGraph_DOF.Scene;

namespace CompGraph_DOF.Rendering;

internal sealed class DofRenderer : IDisposable
{
    private readonly ShaderProgram _sceneShader;
    private readonly ShaderProgram _backgroundShader;
    private readonly ShaderProgram _blurShader;
    private readonly ShaderProgram _compositeShader;
    private readonly FullscreenQuad _fullscreenQuad;
    private readonly Texture2D _backgroundTexture;
    private Framebuffer? _sceneFramebuffer;
    private Framebuffer? _blurPingFramebuffer;
    private Framebuffer? _blurPongFramebuffer;
    private float _defaultFocusDistance;
    private int _width;
    private int _height;

    public float FocusDistance { get; set; }
    public bool BlurEnabled { get; set; }
    public float FocusRange { get; set; } = 0.35f;
    public float FocusTransition { get; set; } = 0.90f;
    public float NearBlurScale { get; set; } = 1.35f;
    public float FarBlurScale { get; set; } = 1.35f;
    public float MaxBlurRadius { get; set; } = 32.0f;
    public float Sigma { get; set; } = 7.0f;
    public float DepthSigma { get; set; } = 4.5f;
    public DofDebugView DebugView { get; set; } = DofDebugView.Composite;

    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.45f, -1.0f, -0.35f));
    private static readonly float[] ClearColor = { 0.05f, 0.06f, 0.09f, 1f };
    private static readonly float[] ClearDepth = { 1f };
    private static readonly uint[] ClearObjectId = { 0u };

    public DofRenderer(string shaderRootDirectory, int width, int height, float initialFocusDistance)
    {
        _sceneShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "scene.vert"),
            Path.Combine(shaderRootDirectory, "scene.frag"));

        _backgroundShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "dof.vert"),
            Path.Combine(shaderRootDirectory, "background.frag"));

        _blurShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "dof.vert"),
            Path.Combine(shaderRootDirectory, "blur.frag"));

        _compositeShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "dof.vert"),
            Path.Combine(shaderRootDirectory, "composite.frag"));

        _fullscreenQuad = new FullscreenQuad();
        _backgroundTexture = Texture2D.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "background.jpg"));
        _defaultFocusDistance = initialFocusDistance;
        FocusDistance = initialFocusDistance;
        Resize(width, height);

        _backgroundShader.Use();
        _backgroundShader.SetInt("uBackgroundTexture", 0);

        _blurShader.Use();
        _blurShader.SetInt("uInputColor", 0);
        _blurShader.SetInt("uSceneDepth", 1);
        _blurShader.SetInt("uBlurEnabled", 0);

        _compositeShader.Use();
        _compositeShader.SetInt("uSharpTexture", 0);
        _compositeShader.SetInt("uBlurredTexture", 1);
        _compositeShader.SetInt("uSceneDepth", 2);
        _compositeShader.SetInt("uSceneObjectIdTexture", 3);
        _compositeShader.SetInt("uBlurEnabled", 0);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        if (width <= 0 || height <= 0)
        {
            _sceneFramebuffer?.Dispose();
            _blurPingFramebuffer?.Dispose();
            _blurPongFramebuffer?.Dispose();
            _sceneFramebuffer = null;
            _blurPingFramebuffer = null;
            _blurPongFramebuffer = null;
            return;
        }

        _sceneFramebuffer?.Dispose();
        _blurPingFramebuffer?.Dispose();
        _blurPongFramebuffer?.Dispose();
        _sceneFramebuffer = new Framebuffer(width, height, FramebufferKind.Scene);
        _blurPingFramebuffer = new Framebuffer(width, height, FramebufferKind.Blur);
        _blurPongFramebuffer = new Framebuffer(width, height, FramebufferKind.Blur);
        AssertNoGlError("Framebuffer creation");
    }

    public void ResetParameters(float defaultFocusDistance)
    {
        _defaultFocusDistance = defaultFocusDistance;
        FocusDistance = defaultFocusDistance;
        BlurEnabled = false;
        FocusRange = 0.35f;
        FocusTransition = 0.90f;
        NearBlurScale = 1.35f;
        FarBlurScale = 1.35f;
        MaxBlurRadius = 32.0f;
        Sigma = 7.0f;
        DepthSigma = 4.5f;
        DebugView = DofDebugView.Composite;
    }

    public void Render(
        Camera camera,
        DemoScene scene,
        int clientWidth,
        int clientHeight,
        PickRequest? pendingPick,
        out PickResult? pickResult)
    {
        pickResult = null;

        if (_sceneFramebuffer is null || _blurPingFramebuffer is null || _blurPongFramebuffer is null || _width <= 0 || _height <= 0)
        {
            return;
        }

        RenderScene(camera, scene);

        if (pendingPick.HasValue)
        {
            TryPick(pendingPick.Value, clientWidth, clientHeight, camera, out PickResult result);
            pickResult = result;

            if (result.ObjectId != 0u)
            {
                FocusDistance = result.LinearDepth;
                BlurEnabled = true;
            }
            else
            {
                FocusDistance = _defaultFocusDistance;
                BlurEnabled = false;
            }
        }

        switch (DebugView)
        {
            case DofDebugView.SceneColor:
                RenderComposite(camera, DofDebugView.SceneColor);
                return;

            case DofDebugView.Depth:
            case DofDebugView.CircleOfConfusion:
            case DofDebugView.ObjectId:
                RenderComposite(camera, DebugView);
                return;

            case DofDebugView.HorizontalBlur:
                RenderHorizontalBlur(camera);
                PresentFramebuffer(_blurPingFramebuffer);
                return;

            case DofDebugView.VerticalBlur:
                RenderHorizontalBlur(camera);
                RenderVerticalBlur(camera);
                PresentFramebuffer(_blurPongFramebuffer);
                return;

            case DofDebugView.Composite:
            default:
                RenderHorizontalBlur(camera);
                RenderVerticalBlur(camera);
                RenderComposite(camera, DofDebugView.Composite);
                return;
        }
    }

    public void Dispose()
    {
        _sceneFramebuffer?.Dispose();
        _blurPingFramebuffer?.Dispose();
        _blurPongFramebuffer?.Dispose();
        _fullscreenQuad.Dispose();
        _sceneShader.Dispose();
        _backgroundShader.Dispose();
        _blurShader.Dispose();
        _compositeShader.Dispose();
        _backgroundTexture.Dispose();
    }

    private void RenderScene(Camera camera, DemoScene scene)
    {
        _sceneFramebuffer!.Bind();
        GL.Viewport(0, 0, _width, _height);
        GL.Enable(GL.DEPTH_TEST);
        GL.DepthFunc(GL.LESS);
        GL.Enable(GL.CULL_FACE);
        GL.CullFace(GL.BACK);
        GL.FrontFace(GL.CCW);

        GL.ClearBufferfv(GL.COLOR, 0, ClearColor);
        GL.ClearBufferuiv(GL.COLOR, 1, ClearObjectId);
        GL.ClearBufferfv(GL.DEPTH, 0, ClearDepth);

        RenderBackground();

        _sceneShader.Use();
        _sceneShader.SetMatrix4("uView", camera.GetViewMatrix());
        _sceneShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        _sceneShader.SetVector3("uCameraPosition", camera.Position);
        _sceneShader.SetVector3("uLightDirection", LightDirection);

        foreach (SceneObject sceneObject in scene.Objects)
        {
            sceneObject.Draw(_sceneShader);
        }

        AssertNoGlError("Scene render");
    }

    private void RenderBackground()
    {
        GL.Disable(GL.DEPTH_TEST);
        GL.Disable(GL.CULL_FACE);

        _backgroundShader.Use();
        _backgroundTexture.Bind();
        _fullscreenQuad.Draw();

        GL.Enable(GL.DEPTH_TEST);
        GL.Enable(GL.CULL_FACE);
    }

    private void RenderHorizontalBlur(Camera camera)
    {
        RenderBlurPass(
            _blurPingFramebuffer!,
            _sceneFramebuffer!.ColorTexture,
            new Vector2(1f, 0f),
            camera,
            "Horizontal blur");
    }

    private void RenderVerticalBlur(Camera camera)
    {
        RenderBlurPass(
            _blurPongFramebuffer!,
            _blurPingFramebuffer!.ColorTexture,
            new Vector2(0f, 1f),
            camera,
            "Vertical blur");
    }

    private void RenderBlurPass(
        Framebuffer target,
        uint inputColorTexture,
        Vector2 direction,
        Camera camera,
        string stageName)
    {
        target.Bind();
        GL.Viewport(0, 0, _width, _height);
        GL.Disable(GL.DEPTH_TEST);
        GL.Disable(GL.CULL_FACE);

        GL.ClearBufferfv(GL.COLOR, 0, ClearColor);

        _blurShader.Use();
        _blurShader.SetInt("uDebugView", (int)DebugView);
        _blurShader.SetInt("uBlurEnabled", BlurEnabled ? 1 : 0);
        _blurShader.SetFloat("uFocusDistance", FocusDistance);
        _blurShader.SetFloat("uFocusRange", FocusRange);
        _blurShader.SetFloat("uFocusTransition", FocusTransition);
        _blurShader.SetFloat("uNearBlurScale", NearBlurScale);
        _blurShader.SetFloat("uFarBlurScale", FarBlurScale);
        _blurShader.SetFloat("uMaxBlurRadius", MaxBlurRadius);
        _blurShader.SetFloat("uSigma", Sigma);
        _blurShader.SetFloat("uDepthSigma", DepthSigma);
        _blurShader.SetFloat("uNearPlane", camera.NearPlane);
        _blurShader.SetFloat("uFarPlane", camera.FarPlane);
        _blurShader.SetVector2("uTexelSize", new Vector2(1f / _width, 1f / _height));
        _blurShader.SetVector2("uDirection", direction);

        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, inputColorTexture);
        GL.ActiveTexture(GL.TEXTURE0 + 1);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer!.DepthTexture);

        _fullscreenQuad.Draw();
        AssertNoGlError(stageName);
    }

    private void RenderComposite(Camera camera, DofDebugView debugView)
    {
        GL.BindFramebuffer(GL.FRAMEBUFFER, 0);
        GL.Viewport(0, 0, _width, _height);
        GL.Disable(GL.DEPTH_TEST);
        GL.Disable(GL.CULL_FACE);

        GL.ClearBufferfv(GL.COLOR, 0, ClearColor);

        _compositeShader.Use();
        _compositeShader.SetInt("uBlurEnabled", BlurEnabled ? 1 : 0);
        _compositeShader.SetFloat("uFocusDistance", FocusDistance);
        _compositeShader.SetFloat("uFocusRange", FocusRange);
        _compositeShader.SetFloat("uFocusTransition", FocusTransition);
        _compositeShader.SetFloat("uNearBlurScale", NearBlurScale);
        _compositeShader.SetFloat("uFarBlurScale", FarBlurScale);
        _compositeShader.SetFloat("uNearPlane", camera.NearPlane);
        _compositeShader.SetFloat("uFarPlane", camera.FarPlane);
        _compositeShader.SetInt("uDebugView", (int)debugView);

        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer!.ColorTexture);
        GL.ActiveTexture(GL.TEXTURE0 + 1);
        GL.BindTexture(GL.TEXTURE_2D, _blurPongFramebuffer?.ColorTexture ?? _sceneFramebuffer.ColorTexture);
        GL.ActiveTexture(GL.TEXTURE0 + 2);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer.DepthTexture);
        GL.ActiveTexture(GL.TEXTURE0 + 3);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer.ObjectIdTexture);

        _fullscreenQuad.Draw();
        AssertNoGlError("Composite");
    }

    private void PresentFramebuffer(Framebuffer source)
    {
        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, source.Handle);
        GL.ReadBuffer(GL.COLOR_ATTACHMENT0);
        GL.BindFramebuffer(GL.DRAW_FRAMEBUFFER, 0);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GL.COLOR_BUFFER_BIT, GL.LINEAR);
        GL.BindFramebuffer(GL.FRAMEBUFFER, 0);
        AssertNoGlError("Framebuffer blit");
    }

    private bool TryPick(
        PickRequest request,
        int clientWidth,
        int clientHeight,
        Camera camera,
        out PickResult pickResult)
    {
        pickResult = default;

        if (_sceneFramebuffer is null || _width <= 0 || _height <= 0 || clientWidth <= 0 || clientHeight <= 0)
        {
            return false;
        }

        int clampedClientX = Math.Clamp(request.ClientX, 0, clientWidth - 1);
        int clampedClientY = Math.Clamp(request.ClientY, 0, clientHeight - 1);
        int framebufferX = Math.Clamp((int)MathF.Floor(clampedClientX * _width / (float)clientWidth), 0, _width - 1);
        int framebufferYTop = Math.Clamp((int)MathF.Floor(clampedClientY * _height / (float)clientHeight), 0, _height - 1);
        int framebufferY = _height - 1 - framebufferYTop;

        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, _sceneFramebuffer.Handle);

        GL.ReadBuffer(GL.COLOR_ATTACHMENT1);
        uint[] objectId = new uint[1];
        GCHandle objectIdHandle = GCHandle.Alloc(objectId, GCHandleType.Pinned);
        try
        {
            GL.ReadPixels(framebufferX, framebufferY, 1, 1, GL.RED_INTEGER, GL.UNSIGNED_INT, objectIdHandle.AddrOfPinnedObject());
        }
        finally
        {
            objectIdHandle.Free();
        }

        GL.ReadBuffer(GL.NONE);
        float[] depth = new float[1];
        GCHandle depthHandle = GCHandle.Alloc(depth, GCHandleType.Pinned);
        try
        {
            GL.ReadPixels(framebufferX, framebufferY, 1, 1, GL.DEPTH_COMPONENT, GL.FLOAT, depthHandle.AddrOfPinnedObject());
        }
        finally
        {
            depthHandle.Free();
        }

        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, 0);

        float linearDepth = camera.LinearizeDepth(depth[0]);
        pickResult = new PickResult(objectId[0], depth[0], linearDepth, framebufferX, framebufferY);

        bool hit = objectId[0] != 0u && depth[0] < 1f;
        AssertNoGlError("Picking");
        return hit;
    }

    private static void AssertNoGlError(string stage)
    {
        uint error = GL.GetError();
        if (error != GL.NO_ERROR)
        {
            throw new InvalidOperationException($"{stage} failed with OpenGL error 0x{error:X4}.");
        }
    }
}
