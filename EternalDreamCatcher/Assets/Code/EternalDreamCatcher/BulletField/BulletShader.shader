Shader "Hidden/BulletShader"
{
    Properties
    { }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Non-glowy bullet shader"

            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BulletShaderContent.cginc"
            ENDCG
        }

        Pass
        {
            Name "Glowy bullet shader"

            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BulletShaderContent.cginc"
            ENDCG
        }
    }
}
