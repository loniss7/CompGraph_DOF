using System.Runtime.InteropServices;
using System.Numerics;
using CompGraph_DOF.Graphics;
using CompGraph_DOF.Scene;

namespace CompGraph_DOF.Rendering;

internal sealed class DofRenderer : IDisposable
{
    private readonly ShaderProgram _sceneShader;
    private readonly ShaderProgram _dofShader;
    private readonly FullscreenQuad _fullscreenQuad;
    private Framebuffer? _sceneFramebuffer;
    private Framebuffer? _dofFramebuffer;
    private int _width;
    private int _height;

    public float FocusDistance { get; set; }
    public float FocusRange { get; set; } = 1.8f;
    public float FocusTransition { get; set; } = 3.0f;
    public float NearBlurScale { get; set; } = 0.50f;
    public float FarBlurScale { get; set; } = 0.80f;
    public float MaxBlurRadius { get; set; } = 2.7f;
    public float Sigma { get; set; } = 1.7f;
    public float DepthSigma { get; set; } = 3.0f;

    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.45f, -1.0f, -0.35f));
    private static readonly float[] ClearColor = { 0.05f, 0.06f, 0.09f, 1f };
    private static readonly float[] ClearDepth = { 1f };
    private static readonly uint[] ClearObjectId = { 0u };

    public DofRenderer(string shaderRootDirectory, int width, int height, float initialFocusDistance)
    {
        _sceneShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "scene.vert"),
            Path.Combine(shaderRootDirectory, "scene.frag"));

        _dofShader = new ShaderProgram(
            Path.Combine(shaderRootDirectory, "dof.vert"),
            Path.Combine(shaderRootDirectory, "dof.frag"));

        _fullscreenQuad = new FullscreenQuad();
        FocusDistance = initialFocusDistance;
        Resize(width, height);

        _sceneShader.Use();
        _sceneShader.SetInt("uSceneColorTexture", 0);
        _sceneShader.SetInt("uSceneDepthTexture", 1);

        _dofShader.Use();
        _dofShader.SetInt("uSceneColorTexture", 0);
        _dofShader.SetInt("uSceneDepthTexture", 1);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        if (width <= 0 || height <= 0)
        {
            _sceneFramebuffer?.Dispose();
            _dofFramebuffer?.Dispose();
            _sceneFramebuffer = null;
            _dofFramebuffer = null;
            return;
        }

        _sceneFramebuffer?.Dispose();
        _dofFramebuffer?.Dispose();
        _sceneFramebuffer = new Framebuffer(width, height, FramebufferKind.Scene);
        _dofFramebuffer = new Framebuffer(width, height, FramebufferKind.Dof);
    }

    public void ResetParameters(float defaultFocusDistance)
    {
        FocusDistance = defaultFocusDistance;
        FocusRange = 1.8f;
        FocusTransition = 3.0f;
        NearBlurScale = 0.50f;
        FarBlurScale = 0.80f;
        MaxBlurRadius = 2.7f;
        Sigma = 1.7f;
        DepthSigma = 3.0f;
    }

    public void Render(Camera camera, DemoScene scene)
    {
        if (_sceneFramebuffer is null || _dofFramebuffer is null || _width <= 0 || _height <= 0)
        {
            return;
        }

        RenderScene(camera, scene);
        RenderDof(camera);
    }

    public bool TryPick(int x, int y, Camera camera, out uint objectId, out float linearDepth)
    {
        objectId = 0;
        linearDepth = 0f;

        if (_sceneFramebuffer is null || _width <= 0 || _height <= 0)
        {
            return false;
        }

        int clampedX = Math.Clamp(x, 0, _width - 1);
        int clampedY = Math.Clamp(y, 0, _height - 1);
        int flippedY = _height - 1 - clampedY;

        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, _sceneFramebuffer.Handle);

        GL.ReadBuffer(GL.COLOR_ATTACHMENT1);
        uint[] id = new uint[1];
        GCHandle idHandle = GCHandle.Alloc(id, GCHandleType.Pinned);
        try
        {
            GL.ReadPixels(clampedX, flippedY, 1, 1, GL.RED_INTEGER, GL.UNSIGNED_INT, idHandle.AddrOfPinnedObject());
        }
        finally
        {
            idHandle.Free();
        }

        if (id[0] == 0)
        {
            GL.BindFramebuffer(GL.READ_FRAMEBUFFER, 0);
            return false;
        }

        GL.ReadBuffer(GL.NONE);
        float[] depth = new float[1];
        GCHandle depthHandle = GCHandle.Alloc(depth, GCHandleType.Pinned);
        try
        {
            GL.ReadPixels(clampedX, flippedY, 1, 1, GL.DEPTH_COMPONENT, GL.FLOAT, depthHandle.AddrOfPinnedObject());
        }
        finally
        {
            depthHandle.Free();
        }

        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, 0);

        if (depth[0] >= 1f)
        {
            return false;
        }

        objectId = id[0];
        linearDepth = camera.LinearizeDepth(depth[0]);
        return true;
    }

    public void Dispose()
    {
        _sceneFramebuffer?.Dispose();
        _dofFramebuffer?.Dispose();
        _fullscreenQuad.Dispose();
        _sceneShader.Dispose();
        _dofShader.Dispose();
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

        _sceneShader.Use();
        _sceneShader.SetMatrix4("uView", camera.GetViewMatrix());
        _sceneShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        _sceneShader.SetVector3("uCameraPosition", camera.Position);
        _sceneShader.SetVector3("uLightDirection", LightDirection);

        foreach (SceneObject sceneObject in scene.Objects)
        {
            sceneObject.Draw(_sceneShader);
        }
    }

    private void RenderDof(Camera camera)
    {
        _dofFramebuffer!.Bind();
        GL.Viewport(0, 0, _width, _height);
        GL.Disable(GL.DEPTH_TEST);
        GL.Disable(GL.CULL_FACE);

        GL.ClearBufferfv(GL.COLOR, 0, new[] { 0f, 0f, 0f, 1f });

        _dofShader.Use();
        _dofShader.SetFloat("uFocusDistance", FocusDistance);
        _dofShader.SetFloat("uFocusRange", FocusRange);
        _dofShader.SetFloat("uFocusTransition", FocusTransition);
        _dofShader.SetFloat("uNearBlurScale", NearBlurScale);
        _dofShader.SetFloat("uFarBlurScale", FarBlurScale);
        _dofShader.SetFloat("uMaxBlurRadius", MaxBlurRadius);
        _dofShader.SetFloat("uSigma", Sigma);
        _dofShader.SetFloat("uDepthSigma", DepthSigma);
        _dofShader.SetFloat("uNearPlane", camera.NearPlane);
        _dofShader.SetFloat("uFarPlane", camera.FarPlane);
        _dofShader.SetVector2("uTexelSize", new Vector2(1f / _width, 1f / _height));

        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer!.ColorTexture);
        GL.ActiveTexture(GL.TEXTURE0 + 1);
        GL.BindTexture(GL.TEXTURE_2D, _sceneFramebuffer.DepthTexture);

        _fullscreenQuad.Draw();

        GL.BindFramebuffer(GL.READ_FRAMEBUFFER, _dofFramebuffer.Handle);
        GL.BindFramebuffer(GL.DRAW_FRAMEBUFFER, 0);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GL.COLOR_BUFFER_BIT, GL.LINEAR);
        GL.BindFramebuffer(GL.FRAMEBUFFER, 0);
    }
}
