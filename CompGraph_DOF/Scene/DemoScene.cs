using System.Numerics;
using CompGraph_DOF.Graphics;

namespace CompGraph_DOF.Scene;

internal sealed class DemoScene : IDisposable
{
    private readonly Mesh _cubeMesh;
    private readonly Mesh _sphereMesh;

    public IReadOnlyList<SceneObject> Objects { get; }
    public float DefaultFocusDistance { get; }

    public DemoScene()
    {
        _cubeMesh = Mesh.CreateCube();
        _sphereMesh = Mesh.CreateSphere(32, 16);

        var objects = new List<SceneObject>
        {
            new(
                1,
                "Near Cube",
                _cubeMesh,
                Matrix4x4.CreateScale(1.2f) * Matrix4x4.CreateRotationY(0.55f) * Matrix4x4.CreateTranslation(-2.2f, 0f, 4.0f),
                new Vector3(0.88f, 0.38f, 0.22f),
                0.26f,
                18f),
            new(
                2,
                "Center Sphere",
                _sphereMesh,
                Matrix4x4.CreateScale(1.2f) * Matrix4x4.CreateTranslation(0.0f, 0f, 0.0f),
                new Vector3(0.30f, 0.66f, 0.82f),
                0.38f,
                22f),
            new(
                3,
                "Far Cube",
                _cubeMesh,
                Matrix4x4.CreateScale(1.2f) * Matrix4x4.CreateRotationY(-0.55f) * Matrix4x4.CreateTranslation(2.2f, 0f, -4.0f),
                new Vector3(0.34f, 0.74f, 0.42f),
                0.26f,
                18f)
        };

        Objects = objects;
        DefaultFocusDistance = 11.5f;
    }

    public void Dispose()
    {
        _cubeMesh.Dispose();
        _sphereMesh.Dispose();
    }

    public bool TryGetObject(uint id, out SceneObject? sceneObject)
    {
        foreach (SceneObject candidate in Objects)
        {
            if (candidate.Id == id)
            {
                sceneObject = candidate;
                return true;
            }
        }

        sceneObject = null;
        return false;
    }
}
