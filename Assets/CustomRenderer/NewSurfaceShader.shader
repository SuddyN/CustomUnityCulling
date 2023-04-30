// assisted by guide at https://roystan.net/articles/toon-shader/
Shader "CustomRendererShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base Texture", 2D) = "white" {}
		[HDR]
		_AmbientColor("Ambient", Color) = (0.4,0.4,0.4,1)
		[HDR]
		_SpecularColor("Specular", Color) = (0.9,0.9,0.9,1)
	}
	SubShader
	{
		Pass
		{
			Tags
			{
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase // Compile multiple versions of this shader depending on lighting settings.

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct IN
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct VertToFrag
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				SHADOW_COORDS(2) // Declares a vector4 into TEXCOORD2 semantic with varying precision depending on platform target
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			VertToFrag vert(IN v)
			{
				VertToFrag v2f;
				v2f.pos = UnityObjectToClipPos(v.vertex);
				v2f.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
				v2f.viewDir = normalize(WorldSpaceViewDir(v.vertex));
				v2f.uv = TRANSFORM_TEX(v.uv, _MainTex);
				TRANSFER_SHADOW(v2f) // Assigns shadow coordinate by transforming the vertex from world to shadow-map space
				return v2f;
			}

			float4 _Color;
			float4 _AmbientColor;
			float4 _SpecularColor;

			float4 frag(VertToFrag v2f) : SV_Target
			{
				// NOTE: _WorldSpaceLightPos0 is a vector pointing opposite the main directional light

				// Calculate illumination
				float NdotL = dot(_WorldSpaceLightPos0, v2f.worldNormal);

				// Smoothly interpolate light intensity between light and dark
				float lightIntensity = smoothstep(0, 0.01, NdotL * SHADOW_ATTENUATION(v2f)) / 20;

				// Calculate specular reflection.
				float3 halfVector = normalize(_WorldSpaceLightPos0 + v2f.viewDir);
				float specularIntensity = dot(v2f.worldNormal, halfVector) * lightIntensity;
				float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
				float lighting = (lightIntensity * _LightColor0 + _AmbientColor + specularIntensitySmooth * _SpecularColor);
				return lighting * _Color * tex2D(_MainTex, v2f.uv);
			}
			ENDCG
		}
	} Fallback "Diffuse"
}