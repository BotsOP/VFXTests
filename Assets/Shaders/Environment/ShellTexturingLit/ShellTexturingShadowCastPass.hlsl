#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes {
	uint id : SV_VertexID;
};

struct Interpolators {
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;
};

struct DrawVertex
{
	float3 position;
	float3 normal;
	float2 uv;
	float4 color;
};

struct DrawTriangle
{
	DrawVertex drawVertices[3];
};

StructuredBuffer<DrawTriangle> _DrawTrianglesBuffer;

float3 _LightDirection;

float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS) {
	float3 lightDirectionWS = _LightDirection;
	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
	
	// We have to make sure that the shadow bias didn't push the shadow out of
	// the camera's view area. This is slightly different depending on the graphics API
	#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	#endif
	return positionCS;
}

Interpolators Vertex(Attributes input) {
	Interpolators output;

	DrawTriangle tri = _DrawTrianglesBuffer[input.id / 3];
	DrawVertex v = tri.drawVertices[input.id  % 3];

	output.uv = v.uv;
	output.color = v.color;

	VertexPositionInputs posnInputs = GetVertexPositionInputs(v.position);
	VertexNormalInputs normInputs = GetVertexNormalInputs(v.normal); 

	output.positionCS = GetShadowCasterPositionCS(posnInputs.positionWS, normInputs.normalWS);
	return output;
}

TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap); 


float4 Fragment(Interpolators input) : SV_TARGET {
	float tex = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.uv).x;
	clip(tex - input.color.x);
	return 0;
}