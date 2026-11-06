Shader "GoUnder/Art/Thin Research Paper"
{
 Properties{_BaseMap("Printed paper",2D)="white"{} _BumpMap("Crease normal",2D)="bump"{} _BumpScale("Crease strength",Float)=.4}
 SubShader
 {
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass
 {
 Tags{"LightMode"="UniversalForward"} Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;float _BumpScale;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half4 t:TEXCOORD2;float2 uv:TEXCOORD3;};
 V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.t=half4(TransformObjectToWorldDir(i.t.xyz),i.t.w*GetOddNegativeScale());o.uv=i.uv;return o;}
 half4 frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
 {
  bool front=IS_FRONT_VFACE(face,true,false);float sign=front?1:-1;
  half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
  if(!front)albedo=lerp(half3(.84,.82,.74),albedo,.14);
  half3 ts=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
  half3 n=normalize(i.n)*sign,t=normalize(i.t.xyz),b=cross(n,t)*i.t.w;
  InputData data=(InputData)0;data.positionWS=i.w;data.normalWS=normalize(t*ts.x+b*ts.y+n*ts.z);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);data.shadowCoord=TransformWorldToShadowCoord(i.w);data.bakedGI=max(SampleSH(data.normalWS),half3(.035,.04,.038));data.shadowMask=half4(1,1,1,1);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
  SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=0;s.specular=.04;s.smoothness=.08;s.normalTS=ts;s.occlusion=1;s.alpha=1;
  half4 color=UniversalFragmentPBR(data,s);Light light=GetMainLight(data.shadowCoord);color.rgb+=albedo*light.color*saturate(-dot(n,light.direction))*.12;
  return color;
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
