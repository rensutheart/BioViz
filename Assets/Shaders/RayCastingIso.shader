Shader "Volume Rendering/Ray Casting Iso Surface" 
{
	Properties 
	{
	    _VolumeRed ("Texture Red", 3D) = "" {}
	    _VolumeGreen ("Texture Green", 3D) = "" {}
	    _VolumeBlue ("Texture Blue", 3D) = "" {}
	    _VolumePurple ("Texture Purple", 3D) = "" {}

	    _BoxDim("Box Dim", Vector) = (1, 1, 1, 0)
	   	_camPosLeft("Camera Position Left", Vector) = (0, 0, 0, 0)
	   	_camPosRight("Camera Position Right", Vector) = (0, 0, 0, 0)
	   	_currentEye("The current eye", Int) = 0
	    _numSamples("Number of samples", Int) = 10
	    _lod("LOD", Int) = 1

	    _isoValueIn("ISO value", Range(0, 255)) = 20
	    _deltaValue("Delta Value", Range(0.0, 0.01)) = 0.005

	    _showRed("Show Red Channel", Int) = 1
	    _showGreen("Show Green Channel", Int) = 1
	    _showBlue("Show Blue Channel", Int) = 1
	    _showPurple("Show Purple Channel", Int) = 1


		// NEW
		_BoxDim("Box Dim", Vector) = (1, 1, 1, 0)
		_ROI_P1("ROI Point 1", Vector) = (0, 0, 0, 0)
	    _ROI_P2("ROI Point 2", Vector) = (1, 1, 1, 0)

		_ROIMask3D("ROI Mask 3D", 3D) = "" {}

		_ROI_XY("ROI XY", 2D) = "" {}
		_ROI_XZ("ROI XZ", 2D) = "" {}
		_ROI_YZ("ROI YZ", 2D) = "" {}

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
		//ZWrite Off	// this is good for transparent cases (I have to test this, for when things are in front, does it still draw?)

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
			uniform int _isoValueIn;
			uniform float _deltaValue;

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

			uniform int _showRed;
			uniform int _showGreen;
			uniform int _showBlue;
			uniform int _showPurple;

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

			    o.uv = v.vertex.xyz;

			    return o;
			}


			//function to give a more accurate position of where the given iso-value (iso) is found
			//given the initial minimum limit (left) and maximum limit (right)
			float3 Bisection(float3 left, float3 right, float iso, sampler3D recTexture)
			{
				//loop 4 times
				for (int i = 0; i<4; i++)
				{
					//get the mid value between the left and right limit
					float3 midpoint = (right + left) * 0.5;
					//sample the texture at the middle point
					float cM = tex3Dlod(recTexture, float4(midpoint, 1)).x;
					//check if the value at the middle point is less than the given iso-value
					if (cM < iso)
						//if so change the left limit to the new middle point
						left = midpoint;
					else
						//otherwise change the right limit to the new middle point
						right = midpoint;
				}
				//finally return the middle point between the left and right limit
				return float3(right + left) * 0.5;
			}


			//function to calculate the gradient at the given location in the volume dataset
			//The function user center finite difference approximation to estimate the 
			//gradient
			float3 GetGradient(float3 uvw , sampler3D recTexture)
			{
				float3 s1, s2;

				//Using center finite difference 
				s1.x = tex3Dlod(recTexture, float4(uvw - float3(_deltaValue, 0.0, 0.0), 1)).x;
				s2.x = tex3Dlod(recTexture, float4(uvw + float3(_deltaValue, 0.0, 0.0), 1)).x;

				s1.y = tex3Dlod(recTexture, float4(uvw - float3(0.0, _deltaValue, 0.0), 1)).x;
				s2.y = tex3Dlod(recTexture, float4(uvw + float3(0.0, _deltaValue, 0.0), 1)).x;

				s1.z = tex3Dlod(recTexture, float4(uvw - float3(0.0, 0.0, _deltaValue), 1)).x;
				s2.z = tex3Dlod(recTexture, float4(uvw + float3(0.0, 0.0, _deltaValue), 1)).x;

				return normalize((s1 - s2) / (2.0*_deltaValue));
			}

			//function to estimate the PhongLighting component given the light vector (L),
			//the normal (N), the view vector (V), the specular power (specPower) and the
			//given diffuse colour (diffuseColor). The diffuse component is first calculated
			//Then, the half way vector is computed to obtain the specular component. Finally
			//the diffuse and specular contributions are added together
			float4 PhongLighting(float3 L, float3 N, float3 V, float specPower, float3 diffuseColor)
			{
				float ambient = 0.075;
				float diffuse = max(dot(L, N), 0.0);
				float3 halfVec = normalize(L + V);
				float specular = pow(max(0.00001, dot(halfVec, N)), specPower);

				return float4(((diffuse+ambient)*diffuseColor + specular), 1.0);				
			}

			// Fragment Shader
			float4 frag (fragInput i) : COLOR
			{
				float4 outColor = float4(0.0, 0.0, 0.0, 0.0);
				float3 boxDim = float3(1.0, 1.0, 1.0);	// TODO: I don't know why it should be like this, I should use _BoxDim
				_numSamples = min(_numSamples, maxNumSamples);

				float isoValue = _isoValueIn / 255.0;	//the isovalue for iso-surface detection

				//get the 3D texture coordinates for lookup into the volume dataset
				//i.uv element [-cube/2, cube/2] eg [-0.5, 0.5]
				//dataPos element [0, 1/cube] eg [0, 1.0]
				float3 dataPos = (i.uv + boxDim/2.0)/boxDim;

				float3 cameraPos;
				if(_currentEye == 0)
				{
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
					float sampleRed = tex3Dlod(_VolumeRed, float4(dataPos, 1)).r;			//current sample
					float sampleRed2 = tex3Dlod(_VolumeRed, float4(dataPos + dirStep, 1)).r;	//next sample
					bool toRenderRed = false;

					float sampleGreen = tex3Dlod(_VolumeGreen, float4(dataPos, 1)).g;			//current sample
					float sampleGreen2 = tex3Dlod(_VolumeGreen, float4(dataPos + dirStep, 1)).g;	//next sample
					bool toRenderGreen = false;

					float sampleBlue = tex3Dlod(_VolumeBlue, float4(dataPos, 1)).b;			//current sample
					float sampleBlue2 = tex3Dlod(_VolumeBlue, float4(dataPos + dirStep, 1)).b;	//next sample
					bool toRenderBlue = false;

					//TODO: this should probably be multiplied by the same factor as in the other shaders (and marching tetrahedra)
					float samplePurple = tex3Dlod(_VolumePurple, float4(dataPos, 1)).r;			//current sample
					float samplePurple2 = tex3Dlod(_VolumePurple, float4(dataPos + dirStep, 1)).r;	//next sample
					bool toRenderPurple = false;


					float epsilon = 0.005;

					//if (((sampleRed - isoValue) < epsilon && (sampleRed2 - isoValue) >= -epsilon) || ((sampleRed2 - isoValue) < epsilon && (sampleRed - isoValue) >= epsilon))
					if ((sampleRed >= isoValue) || (sampleRed2 >= isoValue))
						toRenderRed = true;

					//if ((sampleGreen < isoValue + epsilon && sampleGreen2 >= isoValue - epsilon) || ((sampleGreen2 - isoValue) < epsilon && (sampleGreen - isoValue) >= -epsilon))
					if ((sampleGreen >= isoValue) || (sampleGreen2 >= isoValue))
							toRenderGreen = true;
							
					
					//if (((sampleBlue - isoValue) < epsilon && (sampleBlue2 - isoValue) >= -epsilon) || ((sampleBlue2 - isoValue) < epsilon && (sampleBlue - isoValue) >= -epsilon))
					if ((sampleBlue >= isoValue) || (sampleBlue2 >= isoValue))
						toRenderBlue = true;

					if ((samplePurple >= isoValue) || (samplePurple2 >= isoValue))
						toRenderPurple = true;

					//If there is a zero crossing, we refine the detected iso-surface 
					//location by using bisection based refinement.
					float3 xN = dataPos;
					float3 xF = dataPos + dirStep;


					//The view vector is simply opposite to the ray marching 
					//direction
					float3 V = -geomDir;

					//We keep the view vector as the light vector to give us a head 
					//light
					float3 L = V;

					//In case of iso-surface rendering, we do not use compositing. 
					//Instead, we find the zero crossing of the volume dataset iso function 
					//by sampling two consecutive samples. 
					
					if (toRenderRed && _showRed == 1)
					{
						float3 tc = Bisection(xN, xF, isoValue, _VolumeRed);

						//To get the shaded iso-surface, we first estimate the normal
						//at the refined position
						float3 N = GetGradient(tc, _VolumeRed);


						//Finally, we call PhongLighing function to get the final colour
						//with diffuse and specular components. Try changing this call to this
						//vFragColor =  PhongLighting(L,N,V,250,  tc); to get a multi colour
						//iso-surface
						outColor = PhongLighting(L, N, V, 250, float3(1.0, 0.0, 0.0));// float3(sample.rgb));
						break;
					}
					
					if (toRenderGreen && _showGreen == 1)
					{
						float3 tc = Bisection(xN, xF, isoValue, _VolumeGreen);

						//To get the shaded iso-surface, we first estimate the normal
						//at the refined position
						float3 N = GetGradient(tc, _VolumeGreen);


						//Finally, we call PhongLighing function to get the final colour
						//with diffuse and specular components. Try changing this call to this
						//vFragColor =  PhongLighting(L,N,V,250,  tc); to get a multi colour
						//iso-surface
						outColor =  PhongLighting(L, N, V, 250, float3(0.0, 1.0, 0.0));// float3(sample.rgb)); float4(0.0, 1.0, 0.0, 1.0);//
						
						break;
					}
					
					if (toRenderBlue && _showBlue == 1)
					{
						float3 tc = Bisection(xN, xF, isoValue, _VolumeBlue);

						//To get the shaded iso-surface, we first estimate the normal
						//at the refined position
						float3 N = GetGradient(tc, _VolumeBlue);


						//Finally, we call PhongLighing function to get the final colour
						//with diffuse and specular components. Try changing this call to this
						//vFragColor =  PhongLighting(L,N,V,250,  tc); to get a multi colour
						//iso-surface
						outColor = PhongLighting(L, N, V, 250, float3(0.0, 0.0, 1.0));// float3(sample.rgb));
						break;
					}

					if (toRenderPurple && _showPurple == 1)
					{
						float3 tc = Bisection(xN, xF, isoValue, _VolumePurple);

						//To get the shaded iso-surface, we first estimate the normal
						//at the refined position
						float3 N = GetGradient(tc, _VolumePurple);


						//Finally, we call PhongLighing function to get the final colour
						//with diffuse and specular components. Try changing this call to this
						//vFragColor =  PhongLighting(L,N,V,250,  tc); to get a multi colour
						//iso-surface
						outColor = PhongLighting(L, N, V, 250, float3(1.0, 0.0, 1.0));// float3(sample.rgb));
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