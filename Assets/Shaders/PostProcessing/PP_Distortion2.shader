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
            
            struct MeshData
            {
                float4 positionOS   : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            Interpolators vert(MeshData IN)
            {
                Interpolators OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                
                return OUT;
            }

            sampler2D _MainTex;
            float3 _PosOrigin;
            float _Distance;
            float _Width;

            half4 frag(Interpolators IN) : SV_Target
            {
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif
                
                
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                float3 dir = _PosOrigin - worldPos;
                float shockwaveLength = length(_PosOrigin - worldPos);

                dir = normalize(dir);

                if(shockwaveLength < _Distance && shockwaveLength > _Distance - _Width)
                {
                    float shockwaveGradient = shockwaveLength - (_Distance - _Width);
                    shockwaveGradient *= (_Distance - _Width);
                    shockwaveGradient /= 50 * shockwaveLength;
                    float2 shockwaveDir = float2(dir.x, dir.z);
                    shockwaveDir *= shockwaveGradient;
                    half4 col = tex2D(_MainTex, IN.uv + shockwaveDir);
                    
                    return col;
                }
                
                half4 col = tex2D(_MainTex, IN.uv);
                
                return half4(col.xyz, 1);
            }
            ENDHLSL
        }
    }
}