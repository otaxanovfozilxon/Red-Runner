Shader "UI/CircleFade1"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Range(0,2)) = 1.5
        _Smoothness ("Smoothness", Range(0,0.3)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
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

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, _Center.xy);
                float alpha = smoothstep(_Radius - _Smoothness, _Radius, dist); // Bu yerda _Radius + _Smoothness emas, faqat _Radius — yumshoqroq bo'ladi
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}