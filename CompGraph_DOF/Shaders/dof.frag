#version 330 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uSceneColorTexture;
uniform sampler2D uSceneDepthTexture;
uniform vec2 uTexelSize;
uniform float uFocusDistance;
uniform float uFocusRange;
uniform float uFocusTransition;
uniform float uNearBlurScale;
uniform float uFarBlurScale;
uniform float uMaxBlurRadius;
uniform float uSigma;
uniform float uDepthSigma;
uniform float uNearPlane;
uniform float uFarPlane;

float LinearizeDepth(float depth, float nearPlane, float farPlane)
{
    float zNdc = depth * 2.0 - 1.0;
    return (2.0 * nearPlane * farPlane) /
           (farPlane + nearPlane - zNdc * (farPlane - nearPlane));
}

float Gaussian(float x, float y, float sigma)
{
    return exp(-(x * x + y * y) / (2.0 * sigma * sigma));
}

float FocusBlurMask(float linearDepth)
{
    float signedDifference = linearDepth - uFocusDistance;
    float baseMask = smoothstep(uFocusRange, uFocusRange + uFocusTransition, abs(signedDifference));
    float scale = signedDifference < 0.0 ? uNearBlurScale : uFarBlurScale;
    return clamp(baseMask * scale, 0.0, 1.0);
}

void main()
{
    vec3 sharpColor = texture(uSceneColorTexture, vTexCoord).rgb;
    float centerDepth = texture(uSceneDepthTexture, vTexCoord).r;
    float centerLinearDepth = LinearizeDepth(centerDepth, uNearPlane, uFarPlane);
    float centerBlurMask = FocusBlurMask(centerLinearDepth);

    const int KERNEL_RADIUS = 4;
    float effectiveRadius = uMaxBlurRadius * centerBlurMask;
    float sampleScale = effectiveRadius / float(KERNEL_RADIUS);
    float sigma = max(uSigma, 0.0001);

    vec3 blurAccum = vec3(0.0);
    float weightAccum = 0.0;
    const float MinimumBlurContribution = 0.02;

    for (int y = -KERNEL_RADIUS; y <= KERNEL_RADIUS; ++y)
    {
        for (int x = -KERNEL_RADIUS; x <= KERNEL_RADIUS; ++x)
        {
            vec2 kernelOffset = vec2(float(x), float(y));
            vec2 sampleOffset = kernelOffset * sampleScale * uTexelSize;
            vec2 sampleUv = clamp(vTexCoord + sampleOffset, vec2(0.0), vec2(1.0));

            vec3 sampleColor = texture(uSceneColorTexture, sampleUv).rgb;
            float sampleDepth = texture(uSceneDepthTexture, sampleUv).r;
            float sampleLinearDepth = LinearizeDepth(sampleDepth, uNearPlane, uFarPlane);
            float sampleBlurMask = FocusBlurMask(sampleLinearDepth);

            float gaussianWeight = Gaussian(kernelOffset.x * sampleScale, kernelOffset.y * sampleScale, sigma);
            float depthWeight = exp(-abs(sampleLinearDepth - centerLinearDepth) * uDepthSigma);
            float weight = gaussianWeight * depthWeight * max(sampleBlurMask, MinimumBlurContribution);

            blurAccum += sampleColor * weight;
            weightAccum += weight;
        }
    }

    vec3 blurredColor = blurAccum / max(weightAccum, 0.0001);
    vec3 finalColor = mix(sharpColor, blurredColor, centerBlurMask);
    FragColor = vec4(finalColor, 1.0);
}
