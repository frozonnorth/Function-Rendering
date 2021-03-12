// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Modified by Khiew Jian Bin 2019
// Simple hack to make it 

Shader "Legacy Shaders/Transparent/Specular" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
    _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

Cull Back
ZWrite Off
ZTest Off
CGPROGRAM
#pragma surface surf BlinnPhong alpha:fade vertex:vert

sampler2D _MainTex;
fixed4 _Color;
half _Shininess;

struct Input {
    float2 uv_MainTex;
};

void vert(inout appdata_full v) {
	//v.normal.xyz = v.normal * -1;
}

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = tex.rgb * _Color.rgb;
    o.Gloss = tex.a;
    o.Alpha = tex.a * _Color.a;
    o.Specular = _Shininess;
}
ENDCG

Cull Front
ZWrite Off
ZTest Off
CGPROGRAM
#pragma surface surf BlinnPhong alpha:fade vertex:vert

sampler2D _MainTex;
fixed4 _Color;
half _Shininess;

struct Input {
	float2 uv_MainTex;
};

void vert(inout appdata_full v) {
	v.normal.xyz = v.normal * -1;
}

void surf(Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = tex.rgb * _Color.rgb;
	o.Gloss = tex.a;
	o.Alpha = tex.a * _Color.a;
	o.Specular = _Shininess;
}
ENDCG

}

Fallback "Legacy Shaders/Transparent/VertexLit"
}
