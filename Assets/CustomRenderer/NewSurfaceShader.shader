// assisted by guides:
// https://roystan.net/articles/toon-shader/
// https://forum.unity.com/threads/how-to-make-unlit-shader-that-casts-shadow.646246/
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
		[Header(Rendering)]
		_Offset("Offset", float) = 0
		[Enum(Off,0,On,1)] _ZWrite("ZWrite", Int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Int) = 4
		[Enum(None,0,Alpha,1,Red,8,Green,4,Blue,2,RGB,14,RGBA,15)] _ColorMask("Color Mask", Int) = 15
	}
	SubShader
	{
		Pass
		{
			Tags
			{
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
				"Queue" = "Geometry"
			}

			LOD 100
			Offset[_Offset],[_Offset]
			ZWrite[_ZWrite]
			ZTest[_ZTest]
			ColorMask[_ColorMask]

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
		// Pass to render object as a shadow caster
		Pass
		{
			Name "ShadowCaster"
			Tags 
			{ 
				"LightMode" = "ShadowCaster"
				"Queue" = "Geometry" 
			}
			LOD 80
			Offset[_Offset],[_Offset]
			ZWrite[_ZWrite]
			ZTest[_ZTest]

			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			#pragma target 2.0
			#pragma multi_compile_shadowcaster


			struct v2fShadow {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2fShadow vertShadow(appdata_base v)
			{
				v2fShadow o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 fragShadow(v2fShadow i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	} Fallback "Diffuse"
}