Shader "Custom/TerrainBiomeBlend"
{
    Properties
    {
        _TexArray ("Biome Texture Array", 2DArray) = "" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_TexArray);
            SAMPLER(sampler_TexArray);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 uv2        : TEXCOORD1; // biome indices
                float4 uv3        : TEXCOORD2; // biome weights
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                nointerpolation float4 biomeIdx : TEXCOORD1;
                float4 biomeWgt   : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.biomeIdx = IN.uv2;
                OUT.biomeWgt = IN.uv3;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                int i0 = (int)IN.biomeIdx.x;
                int i1 = (int)IN.biomeIdx.y;
                int i2 = (int)IN.biomeIdx.z;

                half4 col = 0;
                col += SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, IN.uv, i0) * IN.biomeWgt.x;
                col += SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, IN.uv, i1) * IN.biomeWgt.y;
                col += SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, IN.uv, i2) * IN.biomeWgt.z;

                return col;
            }
            ENDHLSL
        }
    }
}
