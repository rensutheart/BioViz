Shader "Volume Rendering/Texture Slicing Together (incomplete)" 
{
	Properties 
	{
	    _VolumeRed ("Texture Red", 3D) = "" {}
	    _VolumeGreen ("Texture Green", 3D) = "" {}
	    _VolumeBlue ("Texture Blue", 3D) = "" {}
	    _VolumePurple ("Texture Purple", 3D) = "" {}

	    _BoxDim("Box Dim", Vector) = (1, 1, 1, 0)
	    _Threshold("Threhold", Range(0.0, 0.1)) = 0.06
	    _Opacity("Opacity", Range(0.0, 0.1)) = 0.05

	    _redOpacity("Red Opacity", Range(0.0, 1.0)) = 1.0
	    _greenOpacity("Green Opacity", Range(0.0, 1.0)) = 1.0
	    _blueOpacity("Blue Opacity", Range(0.0, 1.0)) = 1.0
	    _purpleOpacity("Purple Opacity", Range(0.0, 1.0)) = 1.0

	    _colocalizationMethod("Colocalization Method", Int) = 1
	    _colThresholdDisplay("Colocalization Threshold Display", Int) = 0
	    _colChannel0("First Colocalization Channel", Int) = 0
	    _colChannel1("Second Colocalization Channel", Int) = 1
	    _chan0ThresholdHigh("Chan0 Threshold High", Float) = 0.5
	    _chan1ThresholdHigh("Chan1 Threshold High", Float) = 0.5
	    _chan0ThresholdLow("Chan0 Threshold Low", Float) = 0.0
	    _chan1ThresholdLow("Chan1 Threshold Low", Float) = 0.0
	    _colocalizationOpacity("Colocalization Opacity", Float) = 1.0
	}

	SubShader 
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
		ZWrite Off	// this is good for transparent cases (I have to test this, for when things are in front, does it still draw?)

		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{		 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers flash gles
			 
			#include "UnityCG.cginc"

			sampler3D _Volume;
			uniform float4 _BoxDim;
			uniform float _Opacity;
			uniform float _Threshold;

			uniform float _redOpacity;
			uniform float _greenOpacity;
			uniform float _blueOpacity;
			uniform float _purpleOpacity;

			//colocalization
			uniform int _colocalizationMethod;	// the way the colocalization should be shown
			uniform int _colThresholdDisplay;	// draw 0 = above high, 1 = between low and high, 2 = below low
			uniform int _colChannel0;			// the first channel that will be used for colocalization
			uniform int _colChannel1;			// the second channel that will be used for colocalization
			uniform float _chan0ThresholdHigh;		// the high threshold for channel 0 to determine colocalization
			uniform float _chan1ThresholdHigh;		// the high threshold for channel 1 to determine colocalization
			uniform float _chan0ThresholdLow;		// the low threshold for channel 0 to determine colocalization
			uniform float _chan1ThresholdLow;		// the low threshold for channel 1 to determine colocalization
			uniform float _colocalizationOpacity;// the opacity of the colocalized area
			 
			struct vertInput 
			{
			    float4 vertex : POSITION;
			};
			 
			struct fragInput 
			{
			    float4 pos : SV_POSITION;
			    float3 uv : TEXCOORD0;
			};
			 
			// Vertex Shader
			fragInput vert (vertInput v)
			{
			    fragInput o;
			    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

			    float w = _BoxDim.x;
			    float h = _BoxDim.y;
			    float d = _BoxDim.z;

			    o.uv = float3((v.vertex.x + w / 2.0) / w, 1.0 - (v.vertex.y + h / 2.0) / h, (v.vertex.z + d / 2.0) / d);

			    return o;
			}		

			// Fragment Shader
			float4 frag (fragInput i) : COLOR
			{
				float4 outColor;

				float3 texColor = tex3D(_Volume, i.uv).xyz;
	
				//float threshold = 0.06;
				if (texColor.x < _Threshold && texColor.y < _Threshold && texColor.z < _Threshold)
					outColor = float4(0.0, 0.0, 0.0, 0.0);
				else
					outColor = float4(texColor, _Opacity);

			    return outColor;
			}
			 
			ENDCG
		 
		}
	}
	 
	Fallback "VertexLit"
}