#include "UnityCG.cginc"

sampler2D _MainTex;

v2f_img VertDebug(appdata_img v)
{
    // a little bit messy way to fit the quad to the screen
    v.vertex.z -= 1;
    v2f_img o;
    o.pos = UnityViewToClipPos(v.vertex);
    o.pos.xy /= abs(o.pos.xy);
    o.uv = v.texcoord;
    return o;
}

fixed4 FragDebug(v2f_img i) : SV_Target
{
    return tex2D(_MainTex, i.uv.xy);
}
