Shader "Volume Rendering/Ray Casting" 
{
	Properties 
	{
	    _VolumeRed ("Texture Red", 3D) = "" {}
	    _VolumeGreen ("Texture Green", 3D) = "" {}
	    _VolumeBlue ("Texture Blue", 3D) = "" {}
	    _VolumePurple ("Texture Purple", 3D) = "" {}

	    _BoxDim("Box Dim", Vector) = (1, 1, 1, 0)
		_ROI_P1("ROI Point 1", Vector) = (0, 0, 0, 0)
	    _ROI_P2("ROI Point 2", Vector) = (1, 1, 1, 0)

		_ROIMask3D("ROI Mask 3D", 3D) = "" {}

		_ROI_XY("ROI XY", 2D) = "" {}
		_ROI_XZ("ROI XZ", 2D) = "" {}
		_ROI_YZ("ROI YZ", 2D) = "" {}

	   	_camPosLeft("Camera Position Left", Vector) = (0, 0, 0, 0)
	   	_camPosRight("Camera Position Right", Vector) = (0, 0, 0, 0)
	   	_currentEye("The current eye", Int) = 0
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

		_ch0Average("Channel 0 average", Range(0.0, 1.0)) = 1.0
		_ch1Average("Channel 1 average", Range(0.0, 1.0)) = 1.0
		_ch0Max("Channel 0 Max", Range(0.0, 1.0)) = 1.0
		_ch1Max("Channel 1 Max", Range(0.0, 1.0)) = 1.0
		_MOC_denom("MOC Denominator", float) = 0.0
		_PCC_denom("PCC Denominator", float) = 0.0

		_p1("Line P1", Vector) = (0, 0, 0, 0)
		_p2("Line P2", Vector) = (1, 1, 0, 0)
		_distThresh("Distance Threshold", Range(1.0, 255.0)) = 1.0
		_angle("Distance Angle", Range(1.0, 89.0)) = 60.0

		_nmdp_minMultFactor("nMDP minimum multiplication", float) = 1.0
		_nmdp_maxMultFactor("nMDP maximum multiplication", float) = 1.0

		_useROIMask("Whether the ROI Mask must be shown", Range(0.0, 1.0)) = 0.0
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

			//uniform float4 _camPos;		//camera position
			//uniform float3 step_size;	//ray step size 
			uniform int _numSamples;	//total samples for each ray march step
			uniform float4 _camPosLeft;
			uniform float4 _camPosRight;
			uniform int _currentEye;
			//constants
			// TODO: this is harcoded here
			static const int maxNumSamples = 1000;
			static const float3 texMin = float3(0.0, 0.0, 0.0);	//minimum texture access coordinate
			static const float3 texMax = float3(1.0, 1.0, 1.0);	//maximum texture access coordinate

			uniform int _lod;

			sampler3D _ROIMask3D;

			sampler2D _ROI_XY;
			sampler2D _ROI_XZ;
			sampler2D _ROI_YZ;

			uniform float4 _ROI_P1;
			uniform float4 _ROI_P2;

			uniform float4 _p1;
			uniform float4 _p2;
			
			uniform float _distThresh;
			uniform float _angle;

			uniform float _redOpacity;
			uniform float _greenOpacity;
			uniform float _blueOpacity;
			uniform float _purpleOpacity;

						uniform float _redOnOff;
			uniform float _greenOnOff;
			uniform float _blueOnOff;
			uniform float _purpleOnOff;

			uniform float _useROIMask;

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

			uniform float _ch0Average;
			uniform float _ch1Average;
			uniform float _ch0Max;
			uniform float _ch1Max;
			uniform float _MOC_denom;
			uniform float _PCC_denom;

			uniform float _nmdp_minMultFactor;
			uniform float _nmdp_maxMultFactor;
			 
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

			float3 HeatMapColorRainbow(float val, float min, float max)
			{
				//float v = (float)(val - min) / (float)(max-min);
				float diff = max - min;
				float diffFraction = diff / 7.0;
				float positions[8] = { min, diffFraction + min, diffFraction * 2 + min, diffFraction * 3 + min, diffFraction * 4 + min, diffFraction * 5 + min, diffFraction * 6 + min, diffFraction * 7 + min }; //  the positions at which colors change

																																													  // first interval (blue increases)
				if (val > positions[0] && val < positions[1])
				{
					float v = ((1.0)*val - positions[0]) / ((1.0)*positions[1] - positions[0]);
					return float3(0.0, 0.0, v);
				}

				// second interval (blue constant, green grows)
				if (val > positions[1] && val < positions[2])
				{
					float v = ((1.0)*val - positions[1]) / ((1.0)*positions[2] - positions[1]);
					return float3(0.0, v, 1.0);
				}

				// third interval (blue decrease, green constant)
				if (val > positions[2] && val < positions[3])
				{
					float v = ((1.0)*val - positions[2]) / ((1.0)*positions[3] - positions[2]);
					return float3(0.0, 1.0, (1.0 - v));
				}

				// fourth interval (red increases, green constant)
				if (val > positions[3] && val < positions[4])
				{
					float v = ((1.0)*val - positions[3]) / ((1.0)*positions[4] - positions[3]);
					return float3(v, 1.0, 0.0);
				}

				// fifth interval (red constnat, green decrease)
				if (val > positions[4] && val < positions[5])
				{
					float v = ((1.0)*val - positions[4]) / ((1.0)*positions[5] - positions[4]);
					return float3(1.0, (1.0 - v), 0.0);
				}

				// sixth interval (red constant, blue increases)
				if (val > positions[5] && val < positions[6])
				{
					float v = ((1.0)*val - positions[5]) / ((1.0)*positions[6] - positions[5]);
					return float3(1.0, 0.0, v);
				}

				// seventh interval (red constant, blue constant, green increases to white)
				if (val > positions[6] && val < positions[7])
				{
					float v = ((1.0)*val - positions[6]) / ((1.0)*positions[7] - positions[6]);
					return float3(1.0, v, 1.0);
				}

				if (val > max)
					return float3(1.0, 1.0, 1.0);

				return float3(0.0, 0.0, 0.0);
			}

			
			float3 HeatMapColorNMDP(float val, float min, float max)
			{
				/*
				if (val > max)
					return float3(1.0, 1.0, 1.0);
				else if(-val > min)
					return float3(0.0, 1.0, 0.0);
					*/																																								 
				if (val > 0)
				{
					if(val < max/3.0)
					{
						float v = ((1.0)*val - max/3.0) / (max/3.0 - 0.0);
						return float3(v, 0.0, 0.0);
					}
					else if(val < max*2.0/3.0)
					{
						float v = ((1.0)*(val) - max*2.0/3.0) / ((1.0)*max*2.0/3.0 -max/3.0);
						return float3(1.0, v, 0.0);
					}
					else
					{
						float v = ((1.0)*(val) - max) / ((1.0)*max -max*2.0/3.0);
						return float3(1.0, 1.0, v);
					}
					
				}
				else
				{
				//return float3(0.0, 0.0, 0.0);
					if(-val < min/2.0)
					{
						float v = (-(1.0)*val - min/2.0) / (min/2.0 - 0.0);
						return float3(0.0, 1.0-v, 1.0);
					}
					else
					{
						float v = (-(1.0)*(val) - min) / ((1.0)*min -min/2.0);
						return float3(0.0, 0, 1.0-v);
					}
				}

				
				return float3(0.0, 0.0, 0.0);
			}
			
			float3 getGradientColocHMWithDist(float ch0Val, float ch1Val, float max)
			{
				//https://en.wikipedia.org/wiki/Vector_projection
				float2 p1 = float2(_p1.x, _p1.y);//float2(_chan0ThresholdHigh, _chan1ThresholdHigh);
				float2 p2 = float2(_p2.x, _p2.y);//float2(max, max);
				float2 p3 = float2(ch0Val, ch1Val);

				float k = ((p2.y - p1.y) * (p3.x - p1.x) - (p2.x - p1.x) * (p3.y - p1.y)) / ((p2.y - p1.y) * (p2.y - p1.y) + (p2.x - p1.x) *(p2.x - p1.x));
				float result = p3.x - k * (p2.y - p1.y);


				// in case the point is above the line make it the max
				float m = (p2.y-p1.y)/(p2.x-p1.x);
				float c = p2.y - m*p2.x;
				float inverseC = p2.y + 1/m*p2.x;

				float value = 0;
				if(p3.y < (-1/m)*p3.x + inverseC)
					value = ((result - p1.x) / (p2.x - p1.x));
				else
					value = 1;
				
				if(value > 1)
					value = 1;
				

				//http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
				float dis = (abs((p2.y-p1.y)*p3.x - (p2.x-p1.x)*p3.y + p2.x*p1.y - p2.y*p1.x))/sqrt((p2.y-p1.y)*(p2.y-p1.y) + (p2.x-p1.x)*(p2.x-p1.x))*255;
				if(dis <_distThresh)
					value = value*255 - dis*tan(_angle * 3.14159 / 180);
				else
					value = 0;

				if(value < 0)
					value = 0;

				return HeatMapColorRainbow(value, 0, max*255);
			}

			//nMDP
			float3 getGradientColocAverageHM(float ch0Val, float ch1Val, float max)
			{
			
			
				// this is using the https://sites.google.com/site/colocalizationcolormap/home formula
				float value = (((ch0Val - _ch0Average)*(ch1Val - _ch1Average)) / ((_ch0Max - _ch0Average)*(_ch1Max - _ch1Average)));
				/*
				if((ch1Val) < 1)
					return float3(1,1,1);
				else
					return float3(0,0,0);*/
				
				//return _viridisCM[(value/max)*256];
				return HeatMapColorNMDP(value, max*_nmdp_minMultFactor/255.0, max*_nmdp_maxMultFactor/255.0);
					
			
			}
			

			// Fragment Shader
			float4 frag (fragInput i) : COLOR
			{
				float4 outColor = float4(0.0, 0.0, 0.0, 0.0);
				float3 boxDim = float3(1.0, 1.0, 1.0);	// TODO: I don't know why it should be like this, I should use _BoxDim
				_numSamples = min(_numSamples, maxNumSamples);

				//get the 3D texture coordinates for lookup into the volume dataset
				//i.uv element [-cube/2, cube/2] eg [-0.5, 0.5]
				//dataPos element [0, 1/cube] eg [0, 1.0]
				float3 dataPos = (i.uv + boxDim/2.0)/boxDim;

				bool isLeft = false;
				float3 cameraPos;
				if(_currentEye == 0)
				{
					isLeft = true;
					cameraPos = _camPosLeft.xyz;
				}
				else if (_currentEye == 1)
				{
					cameraPos = _camPosRight.xyz;
				}
				else	// this should never happen
				{
					cameraPos = float3(0.0, 0.0, 0.0);
				}
				//float3 camPosFrag = WorldSpaceViewDir(i.pos); 	// defined in UnityCG
				//Getting the ray marching direction:
				float3 geomDir = normalize(i.uv - cameraPos.xyz);

				float3 dirStep = geomDir * (2.0 / (boxDim*_numSamples));// step_size.x; //NOTE: this was step_size  //float3(_BoxDim.x / MAX_SAMPLES, _BoxDim.y / MAX_SAMPLES, _BoxDim.z / MAX_SAMPLES)

				//flag to indicate if the raymarch loop should terminate
				bool stop = false;

				outColor.a = 0.0;

				//[unroll]//(maxNumSamples)]
				//int iterations = _numSamples & 100;
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
						break;
					}

					// data fetching from the red channel of volume texture
					//float sample = tex3D(_VolumeRed, i.uv).r;// float3(tex3D(_VolumeRed, i.uv).r, tex3D(_VolumeGreen, i.uv).r, 0.0);
					//float4 sample = float4(tex3D(_VolumeRed, dataPos).r, tex3D(_VolumeGreen, dataPos).r, tex3D(_VolumeBlue, dataPos).r, 0.5);
					//float4 sample = float4(tex3D(_VolumeRed, i.uv).r*_redOpacity + tex3D(_VolumePurple, i.uv).r*_purpleOpacity*0.75, tex3D(_VolumeGreen, i.uv).r*_greenOpacity, tex3D(_VolumeBlue, i.uv).r*_blueOpacity + tex3D(_VolumePurple, i.uv).r*_purpleOpacity*0.75, 0.5);

					//TODO: The 0.5 factor is just a thumbsuck value... really determine what it should be
					float purple = tex3Dlod(_VolumePurple, float4(dataPos, _lod)).r*_purpleOpacity*0.5;
					float4 sample = float4(tex3Dlod(_VolumeRed, float4(dataPos, _lod)).r*_redOpacity + purple, tex3Dlod(_VolumeGreen, float4(dataPos, _lod)).g*_greenOpacity, tex3Dlod(_VolumeBlue, float4(dataPos, _lod)).b*_blueOpacity + purple, 1.0);

					// colocalization code
					float chan0Value = 0.0;
					float chan1Value = 0.0;

										// determine which channels to use
					switch (_colChannel0)
					{
					case 0: chan0Value = tex3Dlod(_VolumeRed, float4(dataPos, _lod)).r*_redOpacity; break;	//TODO: reconsider using _redOpacity
					case 1: chan0Value = tex3Dlod(_VolumeGreen, float4(dataPos, _lod)).g*_greenOpacity; break;
					case 2: chan0Value = tex3Dlod(_VolumeBlue, float4(dataPos, _lod)).b*_blueOpacity; break;
					case 3: chan0Value = tex3Dlod(_VolumePurple, float4(dataPos, _lod)).r*_purpleOpacity; break;
					}

					switch (_colChannel1)
					{
					case 0: chan1Value = tex3Dlod(_VolumeRed, float4(dataPos, _lod)).r*_redOpacity; break;	//TODO: reconsider using _redOpacity
					case 1: chan1Value = tex3Dlod(_VolumeGreen, float4(dataPos, _lod)).g*_greenOpacity; break;
					case 2: chan1Value = tex3Dlod(_VolumeBlue, float4(dataPos, _lod)).b*_blueOpacity; break;
					case 3: chan1Value = tex3Dlod(_VolumePurple, float4(dataPos, _lod)).r*_purpleOpacity; break;
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
							//sample.a = colocalizationOpacity;
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
							//sample.a = colocalizationOpacity;
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
							//sample.a = colocalizationOpacity;
						}
						else
						{
							sample = float4(0.0, 0.0, 0.0, 0.0);
							continue;
						}
					}
					else if (_colocalizationMethod == 3) // show only colocalized area (as heatmap)
					{
						if (thresholdCond)
						{
									
							sample.rgb = getGradientColocHMWithDist(chan0Value, chan1Value, _maxValue); // with perpendicular  <--- use this one
							//sample.a = colocalizationOpacity;
						}
						else
						{
							sample = float4(0.0, 0.0, 0.0, 0.0);
							continue;
						}
					}
					else if (_colocalizationMethod == 4) // show only colocalized area (as heatmap) nMDP
					{
						if (thresholdCond)
						//if (texColor.x > _Threshold && texColor.y > _Threshold && texColor.z > _Threshold)
						{
							//texColor = getGradientColocAverageHM(chan0Value, chan1Value, _maxValue);	 //with averages <---- nMDP
							sample.rgb = getGradientColocAverageHM(chan0Value, chan1Value, 1.0f);	 //with averages <---- nMDP
				
						}
						else
						{
							sample = float4(0.0, 0.0, 0.0, 0.0);
							continue;
						}
					}
					else // don't do colocalization
					{

					}


					//float threshold = 0.0;
					if (sample.x < _Threshold && sample.y < _Threshold && sample.z < _Threshold)
					{
						//outColor = float4(sample.xyz, 0.0);
						sample = float4(0.0, 0.0, 0.0, 0.0);
						continue;
						//break;
					}
					
					//outColor = (1.0 - outColor.a)*sample + outColor;
					

					
					//Opacity calculation using compositing:
					//float prev_alpha_r = sample.r - (sample.r * outColor.a);
					//float prev_alpha_g = sample.g - (sample.g * outColor.a);
					//float prev_alpha_b = sample.b - (sample.b * outColor.a);

					//outColor.r = prev_alpha_r * sample.r + outColor.r;
					//outColor.g = prev_alpha_g * sample.g + outColor.g;
					//outColor.b = prev_alpha_b * sample.b + outColor.b;
					////outColor.a = (1.0 - outColor.a)*sample.a + outColor.a;
					//outColor.a += (prev_alpha_r +prev_alpha_g + prev_alpha_b) / 3.0;


					if (_colocalizationMethod > 4 || (sample.x != 1.0 && sample.y != 1.0 && sample.z != 1.0 && _colocalizationMethod != 1))
					{
						float prev_alpha = (1.0f - outColor.a)*(sample.r + sample.g + sample.b) / 3.0f * _Opacity*3; //multiply this by a transparency value
						outColor.rgb = prev_alpha * sample.rgb + outColor.rgb; // float3(prev_alpha*sample.r + outColor.r, prev_alpha*sample.g + outColor.g, prev_alpha*sample.b + outColor.b);//
						outColor.a += prev_alpha;

					}
					else
					{
						float prev_alpha = (1.0 - outColor.a)*(sample.r + sample.g + sample.b) / 3.0 * _colocalizationOpacity*3; //multiply this by a transparency value
						outColor.rgb = prev_alpha * float3(sample.rgb) + outColor.rgb;
						outColor.a += prev_alpha;
					}

					//outColor.rgb += (1.0 - outColor.a) * float3(sample.rgb);
					//outColor.a += (1.0 - outColor.a)*(sample.r + sample.g + sample.b) / 3.0 * opacity; //multiply this by a transparency value

					//if(sample.x < 0.1 && sample.y < 0.1 && sample.z < 0.1)
					//	return float4(1,1,1,1);

					//early ray termination
					if (outColor.a > 0.9)
					{
						break;
					}

				}

				return outColor;

			}
			 
			ENDCG
		 
		}
	}
	 
	Fallback "VertexLit"
}