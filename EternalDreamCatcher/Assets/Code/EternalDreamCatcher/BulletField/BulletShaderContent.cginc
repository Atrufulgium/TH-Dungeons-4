StructuredBuffer<float3> _BulletPosses;

struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f {
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

sampler2D _MainTex;
float4 _MainTex_ST;

v2f vert (appdata v, uint instanceID : SV_INSTANCEID) {
    v2f o;
    float4 pos = v.vertex;
    pos.xy *= 0.01;
    pos.xyz += _BulletPosses[instanceID];
    // We're rendering *directly* onto clip space.
    // Orthogonal projections are just that easy.
    // Note however that our target texture has an aspect ratio of 5:6.
    pos.y *= (5./6.);
    pos.z *= (1/1024);
    // Fit onto the [-1,1] NDC.
    pos.xy = 2 * pos.xy - 1;
    o.vertex = pos;
    o.uv = v.uv;
    return o;
}

fixed4 frag (v2f i) : SV_Target {
    float4 col = float4(i.uv, 0, 1);
    if (distance(i.uv, float2(0.5, 0.5)) > 0.5)
        col.rgb = 0;
    return col;
}