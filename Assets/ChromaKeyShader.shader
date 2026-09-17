Shader "Custom/ChromaKeyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,1)) = 0.4
        [Toggle] _RemoveWhite ("Remove white background", Float) = 0
        _WhiteTolerance ("White background tolerance", Range(0.01,0.3)) = 0.08
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            float _RemoveWhite;
            float _WhiteTolerance;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float green = col.g;
                float red = col.r;
                float blue = col.b;

                if (green > _Threshold && green > red * 1.2 && green > blue * 1.2)
                {
                    col.a = 0;
                }

                if (_RemoveWhite > 0.5)
                {
                    float distanceFromWhite = max(1.0 - red, max(1.0 - green, 1.0 - blue));
                    col.a *= smoothstep(_WhiteTolerance * 0.5, _WhiteTolerance, distanceFromWhite);
                }

                return col;
            }

            ENDCG
        }
    }
}
