Shader "Programmable_Color"
{
Properties
{
_URange("URange", Vector) = (0, 1, 1, 1)
_VRange("VRange", Vector) = (0, 1, 1, 1)
_WRange("WRange", Vector) = (0, 1, 1, 1)
_TRange("TRange", Vector) = (0, 1, 1, 1)
_Tcycle("Timecycle", float) = 1
}
SubShader
{
Pass
{
Tags{"LightMode" = "ForwardBase" }
Cull Off CGPROGRAM
#pragma vertex vert
#pragma fragment frag
uniform float4 _URange;
uniform float4 _VRange;
uniform float4 _WRange;
uniform float4 _TRange;
uniform float _Tcycle;
struct vertexInput { float4 vertex : POSITION; float3 normal : NORMAL; };
struct vertexOutput { float4 pos : SV_POSITION; float4 posWorld : TEXCOORD0; float3 normalDir : TEXCOORD1; };
vertexOutput vert(vertexInput input)
{
vertexOutput output;
float4x4 modelMatrix = unity_ObjectToWorld;
float4x4 modelMatrixInverse = unity_WorldToObject;
output.posWorld = mul(modelMatrix, input.vertex);
output.normalDir = normalize(mul(float4(input.normal, 0.0), modelMatrixInverse).xyz);
output.pos = UnityObjectToClipPos(input.vertex);
return output;
}
static const float pi = 3.14159265358;
float4 frag(vertexOutput input) : COLOR 
{
float u = (input.posWorld.x - _URange.x) / (_URange.y - _URange.x);
float v = (input.posWorld.y - _URange.x) / (_URange.y - _URange.x);
float w = (-input.posWorld.z - _URange.x) / (_URange.y - _URange.x);
float t = cos((_Time.y * 6) / _Tcycle) * (_TRange.y - _TRange.x) + _TRange.x;
float r = u;
float g = v;
float b = w;
return float4(r, g, b, 1);
}
ENDCG
}
}
}