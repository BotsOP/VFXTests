#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Textures
TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap); 

float4 _ColorMap_ST; // This is automatically set by Unity. Used in TRANSFORM_TEX to apply UV tiling
float4 _ColorTint;
float _Smoothness;

struct Attributes {
	uint id : SV_VertexID;
};

struct Interpolators {
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
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

Interpolators Vertex(Attributes input) {
	Interpolators output;

	DrawTriangle tri = _DrawTrianglesBuffer[input.id / 3];
	DrawVertex v = tri.drawVertices[input.id  % 3];
	
	VertexPositionInputs posnInputs = GetVertexPositionInputs(v.position);
	VertexNormalInputs normInputs = GetVertexNormalInputs(v.normal);
	
	output.positionCS = posnInputs.positionCS;
	output.uv = v.uv;
	output.normalWS = normInputs.normalWS;
	output.positionWS = posnInputs.positionWS;
	output.color = v.color;

	return output;
}

float4 Fragment(Interpolators input) : SV_TARGET{
	float2 uv = input.uv;
	float tex = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv).x;
	clip(tex - input.color.x);

	InputData lightingInput = (InputData)0; 
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); 
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = _ColorTint.rgb;
	surfaceInput.alpha = 1;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = _Smoothness;

#if UNITY_VERSION >= 202120
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
#else
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif
}