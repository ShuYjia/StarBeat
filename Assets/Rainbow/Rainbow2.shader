Shader "Custom/Rainbow_Cinematic_Uniform"
{
    Properties
    {
        [Header(Shape Settings)]
        _Center("UV Center", Vector) = (0.5, 0, 0, 0)
        _Radius("Inner Radius", Range(0, 1)) = 0.3
        _Width("Rainbow Width", Range(0, 1)) = 0.15
        
        [Header(Color and Brightness)]
        [HDR]_EmissionColor("Global Tint & Intensity", Color) = (1, 1, 1, 1)
        _Saturation("Saturation", Range(0, 3)) = 1.5
        _Brightness("Brightness Boost", Range(1, 10)) = 2.0
        
        [Header(Fade Settings)]
        _SideFade("Side Fade Smoothness", Range(0, 1)) = 0.4
        _ThicknessFade("Thickness Smoothness", Range(0, 1)) = 0.25
        _Alpha("Global Alpha", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // 确保支持数组和稍微复杂的指令
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Center;
            float _Radius, _Width, _Alpha;
            float4 _EmissionColor;
            float _Saturation, _Brightness;
            float _SideFade, _ThicknessFade;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 核心优化：等比例均匀切分的 7 色光谱
            float3 GetRainbowColor(float t) {
                // 强制将 t 限制在 0-1 范围
                t = saturate(t);

                // 定义 7 种标准的物理光谱色
                float3 colors[7];
                colors[0] = float3(1.0, 0.0, 0.0);   // 红 (外圈)
                colors[1] = float3(1.0, 0.45, 0.0);  // 橙
                colors[2] = float3(1.0, 1.0, 0.0);   // 黄
                colors[3] = float3(0.0, 1.0, 0.0);   // 绿
                colors[4] = float3(0.0, 1.0, 1.0);   // 青
                colors[5] = float3(0.0, 0.2, 1.0);   // 蓝
                colors[6] = float3(0.5, 0.0, 1.0);   // 紫 (内圈)

                // 将 0-1 映射到 0-6 的索引空间
                float rawIdx = t * 6.0; 
                int idx = (int)floor(rawIdx);      // 当前基础颜色索引
                float f = rawIdx - (float)idx;     // 两个颜色间的混合比例

                // 限制索引最大值为 5，确保 idx+1 不会越界（最大取到 colors[6]）
                idx = clamp(idx, 0, 5);

                // 线性插值混合颜色
                return lerp(colors[idx], colors[idx + 1], f);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 relPos = i.uv - _Center.xy;
                float dist = length(relPos);
                
                // 1. 计算当前像素在彩虹厚度中的标准化位置 (t 从 0 到 1)
                // 外边缘 (dist = _Radius + _Width) 时 t = 0 (红色)
                // 内边缘 (dist = _Radius) 时 t = 1 (紫色)
                float t = (_Radius + _Width - dist) / max(_Width, 0.0001);
                
                // 如果超出彩虹圆环范围，直接丢弃以节省性能
                if (t < 0.0 || t > 1.0) discard;

                // 2. 获取均匀的彩虹颜色
                float3 col = GetRainbowColor(t);
                
                // 3. 色彩饱和度与 HDR 亮度增强
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722)); // 计算灰度亮度
                col = lerp(float3(luma, luma, luma), col, _Saturation);
                col *= _Brightness * _EmissionColor.rgb;

                // 4. 计算厚度边缘的平滑衰减 (模拟大气透镜感)
                float thicknessMask = smoothstep(0.0, _ThicknessFade, t) * smoothstep(1.0, 1.0 - _ThicknessFade, t);
                
                // 5. 计算两端接地的平滑衰减 (基于 UV.x)
                float sideMask = smoothstep(0.0, _SideFade, i.uv.x) * smoothstep(1.0, 1.0 - _SideFade, i.uv.x);
                
                // 6. 最终 Alpha 合成
                float finalAlpha = thicknessMask * sideMask * _Alpha;
                
                // 引入一个 0.8 的次幂，让色彩中心部分显得更加通透硬朗，边缘更柔和
                finalAlpha = pow(finalAlpha, 0.8);

                return float4(col, finalAlpha);
            }
            ENDHLSL
        }
    }
}