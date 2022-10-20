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

