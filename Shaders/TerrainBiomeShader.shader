Shader "Custom/TerrainBiomeShader"
{
    Properties
    {
        _IndexTex ("Index Texture", 2D) = "white" {}
        _BiomeTexArray ("Biome Texture Array", 2DArray) = "" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _IndexTex;
            UNITY_DECLARE_TEX2DARRAY(_BiomeTexArray);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Read index (0–1 → 0–255)
                float index = tex2D(_IndexTex, i.uv).r * 255.0;
                int biomeIndex = (int)(index + 0.5);

                // Sample texture array
                float3 uvw = float3(i.uv, biomeIndex);
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_BiomeTexArray, uvw);

                return col;
            }
            ENDCG
        }
    }
}