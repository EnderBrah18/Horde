Shader "Horde/OcclusionOutline"
{
    Properties
    {
        _OutlineColor("Color", Color) = (1,0.35,0.15,1)
        _OutlineWidth("Width", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+101" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite Off
            ZTest Greater
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil { Ref 8 ReadMask 8 WriteMask 0 Comp NotEqual Pass Keep }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 Vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += TransformObjectToWorldNormal(input.normalOS) * _OutlineWidth;
                return TransformWorldToHClip(positionWS);
            }
            half4 Frag() : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
