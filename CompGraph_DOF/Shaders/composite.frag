#version 330 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uSharpTexture;
uniform sampler2D uBlurredTexture;
uniform sampler2D uSceneDepth;
uniform usampler2D uSceneObjectIdTexture;
uniform float uFocusDistance;
uniform float uFocusRange;
uniform float uFocusTransition;
uniform float uNearBlurScale;
uniform float uFarBlurScale;
uniform float uNearPlane;
uniform float uFarPlane;
uniform int uDebugView;

float LinearizeDepth(float depth, float nearPlane, float farPlane)
{
    float zNdc = depth * 2.0 - 1.0;
    return (2.0 * nearPlane * farPlane) /
           (farPlane + nearPlane - zNdc * (farPlane - nearPlane));
}

float ComputeCoC(float linearDepth)
{
    if (linearDepth >= uFarPlane * 0.99999)
    {
        return 1.0;
    }

    float signedDifference = linearDepth - uFocusDistance;
    float mask = smoothstep(
        uFocusRange,
        uFocusRange + uFocusTransition,
        abs(signedDifference));
    float scale = signedDifference < 0.0 ? uNearBlurScale : uFarBlurScale;
    return sign(signedDifference) * clamp(mask * scale, 0.0, 1.0);
}

vec3 ToneMap(vec3 color)
{
    vec3 mapped = color / (color + vec3(1.0));
    return pow(mapped, vec3(1.0 / 2.2));
}

vec3 ObjectIdColor(uint objectId)
{
    if (objectId == 0u)
    {
        return vec3(0.0);
    }

    if (objectId == 1u)
    {
        return vec3(1.0, 0.18, 0.14);
    }

    if (objectId == 2u)
    {
        return vec3(0.18, 1.0, 0.22);
    }

    if (objectId == 3u)
    {
        return vec3(0.16, 0.28, 1.0);
    }

    uint hash = objectId * 1664525u + 1013904223u;
    float r = float(hash & 255u) / 255.0;
    float g = float((hash >> 8u) & 255u) / 255.0;
    float b = float((hash >> 16u) & 255u) / 255.0;
    return vec3(r, g, b);
}

void main()
{
    vec3 sharpColor = texture(uSharpTexture, vTexCoord).rgb;
    float centerDepth = texture(uSceneDepth, vTexCoord).r;
    float centerLinearDepth = LinearizeDepth(centerDepth, uNearPlane, uFarPlane);
    float coc = ComputeCoC(centerLinearDepth);
    float blurFactor = abs(coc);

    if (uDebugView == 1)
    {
        float visibleDepth = clamp((centerLinearDepth - 4.0) / 16.0, 0.0, 1.0);
        FragColor = vec4(vec3(visibleDepth), 1.0);
        return;
    }

    if (uDebugView == 2)
    {
        float magnitude = abs(coc);
        vec3 focusColor = vec3(0.0, 0.2, 0.0);
        vec3 nearColor = vec3(1.0, 0.05, 0.05);
        vec3 farColor = vec3(0.08, 0.18, 1.0);
        vec3 debugColor = coc < 0.0
            ? mix(focusColor, nearColor, magnitude)
            : mix(focusColor, farColor, magnitude);
        FragColor = vec4(debugColor, 1.0);
        return;
    }

    if (uDebugView == 6)
    {
        uint objectId = texture(uSceneObjectIdTexture, vTexCoord).r;
        FragColor = vec4(ObjectIdColor(objectId), 1.0);
        return;
    }

    vec3 blurredColor = texture(uBlurredTexture, vTexCoord).rgb;

    if (uDebugView == 0 || uDebugView == 5)
    {
        vec3 result = mix(sharpColor, blurredColor, blurFactor);
        FragColor = vec4(ToneMap(result), 1.0);
        return;
    }

    FragColor = vec4(ToneMap(sharpColor), 1.0);
}
