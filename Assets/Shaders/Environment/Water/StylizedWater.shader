Shader "NedMakesGames/MyLit" {
    Properties{
        [Header(Surface options)] // Creates a text header
        [MainTexture] _BaseMap("Color", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _BumpScale("Normal scale", Float) = 0
        _BumpMap("Normal map", 2D) = "bump" {}

        _Smoothness("Smoothness", Float) = 0
    }
    SubShader {
        Tags {"RenderPipeline" = "UniversalPipeline"}

        Pass {
            Name "ForwardLit" // For debugging
            Tags{"LightMode" = "UniversalForward"} // Pass specific tags. 

            HLSLPROGRAM // Begin HLSL code

            #pragma shader_feature_local _NORMALMAP
            #define _SPECULAR_COLOR

#if UNITY_VERSION >= 202120
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#else
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#endif
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "StylizedWaterLitPass.hlsl"
            ENDHLSL
        }

//        Pass {
//            // The shadow caster pass, which draws to shadow maps
//            Name "ShadowCaster"
//            Tags{"LightMode" = "ShadowCaster"}
//
//            ColorMask 0 // No color output, only depth
//
//            HLSLPROGRAM
//            #pragma vertex Vertex
//            #pragma fragment Fragment
//
//            #include "MyLitShadowCasterPass.hlsl"
//            ENDHLSL
//        }
    }
}

//float4 LightingWater(SurfaceOutputWater s, float3 lightDir, half3 viewDir, half atten)
//{
//	float4 col = float4(s.albedo, s.alpha);
//	float ndotl = saturate(dot(s.normal, normalize(lightDir)));
//	float shadow = ndotl * atten;
//	col.rgb *= shadow;
//
//	half3 h = normalize(lightDir + viewDir);
//	float nh = max(0, dot(s.normal, h));
//	float spec = pow(nh, 48);
//	col.rgb = spec * shadow;
//
//	return col;
//}