Shader "Custom/LitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    	_Color ("Color", Color) = (0.25, 0.5, 0.5, 1)
	    _Gloss ("Gloss", Range(0, 2)) = 1
	    _TimeScale ("TimeScale", Range(0, 10)) = 1

        _WaveA ("Wave A (dir, steepness, wavelength)", Vector) = (1,0,0.5,10)
    	_WaveB ("Wave B", Vector) = (0,1,0.25,20)
    	_WaveC ("Wave C", Vector) = (1,1,0.15,10)
    }
    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True" 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
        }
        LOD 100
    	Blend SrcAlpha OneMinusSrcAlpha

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct meshData
            {
                float4 vertex : POSITION;
            	float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct interpolators
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
            	float3 wPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Gloss;
            float _TimeScale;

            float4 _WaveA;
            float4 _WaveB;
            float4 _WaveC;

            float3 GerstnerWave (
			float4 wave, float3 p, inout float3 tangent, inout float3 binormal
				)
            {
			    float steepness = wave.z;
			    float wavelength = wave.w;
			    float k = 2 * PI / wavelength;
				float c = sqrt(9.8 / k);
				float2 d = normalize(wave.xy);
				float f = k * (dot(d, p.xz) - c * (_Time.y / _TimeScale));
				float a = steepness / k;
				
				//p.x += d.x * (a * cos(f));
				//p.y = a * sin(f);
				//p.z += d.y * (a * cos(f));

				tangent += float3(
					-d.x * d.x * (steepness * sin(f)),
					d.x * (steepness * cos(f)),
					-d.x * d.y * (steepness * sin(f))
				);
				binormal += float3(
					-d.x * d.y * (steepness * sin(f)),
					d.y * (steepness * cos(f)),
					-d.y * d.y * (steepness * sin(f))
				);
				return float3(
					d.x * (a * cos(f)),
					a * sin(f),
					d.y * (a * cos(f))
				);
			}

            interpolators vert (meshData v)
            {
                interpolators i;

                float3 gridPoint = v.vertex.xyz;
   
				float3 tangent = float3(1, 0, 0);
				float3 binormal = float3(0, 0, 1);
            	float3 p = gridPoint;

            	p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
            	p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
            	p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
            	float3 normal = normalize(cross(normalize(binormal), normalize(tangent)));
	   
        		v.vertex.xyz = p;
            	i.vertex = TransformObjectToHClip(v.vertex);
        		i.normal = normal;
            	i.uv = TRANSFORM_TEX(v.uv, _MainTex);
				i.wPos = mul(unity_ObjectToWorld, v.vertex);
            	
            	return i;
            }

            float4 frag (interpolators i) : SV_Target
            {
            	//Diffuse
            	float3 N = normalize(i.normal);
            	float3 L = normalize(_MainLightPosition.xyz); // actually a dir
            	float3 lambert = saturate(dot(N, L));
            	float diffuseLight = lambert * _MainLightColor.xyz;
            	float3 diffuse = _Color * diffuseLight;

            	//Specular
            	float3 V = normalize(_WorldSpaceCameraPos - i.wPos);
            	float3 H = normalize(L + V);
            	float3 specular = saturate(dot(N, H)) * (lambert > 0);
            	float specularExponent = exp2(_Gloss * 6 + 1);
            	specular = pow(specular, specularExponent);
            	
                //fixed4 col = tex2D(_MainTex, i.uv);

                return float4(diffuse + specular, 1);
            }
            ENDHLSL
        }
    }
}
