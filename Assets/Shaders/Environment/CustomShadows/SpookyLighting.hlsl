#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
half3 CalculateSpookyLighting(Light light, InputData inputData, SurfaceData surfaceData)
{
	half3 shadowColor = lerp(surfaceData.albedo, float3(0, 0, 0), light.shadowAttenuation);
	half3 attenuatedLightColor = light.color * (light.distanceAttenuation * shadowColor);
	half ndotl = saturate(dot(light.direction, inputData.normalWS));
	half3 lightColor = ndotl * attenuatedLightColor;

	lightColor *= surfaceData.albedo;

	#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
	float3 halfVec = SafeNormalize(float3(light.direction) + float3(inputData.viewDirectionWS));
	half NdotH = half(saturate(dot(inputData.normalWS, halfVec)));
	half modifier = pow(NdotH, lerp(48, 500, surfaceData.smoothness));
	half3 specularReflection = half4(surfaceData.specular, 1).rgb * modifier;
	half3 specular = attenuatedLightColor * specularReflection;

	lightColor += specular * surfaceData.smoothness;
	#endif
	
	return lightColor;
}

float4 SpookyLighting(SurfaceData surfaceData, InputData inputData)
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
		lightingData.mainLightColor += CalculateSpookyLighting(mainLight, inputData, surfaceData);
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
		lightingData.additionalLightsColor += CalculateSpookyLighting(light, inputData, surfaceData);
	}
	LIGHT_LOOP_END
	#endif

	#if defined(_ADDITIONAL_LIGHTS_VERTEX)
	lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
	#endif

	return CalculateFinalColor(lightingData, surfaceData.alpha);
}

