Shader "Sprites/Default with Fog"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
		_AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags
		{ 
			"RenderPipeline"="UniversalPipeline"
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="TransparentCutout" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off

		// Main color pass
		Pass
		{
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			ZWrite Off
			Blend One OneMinusSrcAlpha

		HLSLPROGRAM
			#pragma vertex SpriteVertFog
			#pragma fragment SpriteFragFog
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half4 _RendererColor;
				float4 _Flip;
				half _AlphaCutoff;
			CBUFFER_END

			struct appdata
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f_fog
			{
				float4 vertex   : SV_POSITION;
				half4 color     : COLOR;
				float2 texcoord : TEXCOORD0;
				float fogCoord  : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f_fog SpriteVertFog(appdata IN)
			{
				v2f_fog OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

			#ifdef UNITY_INSTANCING_ENABLED
				IN.vertex.xy *= _Flip.xy;
			#endif

				OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color * _RendererColor;

			#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
			#endif

				OUT.fogCoord = ComputeFogFactor(OUT.vertex.z);

				return OUT;
			}

			half4 SpriteFragFog(v2f_fog IN) : SV_Target
			{
				half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
				half4 c = texColor * IN.color;

				// Discard transparent pixels so they don't write to depth
				clip(c.a - _AlphaCutoff);

				// Apply fog
				c.rgb = MixFog(c.rgb, IN.fogCoord);
				c.rgb *= c.a;

				return c;
			}

		ENDHLSL
		}

		// Depth only pass - writes to depth buffer for Depth of Field
		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			Cull Off

		HLSLPROGRAM
			#pragma vertex DepthVert
			#pragma fragment DepthFrag
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ PIXELSNAP_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half4 _RendererColor;
				float4 _Flip;
				half _AlphaCutoff;
			CBUFFER_END

			struct appdata
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f_depth
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f_depth DepthVert(appdata IN)
			{
				v2f_depth OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

			#ifdef UNITY_INSTANCING_ENABLED
				IN.vertex.xy *= _Flip.xy;
			#endif

				OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
				OUT.texcoord = IN.texcoord;

			#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
			#endif

				return OUT;
			}

			half4 DepthFrag(v2f_depth IN) : SV_Target
			{
				half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
				clip(texColor.a - _AlphaCutoff);
				return 0;
			}

		ENDHLSL
		}
	}

	// Fallback for compatibility
	Fallback "Sprites/Default"
}