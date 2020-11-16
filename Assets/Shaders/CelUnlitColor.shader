Shader "Custom/CelUnlitColor" {
    Properties {
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader {
        Tags {
            "RenderType" = "Opaque"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf CelShadingForward nolightmap
        #pragma target 3.0

        half4 LightingCelShadingForward(SurfaceOutput s, half3 lightDir, half atten) {
            half NdotL = dot(s.Normal, lightDir);
            if (NdotL <= 0.5) NdotL = 0.5;
            //else if (NdotL <= 0.5) NdotL = 0.5;
            else NdotL = 1;
            
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten * 2);
            //c.rgb = half4(0,0,0);
            c.a = s.Alpha;
            return c;
        }
        
        fixed4 _Color;

        struct Input {
            half fog;
        };
       
        void surf(Input IN, inout SurfaceOutput o) {
            o.Albedo = _Color;
            o.Alpha = 1;  
        }
        ENDCG
    }
    FallBack "Diffuse"
}