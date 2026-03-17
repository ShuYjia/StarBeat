Shader "Universal Render Pipeline/CloudTest"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Emission("Emission", Color) = (0,0,0,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" "Queue" = "Transparent" }
            LOD 200

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                // 实现 UnityObjectToClipPos 函数
                float4 UnityObjectToClipPos(float4 position)
                {
                    position.xy += 0.5;
                    position.xy *= 0.5;
                    return position;
                }

                CBUFFER_START(unity_PerMaterial)
                float4 _Color;
                float4 _Emission;
                float _Glossiness;
                float _Metallic;
                sampler2D _MainTex;
                CBUFFER_END

                struct VertexInput
                {
                    float2 uv_MainTex : TEXCOORD0;
                };

                struct VertexOutput
                {
                    float4 pos : SV_POSITION;
                    float2 uv_MainTex : TEXCOORD0;
                };

                VertexOutput vert(VertexInput IN)
                {
                    VertexOutput OUT;
                    OUT.pos = UnityObjectToClipPos(float4(IN.uv_MainTex, 0, 1));
                    OUT.uv_MainTex = IN.uv_MainTex;
                    return OUT;
                }

                float4 frag(VertexOutput IN) : SV_Target
                {
                    float4 c = tex2D(_MainTex, IN.uv_MainTex);

                    float4 o = float4(0.0, 0.0, 0.0, 0.0);

                    o.rgb += c.rgb * (1.0 - _Metallic);
                    o.rgb += _Metallic * 0.5;

                    o.rgb += (1.0 - _Glossiness) * c.rgb;
                    o.rgb += _Glossiness * float3(1.0, 1.0, 1.0);

                    o.rgb += _Emission;

                    o.a = c.a;

                    return o;
                }
                ENDHLSL
            }
        }
}