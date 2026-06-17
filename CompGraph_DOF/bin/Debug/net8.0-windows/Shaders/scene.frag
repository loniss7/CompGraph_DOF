#version 330 core

in vec3 vWorldPosition;
in vec3 vNormal;

layout(location = 0) out vec4 FragColor;
layout(location = 1) out uint FragObjectId;

uniform vec3 uCameraPosition;
uniform vec3 uLightDirection;
uniform vec3 uBaseColor;
uniform float uSpecularStrength;
uniform float uShininess;
uniform uint uObjectId;

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(-uLightDirection);
    vec3 viewDir = normalize(uCameraPosition - vWorldPosition);
    vec3 halfDir = normalize(lightDir + viewDir);

    float diffuse = max(dot(normal, lightDir), 0.0);
    float specular = pow(max(dot(normal, halfDir), 0.0), max(uShininess, 1.0));

    vec3 ambient = uBaseColor * 0.16;
    vec3 litColor = ambient + uBaseColor * diffuse * 0.90 + vec3(specular * uSpecularStrength);

    FragColor = vec4(litColor, 1.0);
    FragObjectId = uObjectId;
}
