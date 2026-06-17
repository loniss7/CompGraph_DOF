using System.Numerics;
using CompGraph_DOF.Graphics;

namespace CompGraph_DOF.Scene;

internal sealed class SceneObject
{
    public uint Id { get; }
    public string Name { get; }
    public Mesh Mesh { get; }
    public Matrix4x4 Model { get; set; }
    public Vector3 BaseColor { get; }
    public float SpecularStrength { get; }
    public float Shininess { get; }

    public SceneObject(uint id, string name, Mesh mesh, Matrix4x4 model, Vector3 baseColor, float specularStrength, float shininess)
    {
        Id = id;
        Name = name;
        Mesh = mesh;
        Model = model;
        BaseColor = baseColor;
        SpecularStrength = specularStrength;
        Shininess = shininess;
    }

    public void Draw(ShaderProgram shader)
    {
        // Keep the CPU-side model matrix in row-major form and upload the transposed copy for GLSL.
        shader.SetMatrix4("uModel", Matrix4x4.Transpose(Model));
        shader.SetVector3("uBaseColor", BaseColor);
        shader.SetFloat("uSpecularStrength", SpecularStrength);
        shader.SetFloat("uShininess", Shininess);
        shader.SetUInt("uObjectId", Id);
        Mesh.Draw();
    }
}
