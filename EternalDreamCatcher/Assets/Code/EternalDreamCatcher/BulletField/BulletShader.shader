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
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BulletShaderContent.cginc"
            ENDCG
        }
    }
}
