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
			"Queue"="AlphaTest" 
			"IgnoreProjector"="True" 
			"RenderType"="TransparentCutout" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off

		Pass
		{
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

		HLSLPROGRAM
			#pragma vertex SpriteVertFog
			#pragma fragment SpriteFragFog
			#pragma target 2.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile_fog

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

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
				float4 vertex    : SV_POSITION;
				half4 color      : COLOR;
				float2 texcoord  : TEXCOORD0;
				float fogCoord   : TEXCOORD1;
				half2 lightingUV : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f_fog SpriteVertFog(appdata IN)
			{
				v2f_fog OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				SetUpSpriteInstanceProperties();
				IN.vertex.xyz = UnityFlipSprite(IN.vertex.xyz, unity_SpriteProps.xy);

				OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
				OUT.texcoord = IN.texcoord;

				OUT.color = IN.color * _Color * unity_SpriteColor;

			#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
			#endif

				OUT.fogCoord = ComputeFogFactor(OUT.vertex.z);
				OUT.lightingUV = half2(ComputeScreenPos(OUT.vertex / OUT.vertex.w).xy);

				return OUT;
			}

			half4 SpriteFragFog(v2f_fog IN) : SV_Target
			{
				half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord);
				half4 c = texColor * IN.color;

				clip(c.a - _AlphaCutoff);

				SurfaceData2D surfaceData;
				InputData2D inputData;

				InitializeSurfaceData(c.rgb, c.a, surfaceData);
				InitializeInputData(IN.texcoord, IN.lightingUV, inputData);

				half4 litColor = CombinedShapeLightShared(surfaceData, inputData);

				litColor.rgb = MixFog(litColor.rgb, IN.fogCoord);

				return litColor;
			}

		ENDHLSL
		}

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

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

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

				SetUpSpriteInstanceProperties();
				IN.vertex.xyz = UnityFlipSprite(IN.vertex.xyz, unity_SpriteProps.xy);

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

	Fallback "Sprites/Default"
}
