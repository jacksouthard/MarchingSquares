// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/UnlitMaterialize"
{
	//show values to edit in inspector
	Properties{
		_Color("Tint", Color) = (0, 0, 0, 1)
		_MainTex("Texture", 2D) = "white" {}
		_Visibility("Visibility Ratio", Range(0,1)) = 0.5
	}

		SubShader{
			//the material is completely non-transparent and is rendered at the same time as the other opaque geometry
			Tags{ "RenderType" = "Opaque" "Queue" = "Geometry"}

			//The first pass where we render the Object itself
			Pass{
				CGPROGRAM

				//include useful shader functions
				#include "UnityCG.cginc"

				//define vertex and fragment shader
				#pragma vertex vert
				#pragma fragment frag

				//texture and transforms of the texture
				sampler2D _MainTex;
				float4 _MainTex_ST;

				//tint of the texture
				fixed4 _Color;

				fixed _Visibility;

				//the object data that's put into the vertex shader
				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				//the data that's used to generate fragments and can be read by the fragment shader
				struct v2f {
					float4 position : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				//the vertex shader
				v2f vert(appdata v) {
					v2f o;
					//convert the vertex positions from object space to clip space so they can be rendered
					o.position = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				//the fragment shader
				fixed4 frag(v2f i) : SV_TARGET {
					float a = 1 - tex2D(_MainTex, i.uv).a;
					if (a > _Visibility) {
						discard;
					}
					return _Color;
				}

				ENDCG
			}
		}
}
