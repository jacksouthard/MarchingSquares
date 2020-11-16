// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ArmFadeOutline"
{
	//show values to edit in inspector
	Properties{
		_OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
		_OutlineThickness("Outline Thickness", Range(0,.1)) = 0.03
        
        _BaseAlpha("Base Alpha", Range(0,1)) = 1

		_Color("Tint", Color) = (0, 0, 0, 1)
		_MainTex("Texture", 2D) = "white" {}
	}

		SubShader{
		//the material is completely non-transparent and is rendered at the same time as the other opaque geometry
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
     
         ZWrite On
         Blend SrcAlpha OneMinusSrcAlpha 

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
            
            half _BaseAlpha;

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
			fixed4 frag(v2f i) : SV_TARGET{
				float a = _BaseAlpha;
                a *= i.uv.x * i.uv.x;
                return fixed4(_Color.rgb, a);
			}

			ENDCG
		}

		//The second pass where we render the outlines
		Pass{
			Cull front

			CGPROGRAM

				//include useful shader functions
				#include "UnityCG.cginc"

				//define vertex and fragment shader
				#pragma vertex vert
				#pragma fragment frag

				//color of the outline
				fixed4 _OutlineColor;
			//thickness of the outline
			float _OutlineThickness;
            
            half _BaseAlpha;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

			//the object data that's available to the vertex shader
			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
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
				//calculate the position of the expanded object
				float3 normal = normalize(v.normal);
				float3 outlineOffset = normal * _OutlineThickness;
				float3 position = v.vertex + outlineOffset;
				//convert the vertex positions from object space to clip space so they can be rendered
				o.position = UnityObjectToClipPos(position);
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
			}

			//the fragment shader
			fixed4 frag(v2f i) : SV_TARGET{
				float a = _BaseAlpha;
                a *= i.uv.x * i.uv.x;
                return fixed4(_OutlineColor.rgb, a);
			}

			ENDCG
		}
	}

		//fallback which adds stuff we didn't implement like shadows and meta passes
				FallBack "Standard"
}
