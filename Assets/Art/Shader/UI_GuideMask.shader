Shader "UI/GuideMask"
{
    Properties
    {
        // 镂空区域的中心点坐标 (UV坐标系: 0到1)
        _MaskCenter ("Center", Vector) = (0.5, 0.5, 0, 0)
        // 镂空区域的半径
        _MaskRadius ("Radius", Float) = 0.2
        // 边缘柔和度，值越大边缘越模糊
        _MaskSoftness ("Softness", Float) = 0.01
        // 遮罩颜色，这里默认为半透明黑色
        _Color ("Color", Color) = (0, 0, 0, 0.7)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 声明变量，与Properties块中的名称完全一致
            fixed4 _Color;
            float2 _MaskCenter;
            float _MaskRadius;
            float _MaskSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 计算当前像素点到镂空中心的距离
                float dist = distance(i.uv, _MaskCenter);

                // 2. 使用smoothstep函数生成镂空区域的Alpha值
                //    在 (radius - softness, radius + softness) 之间生成平滑过渡，其余部分为0或1
                float alphaFactor = smoothstep(_MaskRadius - _MaskSoftness, _MaskRadius + _MaskSoftness, dist);

                // 3. 计算最终的Alpha值
                //    在镂空区域内，alphaFactor为0，所以最终alpha也为0，完全透明
                //    在镂空区域外，alphaFactor为1，最终alpha为 _Color.a
                fixed4 finalColor = _Color;
                finalColor.a *= alphaFactor;

                // 4. 如果完全透明，直接丢弃该像素，优化性能
                clip(finalColor.a - 0.001);

                return finalColor;
            }
            ENDCG
        }
    }
}