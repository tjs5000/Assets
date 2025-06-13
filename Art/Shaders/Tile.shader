Shader "Custom/GridTile"
{
    Properties
    {
        _GridColor("Grid Line Color", Color) = (0,0,0,1)
        _FillColor("Tile Fill Color", Color) = (1,1,1,1)
        _GridSize("Grid Cell Size", Float) = 10
        _LineWidth("Line Width", Float) = 1.0

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _GridColor;
        fixed4 _FillColor;
        float _GridSize;
        float _LineWidth;

        half _Glossiness;
        half _Metallic;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 gridUV = IN.uv_MainTex * _GridSize;
            float2 gridLine = abs(frac(gridUV) - 0.5);
            float2 lineWidth = fwidth(gridUV) * _LineWidth;

            // Smooth transition for anti-aliasing the grid line
            float lineFactor = min(
                smoothstep(0.0, lineWidth.x, gridLine.x),
                smoothstep(0.0, lineWidth.y, gridLine.y)
            );

            fixed4 finalColor = lerp(_GridColor, _FillColor, lineFactor);

            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = finalColor.a;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
