Shader "Custom/EasyGrass"
{
    Properties
    {
        [HDR]_Tint ("Tint Color", Color) = (1,1,1,1)
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
		_AlphaClip ("Alpha Clip", Range(0,1)) = 0.5

		_SizeScale("Size Scale", Range(0.1, 3)) = 1.0
		_ShowRange("Show Range", Range(50, 200)) = 150
		_WaveSpeed("Wave Speed", Range(0, 10)) = 1.0
        _WaveAmp("Wave Amp", Range(0 ,1)) = 1.0
        //_HeightFactor("Height Factor", Range(1, 100)) = 1.0
		_HeightCutoff("Height Cutoff", Range(0, 1)) = 0.1
        _WindSpeed("Wind Speed", Range(1, 10)) = 1
		[NoScaleOffset] _WindTex("Wind Texture", 2D) = "white" {}
		//_TerrainPosition("Terrain  Position", vector) = (0, 0, 0, 1)
        //_TerrainSize("Terrain  Size", vector) = (1, 1, 1, 1)

    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "DisableBatching" = "True"}
        LOD 100

        Pass
        {
			Name "FORWARD"
			Tags { "LightMode"="ForwardBase" }
			cull back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			//#pragma instancing_options forcemaxcount:513

            #include "UnityCG.cginc"

            struct appdata
            {
				UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
				float3 normal : NORMAL;
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

			sampler2D _WindTex;
            float4 _WindTex_ST;
			//float4 _TerrainPosition;
            //float4 _TerrainSize;
            float _WaveSpeed;
            float _WaveAmp;
            float _HeightFactor;
			float _HeightCutoff;
            float _WindSpeed;
			float4 _LightColor0;
			float _ShowRange;
			float _SizeScale;

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.uv = v.uv;

				//float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				//float2 samplePos = (worldPos.xz - _TerrainPosition.xz)/_TerrainSize.xz;
                //samplePos += _Time.x * _WindSpeed;
                //float windSample = tex2Dlod(_WindTex, float4(samplePos, 0, 0));
				float windSample = _Time.x * _WindSpeed;

				_HeightFactor = 100;
				float4 vertex = v.vertex;
                float heightFactor = v.uv.y > _HeightCutoff;
				heightFactor = heightFactor * pow(v.uv.y, _HeightFactor);
                vertex.z += sin(_WaveSpeed * windSample) * _WaveAmp * heightFactor;
                vertex.x += cos(_WaveSpeed * windSample) * _WaveAmp * heightFactor;

				#if INSTANCING_ON
					float3 eyePos = UnityObjectToViewPos(float4(0.0, 0.0, 0.0, 1.0));
					float4 viewPos = float4(eyePos.xyz, 1.0)
						+ float4(vertex.x * _SizeScale, vertex.y * _SizeScale, 0.0, 0.0);
					o.vertex = mul(UNITY_MATRIX_P, viewPos);
					o.worldPos = mul(UNITY_MATRIX_I_V, viewPos);
				#else
					o.vertex = UnityObjectToClipPos(vertex);
					o.worldPos = mul(unity_ObjectToWorld, vertex);
				#endif

				o.normal = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);

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
				float ndl = dot(i.normal, lightDir) * 0.5 + 0.5;
				float3 finalColor = col.rgb * _LightColor0.rgb * ndl;

				UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
}
