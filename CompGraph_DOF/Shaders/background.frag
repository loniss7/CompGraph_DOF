#version 330 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uBackgroundTexture;

void main()
{
    vec3 srgbColor = texture(uBackgroundTexture, vTexCoord).rgb;
    vec3 linearColor = pow(clamp(srgbColor, vec3(0.0), vec3(1.0)), vec3(2.2));
    FragColor = vec4(linearColor, 1.0);
}
