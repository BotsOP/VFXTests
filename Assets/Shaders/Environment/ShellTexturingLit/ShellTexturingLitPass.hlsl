#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Textures
TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap); 
TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap); 

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

half3 CalculateDiffuse(Light light, InputData inputData, SurfaceData surfaceData)
{
	half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
	half3 lightColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

	lightColor *= surfaceData.albedo;

	#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
	half smoothness = exp2(10 * surfaceData.smoothness + 1);

	//lightColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);
	#endif

	return lightColor;
}

half4 UniversalFragment(InputData inputData, SurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        lightingData.mainLightColor += CalculateDiffuse(mainLight, inputData, surfaceData);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}


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
	float4 color = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv);
	float tex = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv).x;
	clip(tex - input.color.x);

	InputData lightingInput = (InputData)0; 
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); 
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
	
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = _ColorTint * input.color.x;
	surfaceInput.albedo = input.color.yzw;
	surfaceInput.alpha = 1;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = _Smoothness;

#if UNITY_VERSION >= 202120
	return UniversalFragment(lightingInput, surfaceInput);
#else
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif
}