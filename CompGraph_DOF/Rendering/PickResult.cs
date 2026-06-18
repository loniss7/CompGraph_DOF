namespace CompGraph_DOF.Rendering;

internal readonly record struct PickResult(
    uint ObjectId,
    float RawDepth,
    float LinearDepth,
    int FramebufferX,
    int FramebufferY);
