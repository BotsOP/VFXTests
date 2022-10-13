Shader "Custom/ShellTexturingLit" {
    Properties{
        [Header(Surface options)]
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Float) = 0
        
        [HideInInspector] _SourceBlend("Source blend", float) = 0
        [HideInInspector] _DestBlend("Destination blend", float) = 0
        [HideInInspector] _Zwrite("Zwrite", float) = 0
        
        [HideInInspector] _SurfaceType("Surface type", float) = 0
    }
    SubShader {
        Tags {"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"}

        Pass {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward" }
            // "UniversalForward" tells Unity this is the main lighting pass of this shader
            
            Cull off
            blend [_SourceBlend] [_DestBlend]
            Zwrite [_Zwrite]

            HLSLPROGRAM

            #define _SPECULAR_COLOR

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
            #pragma target 5.0

            #include "ShellTexturingLitPass.hlsl"
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0 // No color output, only depth
            

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma shader_feature_local _ALPHA_CUTOUT
            
            #include "ShellTexturingShadowCastPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LitCustomInspector"
}