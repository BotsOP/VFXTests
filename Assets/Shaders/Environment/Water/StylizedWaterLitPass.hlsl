#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "WaterLighting.hlsl"

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); 
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap); 

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Parallax;
half _OcclusionStrength;
half _ClearCoatMask;
half _ClearCoatSmoothness;
half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _Surface;
CBUFFER_END

struct Attributes {
	float3 positionOS : POSITION;
	float4 tangentOS    : TANGENT;
	float3 normalOS : NORMAL; 
	float2 uv : TEXCOORD0;
};

struct Interpolators {
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	half4 tangentWS : TEXCOORD3;
	half4 fogFactorAndVertexLight : TEXCOORD4;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord              : TEXCOORD5;
#endif
};

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
	return UnpackNormalScale(n, scale);
	return UnpackNormal(n);
}

void InitializeInputData(Interpolators input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    //inputData.normalWS = input.normalWS;

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
	
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

#if defined(DEBUG_DISPLAY)
    inputData.vertexSH = input.vertexSH;
#endif
}

struct SurfaceOutputWater
{
	half3 albedo;
	half alpha;
	half3 normal;
};



Interpolators Vertex(Attributes input) {
	Interpolators output;

	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

	half3 vertexLight = VertexLighting(posnInputs.positionWS, normInputs.normalWS);
	half fogFactor = ComputeFogFactor(posnInputs.positionCS.z);

	real sign = input.tangentOS.w * GetOddNegativeScale();
	half4 tangentWS = half4(normInputs.tangentWS.xyz, sign);
	output.tangentWS = tangentWS;

	output.positionCS = posnInputs.positionCS;
	output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
	output.normalWS = normInputs.normalWS;
	output.positionWS = posnInputs.positionWS;
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

	return output;
}

float4 Fragment(Interpolators input) : SV_TARGET{
	float2 uv = input.uv;
	// Sample the color map
	float4 colorSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
	
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = colorSample.rgb * _BaseColor.rgb;
	surfaceInput.alpha = colorSample.a * _BaseColor.a;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = _Smoothness;
	surfaceInput.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
	//return float4(surfaceInput.normalTS, 1);

	InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
	InitializeInputData(input, surfaceInput.normalTS, lightingInput);
	
	return LightingWater(surfaceInput, lightingInput);

#if UNITY_VERSION >= 202120
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
#else
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif
}