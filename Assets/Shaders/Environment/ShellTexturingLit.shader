Shader "Unlit/ShellTexturingLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct interpolator
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct DrawVertex
            {
                float3 position;
                float3 normal;
                float2 uv;
                float4 color;
            };

            struct DrawTriangle
            {
                DrawVertex drawVertices[3];
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            StructuredBuffer<DrawTriangle> _DrawTrianglesBuffer;
            
            interpolator vert (uint vertexID : SV_VertexID)
            {
                interpolator o;
                DrawTriangle tri = _DrawTrianglesBuffer[vertexID / 3];
                DrawVertex v = tri.drawVertices[vertexID % 3];
                
                o.vertex = UnityObjectToClipPos(v.position);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (interpolator i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
