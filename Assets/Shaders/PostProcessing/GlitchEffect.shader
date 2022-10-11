Shader "Example/URPUnlitShaderBasic"
{
    Properties
    { 
        _MainTex ("Texture", 2D) = "white" {}    
        _PosOrigin ("Shockwave Position", Vector) = (0, 0, 0, 0)
        _Distance ("Distance Travelled", Float) = 0.5
        _Width ("Shockwave width", Range(0.1, 3.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Shaders/Library/Noise.cginc"
            
            struct meshdata
            {
                float4 positionOS   : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct interpolator
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            interpolator vert(meshdata IN)
            {
                interpolator OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                
                return OUT;
            }

            sampler2D _MainTex;
            float3 _PosOrigin;
            float _Distance;
            float _Width;

            half4 frag(interpolator IN) : SV_Target
            {
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                float3 ndc = float3(UV * 2 - 1, depth * 2 - 1);
                float3 clip = mul(UNITY_MATRIX_I_VP, float4(ndc, 1));
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
                
                half4 col = tex2D(_MainTex, IN.uv);
                //float camDist = distance(GetCameraPositionWS()

                float3 v = WNoise(worldPos, 0);
                
                return half4(v.xxx, 1);
            }
            ENDHLSL
        }
    }
}