Shader "GoUnder/Art/Weathered Surface"
{
 Properties { _BaseColor("Paint",Color)=(.4,.43,.4,1) _BaseMap("Scanned surface",2D)="white"{} _Weathering("Weathering",Range(0,1))=.7 _Scale("World texture scale",Float)=.1 _Metallic("Metal",Range(0,1))=0 }
 SubShader
 {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass
 {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;float4 _BaseMap_ST;float _Weathering,_Scale,_Metallic;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
 V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 float fbm(float2 p){return noise(p)*.57+noise(p*2.17)*.28+noise(p*5.1)*.15;}
 half4 frag(V i):SV_Target
 {
 float3 n=normalize(i.n);float2 uv=abs(n.y)>.65?i.w.xz:(abs(n.x)>.6?i.w.zy:i.w.xy);
 float2 p=uv*_Scale;
 float3 scanA=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p).rgb;
 float3 scanB=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(-p.y,p.x)*.73+float2(.37,.11)).rgb;
 float3 scan=lerp(scanA,scanB,noise(uv*.06)*.75);
 float grit=fbm(uv*.35),patch=fbm(uv*.10+4),streak=noise(float2(uv.x*.65,uv.y*.022));
 float damp=saturate((15-i.w.y)/12+.35*(patch-.5));
 float drip=smoothstep(.52,.80,streak)*smoothstep(.38,.65,patch);
 float chip=smoothstep(.62,.75,fbm(uv*.5+noise(uv*.05)*3));
 float weather=saturate(damp*.55+drip*.65+chip*.3)*_Weathering;
 float3 albedo=_BaseColor.rgb*lerp(float3(.65,.65,.65),scan, .65)*( .88+grit*.20);
 albedo=lerp(albedo,albedo*float3(.33,.40,.34),weather);
 albedo=lerp(albedo,float3(.18,.17,.13),chip*_Weathering*.36);
 float tide=exp(-abs(i.w.y-(9.5+noise(float2(uv.x*.08,0))*.7))*2.2)*damp*_Weathering;
 albedo+=tide*float3(.035,.034,.025);
 float3 view=GetWorldSpaceNormalizeViewDir(i.w);float3 illum=float3(.09,.11,.12);
 Light main=GetMainLight(TransformWorldToShadowCoord(i.w));
 illum+=main.color*saturate(dot(n,main.direction))*main.shadowAttenuation;
 #ifdef _ADDITIONAL_LIGHTS
 uint count=GetAdditionalLightsCount();
 for(uint j=0;j<count;j++){Light l=GetAdditionalLight(j,i.w);illum+=l.color*saturate(dot(n,l.direction))*l.distanceAttenuation*l.shadowAttenuation;}
 #endif
 float edge=pow(1-saturate(dot(n,view)),4)*.015;
 return half4(albedo*illum+edge,1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
