Shader "Universal Render Pipeline/Wireframe"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ColorLifetime("Color lifetime", 2D) = "white" {}
        _Color ("Color", Color) = (0.25, 0.5, 0.5, 1)
        _WireframeFrontColour("Wireframe front colour", color) = (1.0, 1.0, 1.0, 1.0)
        _WireframeWidth("Wireframe width", float) = 1
        _TotalTime("Total time effect", float) = 1
        _targetPos("target position", vector) = (0, 0, 0, 0)
        _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Opaque"
        }
        LOD 100
        Cull[_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct meshdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD1;
                float2 displacement : TEXCOORD6;
            };

            struct interpolator
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD2;
                float3 wPos : TEXCOORD3;
                float2 uv : TEXCOORD1;
                float2 displacement : TEXCOORD6;
            };

            // We add our barycentric variables to the geometry struct.
            struct g2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD2;
                float2 uv : TEXCOORD1;
                float3 wPos : TEXCOORD3;
                float3 barycentric : TEXCOORD0;
                float2 displacement : TEXCOORD6;
            };

            sampler2D _MainTex;
            sampler2D _ColorLifetime;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _targetPos;

            interpolator vert(meshdata v)
            {
                interpolator o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                o.wPos = v.vertex;
                o.displacement = v.displacement;
                return o;
            }

            // This applies the barycentric coordinates to each vertex in a triangle.
            [maxvertexcount(3)]
            void geom(triangle interpolator IN[3], inout TriangleStream<g2f> triStream) {
                g2f o;
                
                o.pos = IN[0].vertex;
                o.barycentric = float3(1.0, 0.0, 0.0);
                o.normal = IN[0].normal;
                o.uv = IN[0].uv;
                o.wPos = IN[0].wPos;
                o.displacement = IN[0].displacement;
                triStream.Append(o);
                
                o.pos = IN[1].vertex;
                o.barycentric = float3(0.0, 1.0, 0.0);
                o.normal = IN[1].normal;
                o.uv = IN[1].uv;
                o.wPos = IN[1].wPos;
                o.displacement = IN[1].displacement;
                triStream.Append(o);
                
                o.pos = IN[2].vertex;
                o.barycentric = float3(0.0, 0.0, 1.0);
                o.normal = IN[2].normal;
                o.uv = IN[2].uv;
                o.wPos = IN[2].wPos;
                o.displacement = IN[2].displacement;
                triStream.Append(o);
            }

            half4 _WireframeFrontColour;
            float _WireframeWidth;
            float _TotalTime;

            half4 frag(g2f i) : SV_Target
            {
                //Diffuse
            	float3 N = normalize(i.normal);
            	float3 Lpos = _MainLightPosition.xyz; 
            	float3 L = normalize(Lpos); 
            	float3 lambert = saturate(dot(N, L));
            	float diffuseLight = lambert * _MainLightColor.xyz;
                float3 diffuseColor = _Color * tex2D(_MainTex, i.uv);
            	float3 diffuse = diffuseColor * diffuseLight;

                if(i.displacement.x > 0)
                {
                    float3 unitWidth = fwidth(i.barycentric);
                    float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * _WireframeWidth, i.barycentric);
                    float alpha = 1 - min(aliased.x, min(aliased.y, aliased.z));
                    
                    float localTime = (_Time.y - i.displacement.y + 0.001) / _TotalTime;
                    float3 wireFrameColor = tex2D(_ColorLifetime, float2(localTime, 1));
                    half3 color = lerp(diffuse, wireFrameColor, alpha);
                    color = lerp(diffuse, color, i.displacement.x * 10);
                    return half4(color, 1);
                }

                return half4(diffuse, 1);
            }
            ENDHLSL
        }
    }
}