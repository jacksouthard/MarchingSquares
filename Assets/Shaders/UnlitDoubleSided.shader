Shader "Custom/UnlitDoubleSided"
{
    Properties
    {
		_Color("Color", Color) = (1, 1, 1, 1)
	}
    SubShader
    {
		Tags {"Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Opaque"}
		Cull off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float3 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half3 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				//o.uv.xy = TRANSFORM_TEX(v.uv, _Texture);
				//o.uv.z = v.uv.z;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

		   fixed4 _Color;

			fixed4 frag(v2f i) : SV_Target
			{				
				UNITY_APPLY_FOG(i.fogCoord, _Color.rgb);
				return _Color;
			}
			ENDCG
		}
	}
		//Fallback "Diffuse"
}
