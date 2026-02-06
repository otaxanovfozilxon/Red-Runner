Shader "UI/CircleFade"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Range(0, 2.0)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Aspect Ratio Correction
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = i.uv - _Center.xy;
                uv.x *= aspect;
                
                float dist = length(uv);
                
                // Alpha = 1 when dist > radius (black)
                // Alpha = 0 when dist < radius (transparent)
                // Smooth transition
                float alpha = smoothstep(_Radius, _Radius + _Smoothness, dist);
                
                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
