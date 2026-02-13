// WebGL-compatible UI background blur replacement
// GrabPass is not supported in WebGL, so we use a semi-transparent dark overlay instead
// Note: The Blur.mat has _Color=(1,1,1,1) baked in from the old shader, so we ignore
// the material _Color and use a hardcoded dark tint to let the game show through.

Shader "Unlit/UI_BGBlur"
{
	Properties
	{
		[HideInInspector] _MainTex ("Texture", 2D) = "white" {}
		_Color ("Tint Color", Color) = (0, 0, 0, 0.5)
		_Size ("Size", float) = 1
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Semi-transparent dark overlay — lets the game world show through
				return fixed4(0, 0, 0, 0.5);
			}
			ENDCG
		}
	}
	Fallback "UI/Default"
}
