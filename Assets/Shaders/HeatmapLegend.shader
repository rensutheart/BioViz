Shader "Volume Rendering/Heatmap" 
{
	Properties
	{

	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma exclude_renderers flash gles

			#include "UnityCG.cginc"

			struct vertInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct fragInput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			fragInput vert(vertInput v)
			{
				fragInput o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
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
					float v = (val - positions[0]) / (positions[1] - positions[0]);
					return float3(0.0, 0.0, v);
				}

				// second interval (blue constant, green grows)
				else if (val > positions[1] && val < positions[2])
				{
					float v = (val - positions[1]) / (positions[2] - positions[1]);
					return float3(0.0, v, 1.0);
				}

				// third interval (blue decrease, green constant)
				else if (val > positions[2] && val < positions[3])
				{
					float v = (val - positions[2]) / (positions[3] - positions[2]);
					return float3(0.0, 1.0, (1.0 - v));
				}

				// fourth interval (red increases, green constant)
				else if (val > positions[3] && val < positions[4])
				{
					float v = (val - positions[3]) / (positions[4] - positions[3]);
					return float3(v, 1.0, 0.0);
				}

				// fifth interval (red constnat, green decrease)
				else if (val > positions[4] && val < positions[5])
				{
					float v = (val - positions[4]) / (positions[5] - positions[4]);
					return float3(1.0, (1.0 - v), 0.0);
				}

				// sixth interval (red constant, blue increases)
				else if (val > positions[5] && val < positions[6])
				{
					float v = (val - positions[5]) / (positions[6] - positions[5]);
					return float3(1.0, 0.0, v);
				}

				// seventh interval (red constant, blue constant, green increases to white)
				else if (val > positions[6] && val < positions[7])
				{
					float v = (val - positions[6]) / (positions[7] - positions[6]);
					return float3(1.0, v, 1.0);
				}

				else if (val > max)
					return float3(1.0, 1.0, 1.0);

				return float3(0.0, 0.0, 0.0);
			}

			float3 HeatMapColorRainbow2(float val, float min, float max)
			{
				//float v = (float)(val - min) / (float)(max-min);
				float diff = max - min;
				float diffFraction = diff / 7.0;
				float positions[8] = { min, diffFraction + min, diffFraction * 2 + min, diffFraction * 3 + min, diffFraction * 4 + min, diffFraction * 5 + min, diffFraction * 6 + min, diffFraction * 7 + min }; //  the positions at which colors change

																																																				  // first interval (blue increases)
				if (val > positions[0] && val < positions[1])
				{
					float v = (val - positions[0]) / (positions[1] - positions[0]);
					return float3(v, 0.0, 0.0);
				}

				// second interval (blue constant, green grows)
				else if (val > positions[1] && val < positions[2])
				{
					float v = (val - positions[1]) / (positions[2] - positions[1]);
					return float3(1.0, v, 0);
				}

				// third interval (blue decrease, green constant)
				else if (val > positions[2] && val < positions[3])
				{
					float v = (val - positions[2]) / (positions[3] - positions[2]);
					return float3((1.0 - v), 1.0, 0.0);
				}

				// fourth interval (red increases, green constant)
				else if (val > positions[3] && val < positions[4])
				{
					float v = (val - positions[3]) / (positions[4] - positions[3]);
					return float3(0, 1.0, v);
				}

				// fifth interval (red constnat, green decrease)
				else if (val > positions[4] && val < positions[5])
				{
					float v = (val - positions[4]) / (positions[5] - positions[4]);
					return float3(0.0, (1.0 - v), 1.0);
				}

				// sixth interval (red constant, blue increases)
				else if (val > positions[5] && val < positions[6])
				{
					float v = (val - positions[5]) / (positions[6] - positions[5]);
					return float3(v, 0.0, 1.0);
				}

				// seventh interval (red constant, blue constant, green increases to white)
				else if (val > positions[6] && val < positions[7])
				{
					float v = (val - positions[6]) / (positions[7] - positions[6]);
					return float3(1.0, v, 1.0);
				}

				else if (val > max)
					return float3(1.0, 1.0, 1.0);

				return float3(0.0, 0.0, 0.0);
			}

			float4 frag(fragInput i) : COLOR
			{
				float4 col = float4(HeatMapColorRainbow(i.uv.x, 0.0, 1.0), 1.0);
				
				return col;
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}