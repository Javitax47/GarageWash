Shader "Custom/Uber Shader"
{
    Properties
    {
        // ------------------------------------------------------------------
        // Propiedades Estándar URP Lit
        // ------------------------------------------------------------------
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        [ToggleUI] _AlphaClip("Alpha Clip", Float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [HideInInspector] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _MetallicGlossMap("Metallic", 2D) = "white" {}

        [HideInInspector] _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        [HideInInspector] _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [HideInInspector] _BumpMap("Normal Map", 2D) = "bump" {}

        [HideInInspector] _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        [HideInInspector] _ParallaxMap("Height Map", 2D) = "black" {}

        [HideInInspector] _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _OcclusionMap("Occlusion", 2D) = "white" {}

        [HideInInspector] [HDR] _EmissionColor("Color", Color) = (0,0,0)
        [HideInInspector] _EmissionMap("Emission", 2D) = "white" {}

        [HideInInspector] _DetailMask("Detail Mask", 2D) = "white" {}
        [HideInInspector] _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [HideInInspector] _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        [HideInInspector] _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [HideInInspector] [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        
        // ------------------------------------------------------------------
        // Propiedades Personalizadas de Suciedad (Dirtiness)
        // ------------------------------------------------------------------
        [Header(Dirtiness)]
        // Hemos quitado [NoScaleOffset] para permitir Tiling y Offset en el inspector
        _DirtTexA("Dirt A (Red Channel - e.g. Mud)", 2D) = "white" {}
        _DirtTexB("Dirt B (Green Channel - e.g. Oil)", 2D) = "white" {}
        _DirtMask("Dirt Mask (R=A, G=B)", 2D) = "white" {}

        // ------------------------------------------------------------------
        // Propiedades Internas (No tocar)
        // ------------------------------------------------------------------
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex OverrideLitPassVertex
            #pragma fragment OverrideLitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            // ------------------------------------------------------------------
            // Declaración de Texturas Custom y Tiling/Offset
            // ------------------------------------------------------------------
            TEXTURE2D(_DirtTexA); SAMPLER(sampler_DirtTexA);
            float4 _DirtTexA_ST; // Variable para Tiling/Offset Dirt A

            TEXTURE2D(_DirtTexB); SAMPLER(sampler_DirtTexB);
            float4 _DirtTexB_ST; // Variable para Tiling/Offset Dirt B

            TEXTURE2D(_DirtMask); SAMPLER(sampler_DirtMask);
            float4 _DirtMask_ST; // Variable para Tiling/Offset Mask

            // Estructura para el mapeado triplanar
            struct WeightedTriplanarUV
            {
                float2 x, y, z;
                half3 weights;
            };

            // Cálculo de UVs triplanares
            WeightedTriplanarUV GetWeightedTriplanarUV(float3 positionWS, half3 normal)
            {
                WeightedTriplanarUV triUV;
                triUV.x = positionWS.zy;
                triUV.y = positionWS.xz;
                triUV.z = positionWS.xy;
                
                // Corregir orientación
                if (normal.x < 0) triUV.x.x = -triUV.x.x;
                if (normal.y < 0) triUV.y.x = -triUV.y.x;
                if (normal.z >= 0) triUV.z.x = -triUV.z.x;

                half3 triW = abs(normal);
                triUV.weights = triW / (triW.x + triW.y + triW.z);
                
                return triUV;
            }

            // Función para muestrear textura en 3 ejes y mezclar (Con soporte para Tiling/Offset)
            half4 SampleTexture3D(WeightedTriplanarUV uv, TEXTURE2D_PARAM(tex, samp), float4 st)
            {
                // Aplicamos Tiling (xy) y Offset (zw)
                float2 uvX = uv.x * st.xy + st.zw;
                float2 uvY = uv.y * st.xy + st.zw;
                float2 uvZ = uv.z * st.xy + st.zw;

                half4 albedoX = uv.weights.x * SAMPLE_TEXTURE2D(tex, samp, uvX);
                half4 albedoY = uv.weights.y * SAMPLE_TEXTURE2D(tex, samp, uvY);
                half4 albedoZ = uv.weights.z * SAMPLE_TEXTURE2D(tex, samp, uvZ);
                return albedoX + albedoY + albedoZ;
            }
            
            // Función Principal de Inicialización de Material
            inline void InitializeStandardLitSurfaceDataWithTriplanar(float2 uv, float2 lightmapUV, float3 positionWS, float3 normalWS, out SurfaceData outSurfaceData)
            {
                // 1. Color base estándar
                half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

                // 2. Preparar Triplanar
                WeightedTriplanarUV triplanarUV = GetWeightedTriplanarUV(positionWS, normalWS);

                // 3. Muestrear las dos texturas de suciedad (Pasando parámetros de Tiling)
                half4 dirtA = SampleTexture3D(triplanarUV, _DirtTexA, sampler_DirtTexA, _DirtTexA_ST);
                half4 dirtB = SampleTexture3D(triplanarUV, _DirtTexB, sampler_DirtTexB, _DirtTexB_ST);
                
                // 4. Muestrear la máscara (Usando UVs base + Tiling)
                float2 maskUV = uv * _DirtMask_ST.xy + _DirtMask_ST.zw;
                half4 dirtMask = SAMPLE_TEXTURE2D(_DirtMask, sampler_DirtMask, maskUV);

                // 5. Aplicar mezclas (Capas)
                // Capa A (Barro) controlada por canal Rojo
                albedoAlpha = lerp(albedoAlpha, dirtA, dirtMask.r);
                // Capa B (Aceite) controlada por canal Verde
                albedoAlpha = lerp(albedoAlpha, dirtB, dirtMask.g);

                // 6. Asignar resto de propiedades estándar
                half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
                outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
                outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

            #if _SPECULAR_SETUP
                outSurfaceData.metallic = half(1.0);
                outSurfaceData.specular = specGloss.rgb;
            #else
                outSurfaceData.metallic = specGloss.r;
                outSurfaceData.specular = half3(0.0, 0.0, 0.0);
            #endif

                outSurfaceData.smoothness = specGloss.a;
                outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
                outSurfaceData.occlusion = SampleOcclusion(uv);
                outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

            #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
                half2 clearCoat = SampleClearCoat(uv);
                outSurfaceData.clearCoatMask       = clearCoat.r;
                outSurfaceData.clearCoatSmoothness = clearCoat.g;
            #else
                outSurfaceData.clearCoatMask       = half(0.0);
                outSurfaceData.clearCoatSmoothness = half(0.0);
            #endif

            #if defined(_DETAIL)
                half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
                float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
                outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
                outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
            #endif
            }

            struct OverrideVaryings
            {
                Varyings v;
                float2 lightMapUV : TEXCOORD4; // Mantenemos esto por compatibilidad, aunque la máscara ya no lo use
            };

            OverrideVaryings OverrideLitPassVertex(Attributes input)
            {
                Varyings v = LitPassVertex(input);
                OverrideVaryings ov;
                ov.v = v;
                ov.lightMapUV = input.staticLightmapUV; 
                return ov;
            }

            void OverrideLitPassFragment(
                OverrideVaryings ov
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                Varyings input = ov.v;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #if defined(_PARALLAXMAP)
            #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                half3 viewDirTS = input.viewDirTS;
            #else
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
            #endif
                ApplyPerPixelDisplacement(viewDirTS, input.uv);
            #endif

                SurfaceData surfaceData;
                // Llamamos a nuestra función personalizada de inicialización
                InitializeStandardLitSurfaceDataWithTriplanar(input.uv, ov.lightMapUV, input.positionWS, input.normalWS, surfaceData);

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

            #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
            #endif

                InitializeBakedGIData(input, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

                outColor = color;

            #ifdef _WRITE_RENDERING_LAYERS
                uint renderingLayers = GetMeshRenderingLayer();
                outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}