Shader "ROI/FreehandMesh" 
{
	Properties 
	{
		_Color("Color", Color) = (0.0117,0.6235,0.898,0.1)
	}

	SubShader 
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
		ZWrite Off	// this is good for transparent cases (I have to test this, for when things are in front, does it still draw?)
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{		 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers flash gles
			 
			#include "UnityCG.cginc"

			uniform float4 _Color;

			struct vertInput 
			{
			    float4 vertex : POSITION;
			};
			 
			struct fragInput 
			{
			    float4 pos : SV_POSITION;
			};
			 
			// Vertex Shader
			fragInput vert (vertInput v)
			{
			    fragInput o;
			    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

			    return o;
			}		

			// Fragment Shader
			float4 frag (fragInput i) : COLOR
			{
				return _Color;
			}
			 
			ENDCG
		 
		}
	}
	 
	Fallback "VertexLit"
}