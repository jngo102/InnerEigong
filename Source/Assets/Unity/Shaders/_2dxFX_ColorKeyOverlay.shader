Shader "2DxFX/Standard/ColorKeyOverlay"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _ColorKey ("Color Key", Color) = (0, 1, 0, 1)
        _Tolerance ("Tolerance", Range(0, 1)) = 0.125
        _Smoothing ("Edge Smoothing", Range(0, 1)) = 0.1
        _OverlayTex ("Overlay Texture", 2D) = "black" {}
        _OverlayScale ("Overlay Scale", float) = 1

    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"="Transparent"
            "PreviewType" = "Sprite"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 color: COLOR;
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            struct v2f
            {
                fixed4 color: COLOR;
                float2 uv: TEXCOORD0;
                float2 position: TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ColorKey;
            sampler2D _OverlayTex;
            float4 _OverlayTex_ST;
            float _OverlayScale;
            float _Tolerance;
            float _Smoothing;

            v2f vertex(appdata appData)
            {
                v2f output;
                output.color = appData.color;
                output.vertex = UnityObjectToClipPos(appData.vertex);
                output.position = ComputeScreenPos(output.vertex);
                output.uv = TRANSFORM_TEX(appData.uv, _MainTex);
                return output;
            }

            fixed4 fragment(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                fixed4 overlay = tex2D(_OverlayTex, input.position / _OverlayScale);

                float diff = distance(color.rgb, _ColorKey.rgb);
                float blend = smoothstep(_Tolerance, _Tolerance + _Smoothing, diff);

                fixed4 result = lerp(overlay, color, blend);
                result.a = color.a;
                return result;
            }
            ENDCG
        }
    }
}