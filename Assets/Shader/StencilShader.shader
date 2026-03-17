Shader "Unlit/StencilShader"
//用与蒙版测试的shader
//蒙版剔除Shader  StencilShader
{
    Properties
    {
        [IntRange] _Index("Stencil Index",Range(0, 255)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Blend Zero One
            ZWrite Off
            Stencil
            {
                Ref [_Index]
                Comp Always
                Pass Replace
                Fail Keep
            }
        }
    }
}

// | `[IntRange]`     | 属性修饰符，限制为整数范围                                  |
// | `_Index`         | Stencil 参考值（0-255）                            |
// | `Blend Zero One` | 混合模式：不写入颜色（Zero \* Src + One \* Dst = 只保留原颜色|
// | `ZWrite Off`     | 关闭深度写入                                 |
// | `Ref [_Index]`   | 使用属性中的值作为 Stencil 参考值                       |
// | `Comp Always`    | 总是通过 Stencil 测试                                  |
// | `Pass Replace`   | 测试通过时，将 Stencil 值替换为 Ref 值                  |
// | `Fail Keep`      | 测试失败时，保持原 Stencil 值                           |