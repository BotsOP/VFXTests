Shader "NedMakesGames/MyLit" {
    Properties{
        [Header(Surface options)]
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Float) = 0
    }

    SubShader {
        Tags {"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward" "RenderType" = "Transparent" "Queue" = "Transparent"}

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite off

            HLSLPROGRAM

            #define _SPECULAR_COLOR
            #pragma shader_feature_local _ALPHA_CUTOUT
            #pragma shader_feature_local _DOUBLE_SIDED_NORMALS
            
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
//            Name "ShadowCaster"
//            Tags{"LightMode" = "ShadowCaster"}
//
//            ColorMask 0
//            Cull[_Cull]
//
//            HLSLPROGRAM
//
//            #pragma shader_feature_local _ALPHA_CUTOUT
//            #pragma shader_feature_local _DOUBLE_SIDED_NORMALS
//
//            #pragma vertex Vertex
//            #pragma fragment Fragment
//
//            #include "MyLitShadowCasterPass.hlsl"
//            ENDHLSL
//        }
    }
}