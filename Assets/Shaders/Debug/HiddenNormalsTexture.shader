Shader "Hidden/Roystan/Normals Texture"
{
    Properties
    {
    }
    SubShader
    {
        Tags 
		{ 
			"RenderType" = "Opaque" 
		}

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 viewNormal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = GetVertexPositionInputs(v.vertex).positionCS;
                o.viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(1, 0, 0, 1);
                return float4(i.viewNormal, 0);
            }
            ENDHLSL
        }
    }
}
