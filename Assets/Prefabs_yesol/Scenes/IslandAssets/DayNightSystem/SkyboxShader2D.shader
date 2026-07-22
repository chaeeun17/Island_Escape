Shader "Skybox/NightDay 2D"
{
    Properties
    {
        _Texture1("Texture1", 2D) = "white" {}
        _Texture2("Texture2", 2D) = "white" {}
        _Blend("Blend", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID // [VR 필수] 버텍스 입력 인스턴스 ID
            };

            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO // [VR 필수] 픽셀 셰이더로 전달할 타겟 눈 인덱스
            };

            sampler2D _Texture1;
            sampler2D _Texture2;
            float _Blend;

            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v); // [VR 필수] 인스턴스 ID 초기화
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // [VR 필수] 출력 스트럭처에 눈 인덱스 할당

                o.texcoord = v.vertex.xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                const float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // [VR 필수] 픽셀 셰이더에서 현재 눈 인덱스 연산 적용

                float2 tc = ToRadialCoords(i.texcoord);
                fixed4 tex1 = tex2D(_Texture1, tc);
                fixed4 tex2 = tex2D(_Texture2, tc);
                return lerp(tex1, tex2, _Blend);
            }
            ENDCG
        }
    }
}
/*
Shader "Skybox/NightDay 2D"
{
    Properties
    {
        _Texture1("Texture1", 2D) = "white" {}
        _Texture2("Texture2", 2D) = "white" {}
        _Blend("Blend", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Texture1;
            sampler2D _Texture2;
            float _Blend;

            v2f vert(appdata v)
            {
                v2f o;
                o.texcoord = v.vertex.xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                const float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tc = ToRadialCoords(i.texcoord);
                fixed4 tex1 = tex2D(_Texture1, tc);
                fixed4 tex2 = tex2D(_Texture2, tc);
                return lerp(tex1, tex2, _Blend);
            }
            ENDCG
        }
    }
}
*/