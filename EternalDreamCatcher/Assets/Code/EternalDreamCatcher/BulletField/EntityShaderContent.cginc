// Much of this is duplicated from BulletShaderContent.cginc.
// x,y,z: pos; w: scale
float4 _EntityPosScale;

struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f {
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

sampler2D _EntityTex;
float4 _EntityTex_TexelSize;

v2f vert (appdata v) {
    v2f o;
    float4 pos = v.vertex;

    pos.xy *= 0.12 * _EntityPosScale.w;
    pos.xyz += _EntityPosScale.xyz;

    // uhhh idk whether this is because of my specific setup or a general
    // consequence of the "invert y on some platforms" phenomenon.
    // If the latter, I'll see some fun bug reports in a far future.
    v.uv.y = 1-v.uv.y;
    
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
    return o;
}

fixed4 frag (v2f i) : SV_Target {
    // Animate at 8fps
    // Assuming each frame is a square texture, and they're in a row.
    float frame_count = _EntityTex_TexelSize.z / _EntityTex_TexelSize.w;
    float2 uv = i.uv;
    uv.x += floor(_Time.y * 8);
    uv.x = frac(uv.x / frame_count);

    // Apply the mapping from R/G to actual colors.
    float4 col = tex2D(_EntityTex, uv);

    col.rgb *= col.a;
    return col;
}