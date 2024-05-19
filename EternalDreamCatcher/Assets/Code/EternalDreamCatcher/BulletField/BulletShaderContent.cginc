StructuredBuffer<float3> _BulletPosses;
StructuredBuffer<float4> _BulletReds;
StructuredBuffer<float4> _BulletGreens;
uint _InstanceOffset;

struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f {
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 r : TEXCOORD1;
    float4 g : TEXCOORD2;
};

sampler2D _BulletTex;
float4 _BulletTex_ST;

// Mixes two colors based on the R and G channels of the input color.
// More red in the original color  = more rCol.
// More green in the original color  = more gCol.
float4 ApplyColors(float4 i, float4 rCol, float4 gCol) {
    if (all(i.rg == 0))
        return 0;
    float denominator = i.r + i.g;
    float2 scaled = i.rg / denominator;
    // The alpha of the texture is the _maximum_ alpha.
    // All other colors are discarded.
    i.a *= rCol.a * scaled.r + gCol.a * scaled.g;
    i.rgb = rCol.rgb * scaled.r + gCol.rgb * scaled.g;
    return i;
}

v2f vert (appdata v, uint instanceID : SV_INSTANCEID) {
    instanceID += _InstanceOffset;
    v2f o;
    float4 pos = v.vertex;
    pos.xy *= 0.03;
    pos.xyz += _BulletPosses[instanceID];
    // We're rendering *directly* onto clip space.
    // Orthogonal projections are just that easy.
    // Note however that our target texture has an aspect ratio of 5:6.
    pos.y *= (5./6.);
    // Also note that we don't do Z tests and just draw everything.
    // Ordering is done on the c# side of things by multiple draw calls.
    pos.z *= (1/1024);
    // Fit onto the [-1,1] NDC.
    pos.xy = 2 * pos.xy - 1;
    o.vertex = pos;
    o.uv = v.uv;
    o.r = _BulletReds[instanceID];
    o.g = _BulletGreens[instanceID];
    return o;
}

fixed4 frag (v2f i) : SV_Target {
    float4 col = tex2D(_BulletTex, i.uv);
    col = ApplyColors(col, i.r, i.g);
    col.rgb *= col.a;
    return col;
}