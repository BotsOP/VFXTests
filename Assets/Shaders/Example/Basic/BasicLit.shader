Shader "Example/MyLit" {
    Properties{
        [Header(Surface options)]
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha cutout threshold", Range(0, 1)) = 0.5
        _Smoothness("Smoothness", Float) = 0
        
        [HideInInspector] _Cull("Cull mode", Float) = 2 // 2 is "Back"
        [HideInInspector] _SourceBlend("Source blend", Float) = 0
        [HideInInspector] _DestBlend("Destination blend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 0
        [HideInInspector] _SurfaceType("Surface type", Float) = 0
        [HideInInspector] _FaceRenderingMode("Face rendering type", Float) = 0
    }

    SubShader {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SourceBlend][_DestBlend]
            ZWrite[_ZWrite] 
            Cull[_Cull]

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
            
            #include "BasicLitPass.hlsl"
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            #pragma shader_feature_local _ALPHA_CUTOUT
            #pragma shader_feature_local _DOUBLE_SIDED_NORMALS

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "BasicLitShadowPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "BasicLitCustomInspector"
}