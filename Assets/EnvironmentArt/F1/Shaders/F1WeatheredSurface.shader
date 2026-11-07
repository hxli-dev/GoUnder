Shader "GoUnder/Art/Weathered Surface"
{
 Properties
 {
 _BaseColor("Paint",Color)=(.4,.43,.4,1) _BaseMap("Scanned surface",2D)="white"{}
 _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=.25
 _Weathering("Weathering",Range(0,1))=.7 _Scale("World texture scale",Float)=.1
 _Metallic("Metal",Range(0,1))=0 _Smoothness("Smoothness",Range(0,1))=.22 _Wetness("Damp sheen",Range(0,1))=.3
 }
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor,_BaseMap_ST;float _Weathering,_Scale,_Metallic,_Smoothness,_Wetness,_BumpScale;
 CBUFFER_END
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 float fbm(float2 p){return noise(p)*.57+noise(p*2.17)*.28+noise(p*5.1)*.15;}
 float2 SurfaceUV(float3 w,float3 n){return abs(n.y)>.65?w.xz:(abs(n.x)>.6?w.zy:w.xy);}
 half3 SurfaceColor(float3 w,float3 n,out half damp)
 {
 float2 uv=SurfaceUV(w,n),p=uv*_Scale;
 half3 a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p).rgb,b=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(-p.y,p.x)*.73+float2(.37,.11)).rgb;
 half3 scan=lerp(a,b,noise(uv*.06)*.75);
 float patch=fbm(uv*.10+4),streak=noise(float2(uv.x*.65,uv.y*.022));damp=saturate((15-w.y)/12+.35*(patch-.5));
 float drip=smoothstep(.52,.8,streak)*smoothstep(.38,.65,patch),chip=smoothstep(.62,.75,fbm(uv*.5+noise(uv*.05)*3));
 half3 c=_BaseColor.rgb*lerp(half3(.65,.65,.65),scan,.65)*(.88+fbm(uv*.35)*.20);
 c=lerp(c,c*half3(.33,.40,.34),saturate(damp*.55+drip*.65+chip*.3)*_Weathering);return lerp(c,half3(.18,.17,.13),chip*_Weathering*.36);
 }
 ENDHLSL
 SubShader
 {
 Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass
 {
 Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ LIGHTMAP_ON
 #pragma multi_compile _ DIRLIGHTMAP_COMBINED
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 lm:TEXCOORD1;};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;DECLARE_LIGHTMAP_OR_SH(lm,sh,2);half fog:TEXCOORD3;};
 V vert(A i){V o=(V)0;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);OUTPUT_LIGHTMAP_UV(i.lm,unity_LightmapST,o.lm);OUTPUT_SH(o.n,o.sh);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target
 {
 half3 geom=normalize(i.n);float2 uv=SurfaceUV(i.w,geom)*_Scale;half damp;half3 albedo=SurfaceColor(i.w,geom,damp);
 half3 ts=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv),_BumpScale);
 float3 dp1=ddx(i.w),dp2=ddy(i.w);float2 du1=ddx(uv),du2=ddy(uv);
 float3 t=normalize(dp1*du2.y-dp2*du1.y),b=normalize(-dp1*du2.x+dp2*du1.x);half3 n=normalize(t*ts.x+b*ts.y+geom*ts.z);
 InputData data=(InputData)0;data.positionWS=i.w;data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);data.shadowCoord=TransformWorldToShadowCoord(i.w);data.bakedGI=SAMPLE_GI(i.lm,i.sh,n);data.shadowMask=SAMPLE_SHADOWMASK(i.lm);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);data.fogCoord=i.fog;
 SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=_Metallic;s.specular=.04;s.smoothness=saturate(_Smoothness+damp*_Wetness*.28);s.occlusion=1;s.normalTS=ts;s.alpha=1;
 half4 color=UniversalFragmentPBR(data,s);color.rgb=MixFog(color.rgb,i.fog);return color;
 }
 ENDHLSL
 }
 Pass
 {
 Name "Meta" Tags{"LightMode"="Meta"} Cull Off
 HLSLPROGRAM
 #pragma vertex metaVert
 #pragma fragment metaFrag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
 struct MA{float4 p:POSITION;float3 n:NORMAL;float2 uv1:TEXCOORD1;float2 uv2:TEXCOORD2;};struct MV{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;};
 MV metaVert(MA i){MV o;o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.p=MetaVertexPosition(i.p,i.uv1,i.uv2,unity_LightmapST,unity_DynamicLightmapST);return o;}
 half4 metaFrag(MV i):SV_Target{half damp;MetaInput m=(MetaInput)0;m.Albedo=SurfaceColor(i.w,normalize(i.n),damp)*(1-_Metallic);return MetaFragment(m);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
