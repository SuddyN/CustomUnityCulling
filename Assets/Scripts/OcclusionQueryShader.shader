﻿Shader "OcclusionQueryShader"
{
	SubShader
	{
		Cull Off
		Tags 
		{ 
			"RenderType" = "Transparent" 
			"Queue" = "Transparent" 
		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			RWStructuredBuffer<float4> _WriteBuffer : register(u1);
			StructuredBuffer<float4> _ReadBuffer;

			float4 vert(float4 vertex : POSITION, out uint index : TEXCOORD0, uint id : SV_VertexID) : SV_POSITION
			{
				index = _ReadBuffer[id].w; // the index, stored in 4th element of Vector4
				return mul (UNITY_MATRIX_VP, float4(_ReadBuffer[id].xyz, 1.0));
			}

			[earlydepthstencil]
			float4 frag(float4 vertex : SV_POSITION, uint index : TEXCOORD0) : SV_TARGET
			{
				_WriteBuffer[index] = vertex;
				return float4(1.0, 0.0, 0.0, 0.0); // change alpha value to render bounding box
			}
			ENDCG
		}
	}
}