Shader "Horde/OcclusionMask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite Off
            ZTest Always
            Cull Back
            ColorMask 0
            // Bit 8 is in URP's user mask; upper bits belong to deferred lighting.
            Stencil { Ref 8 ReadMask 8 WriteMask 8 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }
            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
