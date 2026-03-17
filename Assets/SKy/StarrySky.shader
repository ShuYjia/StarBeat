Shader "URP/Custom/SkyDome_Stars_Optimized_Fixed"
{
    Properties
    {
        [Header(Base Sky)]
        _ColorA("Top Color (Deep Blue)", Color) = (0.02, 0.05, 0.2, 1.0)
        _ColorB("Middle Color (Purple)", Color) = (0.1, 0.02, 0.2, 1.0)
        _ColorC("Bottom Color (Horizon)", Color) = (0.2, 0.2, 0.4, 1.0)

        [Header(Star Settings)]
        _StarColor("Core Color", Color) = (1,1,1,1)
        _Density("Density (0-1)", Range(0,1)) = 0.3
        _GridScale("Grid Scale", Float) = 25.0
        _StarSizeMinMax("Radius MinMax", Vector) = (0.005, 0.015, 0, 0)

        [Header(Dynamic)]
        // --- 新增：控制移动的开关 ---
        [Toggle] _EnableMovement("Enable Star Wobble", Float) = 1.0 
        _TarPointspeed("Wobble Speed", Range(0.0, 2.0)) = 0.2
        _TwinkleSpeed("Twinkle Speed", Range(0.0, 5.0)) = 1.0
        
        [Header(Textures)]
        [NoScaleOffset]_StarEdgeColorTex("Edge Color (Sampled)", 2D) = "white" {}
        [NoScaleOffset]_StarMask("Star Mask", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline" = "UniversalPipeline" }
        Cull Off 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float2 random(float2 p) {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            CBUFFER_START(unity_PerMaterial)
                float4 _ColorA, _ColorB, _ColorC;
                float4 _StarColor;
                float _Density, _GridScale, _TarPointspeed, _TwinkleSpeed;
                float _EnableMovement; // 对应属性中的开关
                float4 _StarSizeMinMax;
            CBUFFER_END

            sampler2D _StarMask, _StarEdgeColorTex;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                // 1. 天空背景渐变
                float y_blend = 1.0 - input.uv.y;
                float3 sky_col = (y_blend < 0.5) ? lerp(_ColorA.rgb, _ColorB.rgb, y_blend * 2.0) 
                                                 : lerp(_ColorB.rgb, _ColorC.rgb, (y_blend - 0.5) * 2.0);
                
                // 2. 网格化逻辑
                float2 star_uv = input.uv * _GridScale;
                float2 ipos = floor(star_uv);
                float2 fpos = frac(star_uv);

                float2 rand_data = random(ipos);
                if (rand_data.x > _Density) return float4(sky_col, 1.0);

                // 3. 绘制星星
                // 计算目标点：如果 _EnableMovement 为 1 则移动，为 0 则固定在中心 (0.5)
                float2 offset = 0.4 * sin(_Time.y * _TarPointspeed + 6.2831 * rand_data);
                float2 targetPoint = 0.5 + (offset * _EnableMovement);
                
                // 计算当前像素到星星中心的距离
                float dist = length(fpos - targetPoint);

                // 随机半径
                float star_radius = lerp(_StarSizeMinMax.x, _StarSizeMinMax.y, rand_data.y);
                
                // 绘制核心和边缘
                float core_mask = smoothstep(star_radius + 0.002, star_radius, dist);
                float glow_mask = smoothstep(star_radius + 0.01, star_radius, dist);

                // 采样边缘颜色贴图
                float3 edge_col = tex2D(_StarEdgeColorTex, input.uv).rgb;
                float3 star_base = _StarColor.rgb * core_mask + edge_col * glow_mask * 0.5;

                // 闪烁与遮罩
                float twinkle = 1.0 + 0.5 * sin(_Time.y * _TwinkleSpeed + rand_data.x * 10.0);
                float mask = tex2D(_StarMask, input.uv + _Time.x * 0.1).r;

                float3 final_stars = star_base * twinkle * mask;
                return float4(sky_col + final_stars, 1.0);
            }
            ENDHLSL
        }
    }
}