#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

half3 CalculatePBRWater(BRDFData brdfData, BRDFData brdfDataClearCoat,
	half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
	half3 normalWS, half3 viewDirectionWS,
	half clearCoatMask, bool specularHighlightsOff, half smoothness, half specularStep)
{
	half NdotL = saturate(dot(normalWS, lightDirectionWS));
	half3 radiance = lightColor * (lightAttenuation * NdotL);

	half3 h = normalize(lightDirectionWS + viewDirectionWS);
	float nh = max(0, dot(normalWS, h));
	float spec = pow(nh, lerp(48, 500, smoothness));
	spec = spec > 0.5f ? 1 : 0;
	radiance *= spec * 50;

	half3 brdf = brdfData.diffuse;

	return brdf * radiance;
}

half4 PBRLightingWater(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS);

    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        lightingData.mainLightColor = CalculatePBRWater(brdfData, brdfDataClearCoat, mainLight.color, mainLight.direction,
        	mainLight.distanceAttenuation * mainLight.shadowAttenuation, inputData.normalWS, inputData.viewDirectionWS,
        	surfaceData.clearCoatMask, specularHighlightsOff, surfaceData.smoothness, 0.5f);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
        	lightingData.additionalLightsColor += CalculatePBRWater(brdfData, brdfDataClearCoat, light.color, light.direction,
			light.distanceAttenuation * light.shadowAttenuation, inputData.normalWS, inputData.viewDirectionWS,
			surfaceData.clearCoatMask, specularHighlightsOff, surfaceData.smoothness, 0.01f);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
        	lightingData.additionalLightsColor += CalculatePBRWater(brdfData, brdfDataClearCoat, light.color, light.direction,
			light.distanceAttenuation * light.shadowAttenuation, inputData.normalWS, inputData.viewDirectionWS,
			surfaceData.clearCoatMask, specularHighlightsOff, surfaceData.smoothness / 100, 0.01f);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}
