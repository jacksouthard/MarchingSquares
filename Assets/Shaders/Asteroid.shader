// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Asteroid" {
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
		[NoScaleOffset] _Ramp("Shading Ramp", 2D) = "ramp" {}
		_NormalMapScale("Normal Map Scale", Range(0,10)) = 1
		//_RimIntensity("Rim Intensity", Range(0,1)) = 1
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 100

		Pass
		{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// flip UVs horizontally to correct for back side projection
			#define TRIPLANAR_CORRECT_PROJECTED_U

			// offset UVs to prevent obvious mirroring
			// #define TRIPLANAR_UV_OFFSET

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				//float3 viewDir : TEXCOORD2;
			};

			sampler2D _BumpMap;
			sampler2D _Ramp;

			fixed4 _LightColor0;
			
			fixed4 _Color;
			fixed _NormalMapScale;
			//half _RimIntensity;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = v.vertex;
				o.worldNormal = v.normal;
				//o.viewDir = WorldSpaceViewDir(v.vertex);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// calculate triplanar blend
				half3 triblend = saturate(pow(i.worldNormal, 2)); // 2 was 4
				triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

				// preview blend
				//return fixed4(triblend.xyz, 1);

				// calculate triplanar uvs
				// applying texture scale and offset values ala TRANSFORM_TEX macro
				float2 uvX = i.worldPos.zy * _NormalMapScale;
				float2 uvY = i.worldPos.xz * _NormalMapScale;
				float2 uvZ = i.worldPos.xy * _NormalMapScale;

				// offset UVs to prevent obvious mirroring
			#if defined(TRIPLANAR_UV_OFFSET)
				uvY += 0.33;
				uvZ += 0.67;
			#endif

				// minor optimization of sign(). prevents return value of 0
				half3 axisSign = i.worldNormal < 0 ? -1 : 1;

				// flip UVs horizontally to correct for back side projection
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				uvX.x *= axisSign.x;
				uvY.x *= axisSign.y;
				uvZ.x *= -axisSign.z;
			#endif

				// tangent space normal maps
				half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
				half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
				half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

				// flip normal maps' x axis to account for flipped UVs
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				tnormalX.x *= axisSign.x;
				tnormalY.x *= axisSign.y;
				tnormalZ.x *= -axisSign.z;
			#endif

				// swizzle world normals to match tangent space and apply ala UDN normal blending
				// these should get normalized, but it's very a minor visual difference to skip it
				tnormalX = half3(tnormalX.xy + i.worldNormal.zy, i.worldNormal.x);
				tnormalY = half3(tnormalY.xy + i.worldNormal.xz, i.worldNormal.y);
				tnormalZ = half3(tnormalZ.xy + i.worldNormal.xy, i.worldNormal.z);

				// swizzle tangent normals to match world normal and blend together
				half3 worldNormal = normalize(
					tnormalX.zyx * triblend.x +
					tnormalY.xzy * triblend.y +
					tnormalZ.xyz * triblend.z
					);

				// convert the normal from local space to world space
				worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, worldNormal));

				// calculate lighting
				//float4 rimDot = 1 - dot(i.viewDir, worldNormal);
				//float rim = saturate(rimDot * _RimIntensity);
				half ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));// +rim);
				//half3 ambient = ShadeSH9(half4(worldNormal, 1));
				ndotl = tex2D(_Ramp, ndotl).r;
				//if (ndotl < 0.33) ndotl = 0.33;
				//else if (ndotl < 0.66) ndotl = 0.66;
				//else ndotl = 1;

				half3 lighting = _LightColor0.rgb * ndotl;// +ambient;

				// preview directional lighting
				//return fixed4(ndotl.xxx, 1);

				return fixed4(_Color * lighting, 1);
			}
			ENDCG
		}

		// LIGHT ADD
		Pass
		{
			Tags {"LightMode" = "ForwardAdd"}
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			// flip UVs horizontally to correct for back side projection
			#define TRIPLANAR_CORRECT_PROJECTED_U

			// offset UVs to prevent obvious mirroring
			// #define TRIPLANAR_UV_OFFSET

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				//float3 viewDir : TEXCOORD2;
				float3  lightDir  : TEXCOORD2;
				LIGHTING_COORDS(3, 4)
			};

			sampler2D _BumpMap;

			fixed4 _Color;
			fixed _NormalMapScale;
			//half _RimIntensity;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = v.vertex;
				o.worldNormal = v.normal;
				//o.viewDir = WorldSpaceViewDir(v.vertex);

				o.lightDir = ObjSpaceLightDir(v.vertex);
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// calculate triplanar blend
				half3 triblend = saturate(pow(i.worldNormal, 2)); // 2 was 4
				triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

				// preview blend
				//return fixed4(triblend.xyz, 1);

				// calculate triplanar uvs
				// applying texture scale and offset values ala TRANSFORM_TEX macro
				float2 uvX = i.worldPos.zy * _NormalMapScale;
				float2 uvY = i.worldPos.xz * _NormalMapScale;
				float2 uvZ = i.worldPos.xy * _NormalMapScale;

				// offset UVs to prevent obvious mirroring
			#if defined(TRIPLANAR_UV_OFFSET)
				uvY += 0.33;
				uvZ += 0.67;
			#endif

				// minor optimization of sign(). prevents return value of 0
				half3 axisSign = i.worldNormal < 0 ? -1 : 1;

				// flip UVs horizontally to correct for back side projection
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				uvX.x *= axisSign.x;
				uvY.x *= axisSign.y;
				uvZ.x *= -axisSign.z;
			#endif
            
				// tangent space normal maps
				half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
				half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
				half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

				// flip normal maps' x axis to account for flipped UVs
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				tnormalX.x *= axisSign.x;
				tnormalY.x *= axisSign.y;
				tnormalZ.x *= -axisSign.z;
			#endif

				// swizzle world normals to match tangent space and apply ala UDN normal blending
				// these should get normalized, but it's very a minor visual difference to skip it
				tnormalX = half3(tnormalX.xy + i.worldNormal.zy, i.worldNormal.x);
				tnormalY = half3(tnormalY.xy + i.worldNormal.xz, i.worldNormal.y);
				tnormalZ = half3(tnormalZ.xy + i.worldNormal.xy, i.worldNormal.z);

				// swizzle tangent normals to match world normal and blend together
				half3 worldNormal = normalize(
					tnormalX.zyx * triblend.x +
					tnormalY.xzy * triblend.y +
					tnormalZ.xyz * triblend.z
					);

				// convert the normal from local space to world space
				//worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, worldNormal));

				// vertex lighting
				fixed atten = LIGHT_ATTENUATION(i);

				i.lightDir = normalize(i.lightDir);

				fixed diff = saturate(dot(worldNormal, i.lightDir));
				//if (diff > 0) diff = 1;
				//else diff = 0.5;

				fixed4 c;
				//c.rgb = UNITY_LIGHTMODEL_AMBIENT.rgb * 2 * _Color.rgb;
				c.rgb = (_Color.rgb * _LightColor0.rgb * diff) * (atten * 2);
				c.a = 1;// _LightColor0.a * atten;
				return c;
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
}