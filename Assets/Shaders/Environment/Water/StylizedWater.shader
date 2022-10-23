Shader "NedMakesGames/MyLit" {
    Properties{
        [Header(Surface options)] // Creates a text header
        [MainTexture] _BaseMap("Color", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _WaterBottomColor("Dark water color", Color) = (0, 1, 1, 1)
        _WaterTopColor("Light water color", Color) = (0, 1, 1, 1)
        _WaterFogColor("Water fog color", Color) = (0, 1, 1, 1)
        _WaterFogDensity("Water fog density", Range(0, 2)) = 0.1
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.25
        _BumpScale("Normal scale", Float) = 0
        _BumpMap("Normal map", 2D) = "bump" {}
        _CamTex("camera texture", 2D) = "white" {}

        _Smoothness("Smoothness", Float) = 0
    }
    SubShader {
        Tags {"RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent"}
        
        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UseColorTexture" "Queue" = "Transparent" "RenderType" = "Transparent"}
            
            Zwrite off

            HLSLPROGRAM

            #pragma shader_feature_local _NORMALMAP 
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
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

