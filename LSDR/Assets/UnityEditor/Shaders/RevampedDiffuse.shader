Shader "LSDR/RevampedDiffuse" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            // incoming vertices
            struct appdata
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            // data from vert to frag shader
            struct revampedV2F
            {
                fixed4 pos : SV_POSITION;
                half4 color : COLOR0;
                half2 uv_MainTex : TEXCOORD0;
            };

            revampedV2F vert(appdata v)
            {
                revampedV2F output;
                output.pos = UnityObjectToClipPos(v.position);
                output.color = v.color;
                output.uv_MainTex = v.uv;
                
                return output;
            }
            
            sampler2D _MainTex;
            fixed4 _Tint;
            uniform int _TextureSet;
            
            float4 frag(revampedV2F input) : COLOR
            {
                half4 output = tex2D(_MainTex, input.uv_MainTex);
                
                // alpha cutout
                if (output.a <= 0.1) discard;
                
                // apply vertex color
                output *= input.color;
                
                // apply tint
                output *= _Tint;
                
                return output;
            }
            ENDCG
        }
    }
}