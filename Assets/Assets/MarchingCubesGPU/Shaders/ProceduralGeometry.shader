Shader "PavelKouril/Marching Cubes/Procedural Geometry"
{
	Properties{
		_Color("Diffuse Material Color", Color) = (1,1,1,1)
		_SpecColor("Specular Material Color", Color) = (1,1,1,1)
		_Glossiness("Shininess", Float) = 1
	}
	SubShader
	{
		Pass
		{
			Tags{ "RenderType" = "Opqaue" "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha

			Cull Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			uniform float4 _LightColor0;

			// User-specified properties
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float _Glossiness;

			struct Vertex
			{
				float3 vPosition;
				float3 vNormal;
			};
			struct Triangle
			{
				Vertex v[3];
			};

			uniform StructuredBuffer<Triangle> triangles;
			uniform float4x4 model;
			
			struct v2f
			{
				float4 posWorld : SV_POSITION;
				float3 normalDir : NORMAL;
			};

			v2f vert(uint id : SV_VertexID)
			{
				v2f o;

				uint pid = id / 3;
				uint vid = id % 3;

				o.posWorld = mul(UNITY_MATRIX_VP, mul(model, float4(triangles[pid].v[vid].vPosition, 1)));
				o.normalDir = mul(unity_ObjectToWorld, triangles[pid].v[vid].vNormal*-1);
				
				return o;
			}

			float4 frag(v2f input) : COLOR
			{
				float3 normalDirection = normalize(input.normalDir);

				float3 viewDirection = normalize(
					_WorldSpaceCameraPos - input.posWorld.xyz);
				float3 lightDirection;
				float attenuation;

				if (0.0 == _WorldSpaceLightPos0.w) // directional light?
				{
					attenuation = 1.0; // no attenuation
					lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else // point or spot light
				{
					float3 vertexToLightSource =
						_WorldSpaceLightPos0.xyz - input.posWorld.xyz;
					float distance = length(vertexToLightSource);
					attenuation = 1.0 / distance; // linear attenuation 
					lightDirection = normalize(vertexToLightSource);
				}

				float3 ambientLighting =
					UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

				float3 diffuseReflection =
					attenuation * _LightColor0.rgb * _Color.rgb * _Color.a
					* max(0.0, dot(normalDirection, lightDirection));

				float3 specularReflection;
				if (dot(normalDirection, lightDirection) < 0.0)
					// light source on the wrong side?
				{
					specularReflection = float3(0.0, 0.0, 0.0);
					// no specular reflection
				}
				else // light source on the right side
				{
					specularReflection = attenuation * _LightColor0.rgb
						* _SpecColor.rgb * pow(max(0.0, dot(
							reflect(-lightDirection, normalDirection),
							viewDirection)), clamp(1-_Glossiness,0.01,1)*100);
				}

				return float4(ambientLighting + diffuseReflection
					+ specularReflection, _Color.a);
			}
			ENDCG
		}
	}
}
