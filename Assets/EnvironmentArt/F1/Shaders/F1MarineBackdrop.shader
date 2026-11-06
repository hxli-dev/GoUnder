Shader "GoUnder/Art/Marine Backdrop"
{
 Properties { _BaseColor("Base colour",Color)=(.1,.18,.22,1) _Mode("Sea 0, sky 1, architecture 2",Float)=0 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 p:POSITION; }; struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor; float _Mode;
   CBUFFER_END
   V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   half4 frag(V i):SV_Target
   {
    float3 fog=float3(.17,.24,.27),c=_BaseColor.rgb;
    float d=distance(i.w.xz,_WorldSpaceCameraPos.xz);
    if(_Mode<.5)
    {
      float2 p=i.w.xz;float t=_Time.y*.08;
      float wave=sin(p.x*.29+p.y*.12+t)*sin(p.y*.43-p.x*.08-t*.7);
      float fine=sin(p.x*1.7+p.y*.42+t)*.25+noise(p*.19)*.5;
      c=lerp(float3(.035,.09,.12),fog,saturate(d/650));
      c+=(wave*.016+fine*.012)*float3(.6,.9,1);
      float3 dir=normalize(i.w-_WorldSpaceCameraPos);
      c+=pow(saturate(wave*.5+.5),30)*pow(saturate(1-abs(dir.x*.5+dir.z*.7)),6)*.07*float3(.7,.85,.9);
    }
    else if(_Mode<1.5)
    {
      float h=saturate((i.w.y-4)/130);float2 uv=float2(i.w.x+i.w.z*.6,i.w.y*2)*.012;
      float clouds=noise(uv)*.55+noise(uv*2.1)*.3+noise(uv*4.2)*.15;
      c=lerp(fog,float3(.055,.09,.13),h)+(clouds-.5)*.045;
    }
    else
    {
      c*=.8+noise(i.w.xz*.08)*.2;c=lerp(c,fog,saturate((d-80)/600)*.85);
      c=lerp(c,fog,saturate((10-i.w.y)/18)*.25);
    }
    return half4(c,1);
   }
   ENDHLSL
  }
 }
}
