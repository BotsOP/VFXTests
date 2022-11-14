#ifndef MY_LIT_FORWARD_LIT_PASS_INCLUDED
#define MY_LIT_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "BasicCommon.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 uv : TEXCOORD0;
};

struct Interpolators {
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
};

Interpolators Vertex(Attributes input) {
	Interpolators output;

	// Found in URP/ShaderLib/ShaderVariablesFunctions.hlsl
	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
	
	output.positionCS = posnInputs.positionCS;
	output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
	output.normalWS = normInputs.normalWS;
	output.positionWS = posnInputs.positionWS;

	return output;
}

float4 Fragment(Interpolators input
#ifdef _DOUBLE_SIDED_NORMALS
	, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
#endif
) : SV_TARGET {
	float2 uv = input.uv;
	float4 colorSample = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv);
	TestAlphaClip(colorSample);

	float3 normalWS = normalize(input.normalWS);
#ifdef _DOUBLE_SIDED_NORMALS
	normalWS *= IS_FRONT_VFACE(frontFace, 1, -1);
#endif
	
	InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalWS;
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl
	
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = colorSample.rgb * _ColorTint.rgb;
	surfaceInput.alpha = colorSample.a * _ColorTint.a;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = _Smoothness;

#if UNITY_VERSION >= 202120
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
#else
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif
}

#endif