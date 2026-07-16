Shader "Custom/Paint"
{
    Properties
    {
        _PaintPositionWS("Paint Position", Vector) = (0, 0, 0, 0)
        _PaintNormalWS("Paint Normal", Vector) = (0, 0, 0, 0)
        _SpreadRadius("Spread Radius", Float) = 0
        _PaintColor("Paint Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "Universal Forward"
            Cull Off ZWrite Off ZTest Always ZClip Off
            
            // Mezcla Multiplicativa (Para limpiar sobre tu textura)
            Blend DstColor Zero
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local_fragment _ FLOOD_COLOR
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0; // --- CAMBIO: USAMOS UV0 (Tus UVs) ---
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float3 _PaintPositionWS; float3 _PaintNormalWS; float _SpreadRadius; float4 _PaintColor;
            CBUFFER_END
            
            Varyings vert(Attributes i)
            {
                Varyings v;
                v.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                v.normalWS = TransformObjectToWorldNormal(i.normalOS);
                
                // --- CAMBIO: Usamos 'i.uv' en vez de 'i.lightMapUV' ---
                // Mapeamos tus UVs (0 a 1) a la pantalla (-1 a 1)
                float3 remappedPositionWS = float3(i.uv * 2 - 1, 0);
                
                v.positionCS = TransformWorldToHClip(remappedPositionWS);
                return v;
            }
            
            float4 frag(Varyings i) : SV_Target0
            {
                #ifdef FLOOD_COLOR
                    return _PaintColor;
                #endif

                float dist = distance(i.positionWS, _PaintPositionWS);
                
                // Tijera para optimizar y no borrar el resto
                clip(_SpreadRadius - dist); 

                float positionStrength = 1 - saturate(dist / _SpreadRadius);
                float facingStrength = dot(i.normalWS, _PaintNormalWS) > 0 ? 1 : 0;
                float brushImpact = positionStrength * facingStrength;

                float3 targetChannels = _PaintColor.rgb;
                float strength = _PaintColor.a * brushImpact;

                // Lerp multiplicativo
                float3 outputRGB = lerp(float3(1,1,1), targetChannels, strength);

                return float4(outputRGB, 1);
            }
            ENDHLSL
        }
    }
}