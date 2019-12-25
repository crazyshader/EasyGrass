Shader "Custom/EasyGrass"
{
    Properties
    {
        [HDR]_Tint ("Tint Color", Color) = (1,1,1,1)
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
		_AlphaClip ("AlphaClip", Range(0,1)) = 0.5
		_SizeScale("Size Scale", Range(0.1, 3)) = 1.0
		_ShowRange("Show Range", Range(50, 200)) = 150
		_WaveSpeed("Wave Speed", Range(0, 10)) = 1.0
        _WaveAmp("Wave Amp", Range(0 ,1)) = 1.0
		_HeightCutoff("Height Cutoff", Range(0, 1)) = 0.1
        _WindSpeed("Wind Speed", Range(0 ,10)) = 5
		//_WorldPosition("World Position", vector) = (0, 0, 0, 1)
        //_WorldSize("World Size", vector) = (1, 1, 1, 1)
        _SnowBlendWeight("Snow Blend Weight", Range(0, 3)) = 1.1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" "DisableBatching"="True"}
        LOD 200
		ColorMask RGB

        Pass
        {
			Name "FORWARD"
			Tags { "LightMode"="ForwardBase" }
			Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			//#pragma instancing_options forcemaxcount:512
			 #pragma instancing_options assumeuniformscaling

            #include "UnityCG.cginc"

            struct appdata
            {
				UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
				UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 uv : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
				float3 normal : NORMAL;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			half _AlphaClip;
			half4 _Tint;
			//float4 _WorldPosition;
            //float4 _WorldSize;
            float _WaveSpeed;
            float _WaveAmp;
			float _HeightCutoff;
            float _WindSpeed;
			float4 _LightColor0;
			float _ShowRange;
			float _SizeScale;
            float _SnowStrength;
            float _SnowBlendWeight;

			inline float Remap (float value, float from1, float to1, float from2, float to2) 
			{
				return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
			}

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				float heightFactor = v.uv.y > _HeightCutoff;
				heightFactor = heightFactor * pow(v.uv.y, 100);
				float4 normal = heightFactor > 0 ? float4(0, 1, 0, 0) : float4(0, 0, 1, 0);
				o.normal = normalize(mul(normal, unity_WorldToObject).xyz);

				//float2 samplePos = (o.worldPos.xz - _WorldPosition.xz)/_WorldSize.xz;
                //samplePos += _Time.x * _WindSpeed;
				float2 samplePos = _Time.x * _WindSpeed; 
                v.vertex.z += sin(_WaveSpeed * samplePos.y) * _WaveAmp * heightFactor;
                v.vertex.x += cos(_WaveSpeed * samplePos.x) * _WaveAmp * heightFactor;

				#if INSTANCING_ON
					float3 eyePos = UnityObjectToViewPos(float4(0.0, 0.0, 0.0, 1.0));
					float4 viewPos = float4(eyePos.xyz, 1.0)
						+ float4(v.vertex.x * _SizeScale, v.vertex.y * _SizeScale, 0.0, 0.0);
					o.vertex = mul(UNITY_MATRIX_P, viewPos);
					o.worldPos = mul(UNITY_MATRIX_I_V, viewPos);
				#else
					o.vertex = UnityObjectToClipPos(v.vertex);
				#endif

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                fixed4 col = tex2D(_MainTex, uv) * _Tint;
				float dist = length(i.worldPos - _WorldSpaceCameraPos);
				float fade = smoothstep(_ShowRange * 0.5, _ShowRange * 0.8, dist);
				clip(col.a - lerp(_AlphaClip, 1.0, fade));

				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float ndl = Remap(dot(i.normal, lightDir) * 0.5 + 0.5, 0, 1, 0.25, 1);
				float3 finalColor = col.rgb * _LightColor0.rgb * ndl;
                finalColor = lerp(finalColor, float3(1, 1, 1), clamp(i.uv.y * _SnowBlendWeight * _SnowStrength, 0, 1));              

				UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return float4(finalColor, 1);
            }
            ENDCG
        }
    }

	Fallback Off
}
