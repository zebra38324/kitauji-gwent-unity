Shader "Unlit/RoundedRect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Radius ("Radius", float) = 0
		_Ratio ("Height/Width", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float _Radius;
			float _Ratio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
			    float2 p = abs(step(0.5, i.uv) - i.uv);
                fixed4 col = fixed4(0, 0, 0, 0);
                if (step(_Radius, p.x) || step(_Radius, p.y * _Ratio) || step(length(float2(p.x - _Radius, p.y * _Ratio - _Radius)), _Radius)) {
                    col = tex2D(_MainTex, i.uv);
                } else {
                    discard;
                }
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
