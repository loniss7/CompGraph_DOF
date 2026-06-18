#version 330 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uInputColor;
uniform sampler2D uSceneDepth;
uniform vec2 uTexelSize;
uniform vec2 uDirection;
uniform float uFocusDistance;
uniform float uFocusRange;
uniform float uFocusTransition;
uniform float uNearBlurScale;
uniform float uFarBlurScale;
uniform float uMaxBlurRadius;
uniform float uSigma;
uniform float uDepthSigma;
uniform int uBlurEnabled;
uniform float uNearPlane;
uniform float uFarPlane;

float LinearizeDepth(float depth, float nearPlane, float farPlane)
{
    float zNdc = depth * 2.0 - 1.0;
    return (2.0 * nearPlane * farPlane) /
           (farPlane + nearPlane - zNdc * (farPlane - nearPlane));
}

float ComputeCoC(float linearDepth)
{
    if (uBlurEnabled == 0)
    {
        return 0.0;
    }

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

float Gaussian(float x, float sigma)
{
    return exp(-(x * x) / (2.0 * sigma * sigma));
}

void main()
{
    vec3 centerColor = texture(uInputColor, vTexCoord).rgb;
    float centerDepth = texture(uSceneDepth, vTexCoord).r;
    float centerLinearDepth = LinearizeDepth(centerDepth, uNearPlane, uFarPlane);
    float centerCoC = abs(ComputeCoC(centerLinearDepth));

    float effectiveRadius = uMaxBlurRadius * centerCoC;
    if (effectiveRadius <= 0.0001)
    {
        FragColor = vec4(centerColor, 1.0);
        return;
    }

    const int KERNEL_RADIUS = 6;
    float sampleScale = effectiveRadius / float(KERNEL_RADIUS);
    float sigma = max(uSigma, 0.0001);
    float depthSigma = max(uDepthSigma, 0.0001);

    vec3 blurAccum = vec3(0.0);
    float weightAccum = 0.0;

    for (int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; ++i)
    {
        vec2 sampleUv = clamp(
            vTexCoord + uDirection * float(i) * sampleScale * uTexelSize,
            vec2(0.0),
            vec2(1.0));

        vec3 sampleColor = texture(uInputColor, sampleUv).rgb;
        float sampleDepth = texture(uSceneDepth, sampleUv).r;
        float sampleLinearDepth = LinearizeDepth(sampleDepth, uNearPlane, uFarPlane);

        float gaussianWeight = Gaussian(float(i) * sampleScale, sigma);
        float depthDelta = sampleLinearDepth - centerLinearDepth;
        float depthWeight = exp(
            -(depthDelta * depthDelta) /
            (2.0 * depthSigma * depthSigma));
        float softDepthWeight = mix(0.35, 1.0, depthWeight);
        float weight = gaussianWeight * softDepthWeight;
        blurAccum += sampleColor * weight;
        weightAccum += weight;
    }

    FragColor = vec4(blurAccum / max(weightAccum, 0.0001), 1.0);
}
