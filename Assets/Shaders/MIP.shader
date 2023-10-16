Shader "Volume Rendering/MIP" 
{
	Properties 
	{
	    _VolumeRed ("Texture Red", 3D) = "" {}
	    _VolumeGreen ("Texture Green", 3D) = "" {}
	    _VolumeBlue ("Texture Blue", 3D) = "" {}
	    _VolumePurple ("Texture Purple", 3D) = "" {}

	    _BoxDim("Box Dim", Vector) = (1, 1, 1, 0)
	    _numSamples("Number of samples", Int) = 10
	    _lod("LOD", Int) = 1

	    _Threshold("Threshold", Range(0.0, 0.5)) = 0.06
	    _Opacity("Opacity", Range(0.0, 1.0)) = 0.05

	    _redOpacity("Red Opacity", Range(0.0, 1.0)) = 1.0
	    _greenOpacity("Green Opacity", Range(0.0, 1.0)) = 1.0
	    _blueOpacity("Blue Opacity", Range(0.0, 1.0)) = 1.0
	    _purpleOpacity("Purple Opacity", Range(0.0, 1.0)) = 1.0

	    _colocalizationMethod("Colocalization Method", Int) = 3
	    _colThresIntervalDisplay("Colocalization Threshold Display", Int) = 0
	    _colChannel0("First Colocalization Channel", Int) = 0
	    _colChannel1("Second Colocalization Channel", Int) = 1
	    _chan0ThresholdHigh("Chan0 Threshold High", Range(0.0, 1.0)) = 0.5
	    _chan1ThresholdHigh("Chan1 Threshold High", Range(0.0, 1.0)) = 0.5
	    _chan0ThresholdLow("Chan0 Threshold Low", Range(0.0, 1.0)) = 0.0
	    _chan1ThresholdLow("Chan1 Threshold Low", Range(0.0, 1.0)) = 0.0
	    _colocalizationOpacity("Colocalization Opacity", Range(0.0, 1.0)) = 1.0
		_maxValue("Max Coloc Value", Range(0.0, 1.0)) = 1.0
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

			sampler3D _VolumeRed;
			sampler3D _VolumeGreen;
			sampler3D _VolumeBlue;
			sampler3D _VolumePurple;

			uniform float4 _BoxDim;
			uniform float _Opacity;
			uniform float _Threshold;

			uniform int _numSamples;	//total samples for each ray march step

			// constants
			static const float3 texMin = float3(0.0, 0.0, 0.0);	//minimum texture access coordinate
			static const float3 texMax = float3(1.0, 1.0, 1.0);	//maximum texture access coordinate

			uniform int _lod;

			uniform float _redOpacity;
			uniform float _greenOpacity;
			uniform float _blueOpacity;
			uniform float _purpleOpacity;

			//colocalization
			uniform int _colocalizationMethod;	// the way the colocalization should be shown
			uniform int _colThresIntervalDisplay;	// draw 0 = above high, 1 = between low and high, 2 = below low
			uniform int _colChannel0;			// the first channel that will be used for colocalization
			uniform int _colChannel1;			// the second channel that will be used for colocalization
			uniform float _chan0ThresholdHigh;		// the high threshold for channel 0 to determine colocalization
			uniform float _chan1ThresholdHigh;		// the high threshold for channel 1 to determine colocalization
			uniform float _chan0ThresholdLow;		// the low threshold for channel 0 to determine colocalization
			uniform float _chan1ThresholdLow;		// the low threshold for channel 1 to determine colocalization
			uniform float _colocalizationOpacity;// the opacity of the colocalized area
			uniform float _maxValue;
			 
			struct vertInput 
			{
			    float4 vertex : POSITION;
			};
			 
			struct fragInput 
			{
			    float4 pos : SV_POSITION;
			    float3 uv : TEXCOORD0;
			    //float3 camPos : TEXCOORD1;
			};
			 
			// Vertex Shader
			fragInput vert (vertInput v)
			{
			    fragInput o;
			    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

			    o.uv = v.vertex.xyz;

			    return o;
			}		

			// Fragment Shader
			float4 frag (fragInput i) : COLOR
			{
				float4 outColor = float4(0.0, 0.0, 0.0, 1.0);
				//float3 boxDim = float3(1.0, 1.0, 1.0);	// TODO: I don't know why it should be like this, I should use _BoxDim

				float3 dirStep = float3(0.0, 0.0, 1.0/_numSamples);// step_size.x; //NOTE: this was step_size  //float3(_BoxDim.x / MAX_SAMPLES, _BoxDim.y / MAX_SAMPLES, _BoxDim.z / MAX_SAMPLES)
				//float3 dataPos = (i.uv + 0.5);
				//float3 dataPos = (i.uv + boxDim/2.0)/boxDim;
				float3 dataPos = float3(i.uv.x+0.5, i.uv.y+0.5, 0.0);

				//flag to indicate if the raymarch loop should terminate
				bool stop = false;


				//[unroll]//(maxNumSamples)]

				for (int s = 0; s < _numSamples; s++) //_numSamples
				{
					// advance ray by dirstep
					dataPos = dataPos + dirStep;

					//stop = dot(sign(dataPos*_BoxDim - texMin), sign(texMax - dataPos*_BoxDim)) < 3.0; //texMax instead of _BoxDim
					stop = dot(sign(dataPos - texMin), sign(texMax - dataPos)) < 3.0; //texMax instead of _BoxDim

					//if the stopping condition is true we brek out of the ray marching loop
					if (stop)
					{
						//outColor = float4(1.0, 1.0, 1.0, 0.1);
						//break;
					}



					//TODO: The 0.5 factor is just a thumbsuck value... really determine what it should be
					float red = tex3Dlod(_VolumeRed, float4(dataPos, _lod)).r*_redOpacity;
					float green = tex3Dlod(_VolumeGreen, float4(dataPos, _lod)).g*_greenOpacity;
					float blue = tex3Dlod(_VolumeBlue, float4(dataPos, _lod)).b*_blueOpacity;
					float purple = tex3Dlod(_VolumePurple, float4(dataPos, _lod)).r*_purpleOpacity;
					float4 sample = float4(red + purple*0.5, green, blue + purple*0.5, 1.0);

					// colocalization code
					float chan0Value = 0.0;
					float chan1Value = 0.0;

										// determine which channels to use
					switch (_colChannel0)
					{
					case 0: chan0Value = red; break;	//TODO: reconsider using _redOpacity
					case 1: chan0Value = green; break;
					case 2: chan0Value = blue; break;
					case 3: chan0Value = purple; break;
					}

					switch (_colChannel1)
					{
					case 0: chan1Value = red; break;	//TODO: reconsider using _redOpacity
					case 1: chan1Value = green; break;
					case 2: chan1Value = blue; break;
					case 3: chan1Value = purple; break;
					}

					bool thresholdCond = false;
					// determine the condition
					switch (_colThresIntervalDisplay)
					{
					case 0: thresholdCond = (chan0Value > _chan0ThresholdHigh && chan1Value > _chan1ThresholdHigh); break;
					case 1: thresholdCond = (chan0Value <= _chan0ThresholdHigh && chan1Value <= _chan1ThresholdHigh && chan0Value >= _chan0ThresholdLow && chan1Value >= _chan1ThresholdLow); break;
					case 2: thresholdCond = (chan0Value < _chan0ThresholdLow && chan1Value < _chan1ThresholdLow); break;
					}

					if (_colocalizationMethod == 0)	//overlay white
					{
						if (thresholdCond)
						{
							sample = float4(1.0, 1.0, 1.0, 1.0);
							//outColor.a = _colocalizationOpacity;
						}
						else 
						{
							//sample = float4(0.0);
						}
					}
					else if (_colocalizationMethod == 1) // show only colocalized area (in colour)
					{
						if (thresholdCond)
						{
							//sample = float4(1.0);
							//outColor.a = _colocalizationOpacity;
						}
						else
						{
							sample = float4(0.0, 0.0, 0.0, 0.0);
							continue;
						}
					}
					else if (_colocalizationMethod == 2) // show only colocalized area (in white)
					{
						if (thresholdCond)
						{
							sample = float4(1.0, 1.0, 1.0, 1.0);
							//outColor.a = _colocalizationOpacity;
						}
						else
						{
							sample = float4(0.0, 0.0, 0.0, 0.0);
							continue;
						}
					}
					else // don't do colocalization
					{
						outColor.a = 1.0;
					}


					//float threshold = 0.0;

					//Uncomment this to add thresholding to MIP again, but it doesn't really make sense to use it
					/*
					if (sample.x < _Threshold && sample.y < _Threshold && sample.z < _Threshold)
					{
						//outColor = float4(sample.xyz, 0.0);
						sample = float4(0.0, 0.0, 0.0, 0.0);
						continue;
						//break;
					}
					*/

					/*
					// this shows only the single colour that's max
					if(red > max(outColor.r, max(green, max(blue, purple))))
						outColor.rgb = float3(red, 0.0, 0.0);
					else if(green > max(outColor.g, max(red, max(blue, purple))))
						outColor.rgb = float3(0.0, green, 0.0);
					else if(blue > max(outColor.b, max(green, max(red, purple))))
						outColor.rgb = float3(0.0, 0.0, blue);
					else if(purple > max(outColor.r, max(green, max(blue, red))))
						outColor.rgb = float3(purple, 0.0, purple);
					*/


					// This is simply the max of everything
					outColor.r = max(outColor.r, sample.r);
					outColor.g = max(outColor.g, sample.g);
					outColor.b = max(outColor.b, sample.b);

				}



				return outColor;
				//return float4(1.0, 1.0, 1.0, 1.0);
			}
			 
			ENDCG
		 
		}
	}
	 
	Fallback "VertexLit"
}