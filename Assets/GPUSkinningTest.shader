Shader "Unlit/GPUSkinningTest"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AnimTex ("Anim", 2D) = "white" {}
		_AnimTexWidth ("AnimTexWidth", float) = 1
		_AnimTexHeight ("AnimTexHeight", float) = 1
		_AnimFrameCount ("AnimFrameCount", float) = 1
		_PixelPerFrame ("PixelPerFrame", float) = 1
		_FPS ("FPS", float) = 30
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _AnimTex;
			float _AnimTexWidth, _AnimTexHeight;
			float _PixelPerFrame;
			float _FPS;
			float _AnimFrameCount;

			float4 IdxToUV(uint idx)
			{
				float4 ret;
				uint texWidth = (uint)_AnimTexWidth;
				uint texHeight = (uint)_AnimTexHeight;
				uint row = idx / texWidth;
				uint column = idx - row * texWidth;
				ret.x = (float)column / texWidth;
				ret.y = (float)row / texHeight;
				ret.z = 0;
				ret.w = 0;
				return ret;
			}

			float4x4 GetMatrix(uint frameStartIdx, uint boneIdx)
			{
				uint idx = frameStartIdx + boneIdx * 3;
				float4 pos = tex2Dlod(_AnimTex, IdxToUV(idx));
				float4 rot = tex2Dlod(_AnimTex, IdxToUV(idx+1));
				float4 sca = tex2Dlod(_AnimTex, IdxToUV(idx+2));
				return float4x4(pos, rot, sca, float4(0,0,0,1));
			}
			
			v2f vert (appdata v)
			{
				uint frameIdx = ((uint)(_Time.y * _FPS)) % ((uint)(_AnimFrameCount));
				uint frameStartIdx = frameIdx * _PixelPerFrame;

				uint bone0Idx = (uint)v.uv1.x;
				float bone0Weight = 1;//v.uv1.y;
				float4 pos = mul(GetMatrix(frameStartIdx, bone0Idx), v.vertex) * bone0Weight;

				// int bone1Idx = (int)v.uv1.z;
				// float bone1Weight = v.uv1.w;
				// pos += mul(GetMatrix(frameStartIdx, bone1Idx), v.vertex) * bone1Weight;

				// int bone2Idx = (int)v.uv2.x;
				// float bone2Weight = v.uv2.y;
				// pos += mul(GetMatrix(frameStartIdx, bone2Idx), v.vertex) * bone2Weight;

				// int bone3Idx = (int)v.uv2.z;
				// float bone3Weight = v.uv2.w;
				// pos += mul(GetMatrix(frameStartIdx, bone3Idx), v.vertex) * bone3Weight;

				v2f o;
				o.vertex = UnityObjectToClipPos(pos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
