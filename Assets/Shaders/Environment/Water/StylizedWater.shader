Shader "NedMakesGames/MyLit" {
    Properties{
        [Header(Surface options)] 
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _Smoothness("Smoothness", Float) = 0
    }
    SubShader {
        // These tags are shared by all passes in this sub shader
        Tags {"RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent"}

        Pass {
            Name "ForwardLit" // For debugging
            Tags{"LightMode" = "UniversalForward" "RenderType" = "Transparent" "Queue" = "Transparent"} // Pass specific tags. 
            
            Zwrite off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM // Begin HLSL code

            #define _SPECULAR_COLOR
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            // Shader variant keywords
            // Unity automatically discards unused variants created using "shader_feature" from your final game build,
            // however it keeps all variants created using "multi_compile"
            // For this reason, multi_compile is good for global keywords or keywords that can change at runtime
            // while shader_feature is good for keywords set per material which will not change at runtime

            // Global URP keywords
#if UNITY_VERSION >= 202120
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
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