// レーザービーム用：加算合成、視線に対する面の角度で縁が溶ける（フレネル減衰）、
// 長さ方向に流れるエネルギーの縞と、走る光のパケット。HDR 出力でブルームに乗る。
Shader "Gravity/LaserBeam"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.1, 0.05, 1)
        _Intensity ("Intensity (HDR)", Range(0, 12)) = 1
        _Power ("Edge softness", Range(0.2, 8)) = 2
        _FlowAmount ("Flow amount", Range(0, 1)) = 0.35
        _FlowScale ("Flow scale", Float) = 6
        _FlowSpeed ("Flow speed", Float) = 1.5
        _PulsePos ("Pulse position", Range(-1, 2)) = -1
        _PulseWidth ("Pulse width", Float) = 0.06
        _PulseGain ("Pulse gain", Float) = 2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 wnormal : TEXCOORD0;
                float3 wpos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            fixed4 _Color;
            float _Intensity, _Power, _FlowAmount, _FlowScale, _FlowSpeed, _PulsePos, _PulseWidth, _PulseGain;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wnormal = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = abs(dot(normalize(i.wnormal), viewDir));
                float edge = pow(ndv, _Power);

                float flow = 1.0 - _FlowAmount * 0.5 * (1.0 + sin((i.uv.y * _FlowScale - _Time.y * _FlowSpeed) * 6.2831853));
                float d = (i.uv.y - _PulsePos) / max(_PulseWidth, 0.001);
                float pulse = exp(-d * d) * _PulseGain;

                float a = edge * (flow + pulse) * _Intensity * _Color.a;
                return fixed4(_Color.rgb * a, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
