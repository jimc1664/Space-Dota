Shader "Hidden/FoW" {
Properties {
	_MainTex ("", 2D) = "" {}
	_RandomTexture ("", 2D) = "" {}
	_Fow ("", 2D) = "" {}
}
Subshader {
	ZTest Always Cull Off ZWrite Off

CGINCLUDE
// Common code used by several SSAO passes below
#include "UnityCG.cginc"

struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
};

struct v2f_ao {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float2 uvr : TEXCOORD1;
	float3 ray : TEXCOORD2;
};

uniform float2 _NoiseScale;
float4 _CameraDepthNormalsTexture_ST;



sampler2D _MainTex;
sampler2D _CameraDepthNormalsTexture;
sampler2D _CameraDepthTexture;
sampler2D _RandomTexture;
float4 _Params; // x=radius, y=minz, z=attenuation power, w=SSAO power


float4x4 _Camera2World;
float4x4 _CameraToWorld;


ENDCG


	
	// ---- Composite pass
	Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

uniform float4x4 _FrustumCornersWS;
sampler2D _Fow;
	uniform float4 _CameraWS;

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv[2] : TEXCOORD0;
	float4 interpolatedRay : TEXCOORD2;
};



v2f vert (appdata_img v)
{
	v2f o;
	half index = v.vertex.z;
//	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.uv[0] = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord);
	o.uv[1] = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord);
	//o.uv[1].y = 1- o.uv[1].y;
	o.uv[0].y = 1- o.uv[0].y;


		v.vertex.z = 0.1;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	/*	
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif		
*/
	o.interpolatedRay = _FrustumCornersWS[(int)index];
	o.interpolatedRay.w = index;

	return o;
}



half4 frag( v2f i ) : SV_Target {
	half4 c = tex2D (_MainTex, i.uv[0]);

	
   // DecodeDepthNormal (depthnormal, depth, viewNorm);
   // depth *= _ProjectionParams.z;
	//depth = Linear01Depth( depth );
		//return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);

	//i.ray.xy = (i.uv[0] -float2(0.5,0.5))*float2(1,-1);

	//UNITY_MATRIX_P
	//i.ray.x *= _ScreenParams.x / _ScreenParams.y;
	//i.ray.y *= _ScreenParams.y / _ScreenParams.x;
	//i.ray.z = 1; 


	float fov = 2*atan(1 / UNITY_MATRIX_P[1][1]);
	//i.ray.y /= tan(fov);
	//i.ray.y *= UNITY_MATRIX_P[0][0];

	//i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	//i.ray = normalize( i.ray );
	//i.ray = viewNorm;
	//float4 vpos = float4(i.ray * depth,1);
	//vpos.w = 1;
	//float3 wpos = mul (_Camera2World, vpos).xyz;
	//wpos = vpos.xyz;
	//wpos += _WorldSpaceCameraPos.xyz;
	//c.xyz = wpos.xyz /2;
	//c.xy = (float2(0.5,0.5) + wpos.xy) /3;
	//c.z = 0;
	//c.xy *= 2;
	//c.y = sin(c.y);
	//c.x = sin(c.x);
	//c.xy *= 0;
	//c.x = sin(c.x);
	//wpos.x *= -1;

	//float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv[1]);
	//float dpth = Linear01Depth(rawDepth);

	float4 depthnormal = tex2D (_CameraDepthNormalsTexture, i.uv[1] );
    float3 viewNorm;
    float depth;
	DecodeDepthNormal (depthnormal, depth, viewNorm);

	depthnormal.rgb = normalize( 2*depthnormal.rgb -   float3(1,1,1) );

	//depth = Linear01Depth(depth);
	//depth *= _ProjectionParams.z;
	float4 wsDir = depth * i.interpolatedRay;
	float4 wsPos = _CameraWS + wsDir;



	wsPos.x *= -1;
	half2 uv = wsPos.xy /64 +half2(0.5,0.5f);


	float nMod = dot( depthnormal.rgb, float3(0,0,-1) );
	//c.rgb = float3(1,1,1) *nMod;
	//c.rgb = -depthnormal.rgb;

	nMod = 0.5 +0.5*nMod;
	float2 losM = tex2D (_Fow, uv ).rg;
	float los = tex2D (_Fow, uv ).r;
		
	float hn = 0.8, h1 = 1.0f, h2 = 1.3f;
	float hMod =  1-saturate(  (( nMod - hn) /(1-hn) ) * (  (wsPos.z-h1)/(h2-h1) ) ) ;
	//if( wsPos.z > 0.3  && nMod > 0.8f) 
	//	los = 0;
	//los *= hMod;

	//c.rgb = (c.rgb + los*2)/3;

	los = max( los, losM.g );

	half3 p2 = min( c.rgb, float3(1,1,1)*0.5 );
	float avgM = 20.0/( p2.r+p2.b+p2.r +0.8); 
	float3 grey = float3(1,1,1)/avgM  +0.01 +  (1-nMod) *0.3f;
	c.rgb = lerp ( grey, c.rgb,los*0.9 );

	return c;
}
ENDCG
	}

}

Fallback off
}
