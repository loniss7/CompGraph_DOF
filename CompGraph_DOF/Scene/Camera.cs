using System.Numerics;

namespace CompGraph_DOF.Scene;

internal sealed class Camera
{
    private const float DefaultYaw = -90f;
    private const float DefaultPitch = 0f;

    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float FieldOfViewDegrees { get; }
    public float NearPlane { get; }
    public float FarPlane { get; }
    public float AspectRatio { get; private set; }
    public float MovementSpeed { get; set; } = 5.5f;
    public float MouseSensitivity { get; set; } = 0.08f;

    public Camera(float aspectRatio)
    {
        FieldOfViewDegrees = 52f;
        NearPlane = 0.1f;
        FarPlane = 80f;
        AspectRatio = aspectRatio;
        Reset();
    }

    public void Reset()
    {
        Position = new Vector3(0f, 0f, 11.5f);
        Yaw = DefaultYaw;
        Pitch = DefaultPitch;
    }

    public void SetAspectRatio(float aspectRatio)
    {
        AspectRatio = MathF.Max(0.0001f, aspectRatio);
    }

    public Vector3 GetForwardVector()
    {
        float yawRadians = DegreesToRadians(Yaw);
        float pitchRadians = DegreesToRadians(Pitch);

        Vector3 forward = new(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians));

        return Vector3.Normalize(forward);
    }

    public Vector3 GetRightVector()
    {
        Vector3 forward = GetForwardVector();
        return Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
    }

    public Vector3 GetUpVector()
    {
        Vector3 right = GetRightVector();
        Vector3 forward = GetForwardVector();
        return Vector3.Normalize(Vector3.Cross(right, forward));
    }

    public void MoveForward(float amount)
    {
        Position += GetForwardVector() * amount;
    }

    public void MoveRight(float amount)
    {
        Position += GetRightVector() * amount;
    }

    public void MoveUp(float amount)
    {
        Position += Vector3.UnitY * amount;
    }

    public void Rotate(float yawDelta, float pitchDelta)
    {
        Yaw += yawDelta;
        Pitch = Math.Clamp(Pitch + pitchDelta, -89f, 89f);
    }

    public Matrix4x4 GetViewMatrix()
    {
        Vector3 forward = GetForwardVector();
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        float tx = -Vector3.Dot(right, Position);
        float ty = -Vector3.Dot(up, Position);
        float tz = Vector3.Dot(forward, Position);

        return new Matrix4x4(
            right.X, right.Y, right.Z, tx,
            up.X, up.Y, up.Z, ty,
            -forward.X, -forward.Y, -forward.Z, tz,
            0f, 0f, 0f, 1f);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        float fovRadians = DegreesToRadians(FieldOfViewDegrees);
        float f = 1f / MathF.Tan(fovRadians * 0.5f);
        float range = NearPlane - FarPlane;

        return new Matrix4x4(
            f / AspectRatio, 0f, 0f, 0f,
            0f, f, 0f, 0f,
            0f, 0f, (FarPlane + NearPlane) / range, (2f * FarPlane * NearPlane) / range,
            0f, 0f, -1f, 0f);
    }

    public float LinearizeDepth(float depth)
    {
        float zNdc = depth * 2f - 1f;
        return (2f * NearPlane * FarPlane) /
               (FarPlane + NearPlane - zNdc * (FarPlane - NearPlane));
    }

    private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
}
