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
            Name "Non-glowy entity shader"

            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "EntityShaderContent.cginc"
            ENDCG
        }

        Pass
        {
            Name "Glowy entity shader"

            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "EntityShaderContent.cginc"
            ENDCG
        }
    }
}
