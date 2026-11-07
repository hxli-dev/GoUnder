#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class F1CorridorLighting
{
 const string Root="Assets/EnvironmentArt/F1";
 const string Preview=Root+"/Preview/MainSceneF1_ArtPreview.unity";
 const string Stage=Root+"/Baked/F1_CorridorBake.unity";
 const string Output=@"C:\Users\Hongxin\Documents\Codex\2026-11-06\referenced-chatgpt-conversation-this-is-an-2\outputs\corridor";
 [Serializable] class Item{public string id;public bool baked;public Vector3 position;public int index;public Vector4 offset;public float[] sh;}
 [Serializable] class Job{public Item[] items;public string[] colors,dirs,masks;public double started;}
 static double next;
 static F1CorridorLighting(){Lightmapping.bakeCompleted+=Complete;EditorApplication.update+=Poll;}
 static string Hierarchy(Transform t){return t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;}
 static bool StaticSurface(Renderer r)
 {
  string p=Hierarchy(r.transform);
  return p.Contains("/01 Floor")||p.Contains("/02 West")||p.Contains("/03 East")||p.Contains("/04 Ceiling")||p.Contains("/Integrated end wall");
 }
 [MenuItem("Tools/GoUnder Art/Prepare Corridor Lighting Bake")]
 public static void Prepare()
 {
  var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Need saved art preview Edit Mode.");
  var art=GameObject.Find("F1_EnvironmentArt_Preview").transform;if(art.Find("V4 Lighting rig"))throw new Exception("Lighting rig already prepared.");
  Directory.CreateDirectory(Root+"/Baked");Directory.CreateDirectory(Root+"/Meshes/LightingUV");AssetDatabase.Refresh();
  // Configure scanned normals on the existing five authoring materials; no shared source asset is edited.
  var scan=AssetDatabase.LoadAssetAtPath<Material>("Assets/Apple/3D model/Material/Surfaces/Flaked Paint Wall_vhqkeff/Textures/hallway.mat");
  foreach(var name in new[]{"Salt-grey mineral plaster","Deep petrol wall base","Warm grey ceiling","Basalt terrazzo","Oxidised bronze trim"})
  {
   var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");m.SetTexture("_BumpMap",scan.GetTexture("_BumpMap"));m.SetFloat("_BumpScale",name=="Basalt terrazzo"?.12f:.22f);m.SetFloat("_Wetness",name=="Basalt terrazzo"?.5f:.25f);m.SetFloat("_Smoothness",name=="Oxidised bronze trim"?.4f:.20f);EditorUtility.SetDirty(m);
  }
  var enamel=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/V3 aged green enamel.mat");enamel.SetTexture("_BumpMap",null);enamel.DisableKeyword("_NORMALMAP");EditorUtility.SetDirty(enamel);
  var surfaces=art.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&StaticSurface(r)).ToArray();int index=0;
  foreach(var r in surfaces)
  {
   var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
   var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);var scale=r.transform.localScale;var vertices=mesh.vertices;var normals=mesh.normals;
   for(int i=0;i<vertices.Length;i++){vertices[i]=Vector3.Scale(vertices[i],scale);if(normals.Length==vertices.Length)normals[i]=new Vector3(normals[i].x/scale.x,normals[i].y/scale.y,normals[i].z/scale.z).normalized;}
   mesh.vertices=vertices;if(normals.Length==vertices.Length)mesh.normals=normals;mesh.RecalculateBounds();
   UnwrapParam param;UnwrapParam.SetDefaults(out param);param.packMargin=.015f;Unwrapping.GenerateSecondaryUVSet(mesh,param);
   AssetDatabase.CreateAsset(mesh,Root+"/Meshes/LightingUV/Surface_"+(index++).ToString("000")+".asset");mf.sharedMesh=mesh;r.transform.localScale=Vector3.one;r.receiveGI=ReceiveGI.Lightmaps;
  }
  var rig=new GameObject("V4 Lighting rig").transform;rig.SetParent(art,false);
  // Remove only the earlier art prototype's uniform fills. Original game lights/effects are untouched.
  foreach(var l in art.GetComponentsInChildren<Light>())if(l.transform!=rig&&!Hierarchy(l.transform).Contains("V4 Lighting rig"))l.enabled=false;
  foreach(var r in art.GetComponentsInChildren<Renderer>())if(r.name=="Cove diffuser"||r.name=="Pier light diffuser"||r.name=="Glass edge reflection")r.enabled=false;
  int n=0;
  foreach(int side in new[]{-1,1})foreach(float z in new[]{216f,136f,56f})
  {
   var g=new GameObject("Baked window bounce "+n++);g.transform.SetParent(rig,false);g.transform.position=new Vector3(150+side*21,22,z);g.transform.rotation=Quaternion.LookRotation(Vector3.right*-side+Vector3.down*.12f);
   var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.50f,.67f,.78f);l.intensity=12;l.range=100;l.spotAngle=110;l.innerSpotAngle=75;l.shadows=LightShadows.Soft;l.lightmapBakeType=LightmapBakeType.Baked;
  }
  foreach(float z in new[]{185f,65f})
  {
   var g=new GameObject("Baked practical bounce");g.transform.SetParent(rig,false);g.transform.position=new Vector3(150,35,z);g.transform.rotation=Quaternion.Euler(90,0,0);var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.9f,.64f,.34f);l.intensity=9;l.range=72;l.spotAngle=105;l.shadows=LightShadows.Soft;l.lightmapBakeType=LightmapBakeType.Baked;
  }
  // A restrained real-time source keeps moving papers and the door connected to visible window light.
  foreach(float z in new[]{210f,105f,0f})
  {
   var g=new GameObject("Realtime window key");g.transform.SetParent(rig,false);g.transform.position=new Vector3(169,24,z);g.transform.rotation=Quaternion.LookRotation(new Vector3(-1,-.2f,-.3f));var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.50f,.66f,.76f);l.intensity=9;l.range=75;l.spotAngle=100;l.shadows=LightShadows.Soft;l.shadowBias=.025f;l.shadowNormalBias=.15f;
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("V4 UV2 and lighting rig prepared; original gameplay lights unmodified. Static renderers: "+surfaces.Length);
 }
 [MenuItem("Tools/GoUnder Art/Start Independent Corridor Bake")]
 public static void StartBake()
 {
  var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||scene.isDirty||Lightmapping.isRunning)throw new Exception("Expected saved preview with no active bake.");
  var art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
  var moving=GameObject.Find("NaiveDoor (6)/Door3.1Pivot/DoorVisual_DistressedSteel");
  var targets=art.GetComponentsInChildren<MeshRenderer>().Concat(moving.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponent<MeshFilter>().sharedMesh&& !Hierarchy(r.transform).Contains("/05 Window exterior")&&!Hierarchy(r.transform).Contains("Drowned coastal district")).ToArray();
  var items=targets.Select(r=>new Item{id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),baked=StaticSurface(r),position=r.bounds.center}).ToArray();
  var stage=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(stage);
  for(int i=0;i<targets.Length;i++)
  {
   if(!items[i].baked)continue;var source=targets[i];var go=new GameObject("BAKE_"+i);go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);go.transform.localScale=source.transform.lossyScale;go.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=source.sharedMaterials;r.receiveGI=ReceiveGI.Lightmaps;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI);
  }
  foreach(var source in art.Find("V4 Lighting rig").GetComponentsInChildren<Light>().Where(l=>l.lightmapBakeType==LightmapBakeType.Baked))
  {var go=new GameObject(source.name);go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);var light=go.AddComponent<Light>();EditorUtility.CopySerialized(source,light);}
  var pg=new GameObject("Corridor probe sampling").AddComponent<LightProbeGroup>();var positions=new List<Vector3>();
  foreach(float z in new[]{-43f,0f,45f,90f,135f,180f,225f,244f})foreach(float x in new[]{130f,150f,170f})foreach(float y in new[]{7f,18f,31f})positions.Add(new Vector3(x,y,z));pg.probePositions=positions.ToArray();
  RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.045f,.065f,.085f);RenderSettings.ambientIntensity=.4f;
  var settings=new LightingSettings();settings.bakedGI=true;settings.realtimeGI=false;settings.autoGenerate=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;settings.lightmapResolution=1.25f;settings.lightmapMaxSize=1024;settings.lightmapPadding=4;settings.directSampleCount=32;settings.indirectSampleCount=128;settings.environmentSampleCount=64;settings.maxBounces=3;settings.ao=true;settings.aoMaxDistance=1.1f;settings.aoExponentIndirect=.6f;settings.directionalityMode=LightmapsMode.CombinedDirectional;
  string settingsPath=Root+"/Baked/Corridor_LightingSettings.lighting";if(AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath))AssetDatabase.DeleteAsset(settingsPath);AssetDatabase.CreateAsset(settings,settingsPath);Lightmapping.lightingSettings=settings;Lightmapping.lightingDataAsset=null;
  EditorSceneManager.SaveScene(stage,Stage);EditorSceneManager.CloseScene(scene,true);
  var job=new Job{items=items,started=EditorApplication.timeSinceStartup};File.WriteAllText(Path.Combine(Output,"v4-bake-job.json"),JsonUtility.ToJson(job,true));SessionState.SetBool("GoUnder.Art.Baking",true);
  if(!Lightmapping.BakeAsync())throw new Exception("Unity refused BakeAsync.");Debug.Log("Independent corridor bake started (CPU, 32 direct / 128 indirect samples).");
 }
 static void Poll()
 {
  if(!SessionState.GetBool("GoUnder.Art.Baking",false)||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+10;
  File.WriteAllText(Path.Combine(Output,"v4-bake-progress.json"),"{\"running\":"+Lightmapping.isRunning.ToString().ToLowerInvariant()+",\"progress\":"+Lightmapping.buildProgress.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
 }
 [MenuItem("Tools/GoUnder Art/Fix Corridor Lightmap Charts")]
 public static void FixCharts()
 {
  SessionState.SetBool("GoUnder.Art.Baking",false);if(Lightmapping.isRunning)Lightmapping.Cancel();
  if(EditorSceneManager.GetActiveScene().path!=Preview)EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
  var art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
  var roots=new[]{art.Find("01 Floor - existing collision retained"),art.Find("02 West window wall"),art.Find("03 East window wall"),art.Find("04 Ceiling and progressive structural drift"),art.Find("V3 Door and document details/Integrated end wall and fixed door surround")};int index=0;
  AssetDatabase.StartAssetEditing();
  try
  {
   foreach(var root in roots)
   {
    var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)continue;
    var pieces=new List<CombineInstance>();var materials=new List<Material>();var temporary=new List<Mesh>();
    foreach(var group in rs.GroupBy(r=>r.sharedMaterial))
    {
     var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);temporary.Add(mesh);pieces.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});materials.Add(group.Key);
    }
    var combined=new Mesh{name="Corridor area lighting charts",indexFormat=IndexFormat.UInt32};combined.CombineMeshes(pieces.ToArray(),false,true);
    UnwrapParam param;UnwrapParam.SetDefaults(out param);param.packMargin=.025f;Unwrapping.GenerateSecondaryUVSet(combined,param);
    string path=Root+"/Meshes/LightingUV/Area_"+(index++)+".asset";AssetDatabase.CreateAsset(combined,path);
    var go=new GameObject("Static area with separated lightmap charts");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=combined;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials.ToArray();renderer.receiveGI=ReceiveGI.Lightmaps;renderer.allowOcclusionWhenDynamic=false;
    foreach(var r in rs)r.enabled=false;foreach(var mesh in temporary)UnityEngine.Object.DestroyImmediate(mesh);
   }
  }
  finally{AssetDatabase.StopAssetEditing();}
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("Static corridor geometry regrouped into five areas with padded UV2 charts; shape and collision unchanged.");
 }
 static void Complete()
 {
  if(!SessionState.GetBool("GoUnder.Art.Baking",false))return;
  SessionState.SetBool("GoUnder.Art.Baking",false);
  try
  {
   var job=JsonUtility.FromJson<Job>(File.ReadAllText(Path.Combine(Output,"v4-bake-job.json")));var maps=LightmapSettings.lightmaps;
   if(maps.Length==0)throw new Exception("Bake completed without lightmaps.");
   for(int i=0;i<job.items.Length;i++)
   {
    var item=job.items[i];
    if(item.baked){var r=GameObject.Find("BAKE_"+i).GetComponent<Renderer>();item.index=r.lightmapIndex;item.offset=r.lightmapScaleOffset;}
    else {SphericalHarmonicsL2 sh;LightProbes.GetInterpolatedProbe(item.position,null,out sh);item.sh=new float[27];for(int c=0;c<3;c++)for(int j=0;j<9;j++)item.sh[c*9+j]=sh[c,j];}
   }
   job.colors=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapColor)).ToArray();job.dirs=maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapDir)).ToArray();job.masks=maps.Select(m=>AssetDatabase.GetAssetPath(m.shadowMask)).ToArray();
   File.WriteAllText(Path.Combine(Output,"v4-bake-result.json"),JsonUtility.ToJson(job,true));EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
   EditorApplication.delayCall+=()=>Apply(job);
  }
  catch(Exception e){File.WriteAllText(Path.Combine(Output,"v4-bake-error.txt"),e.ToString());Debug.LogException(e);}
 }
 static void Apply(Job job)
 {
  EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);var art=GameObject.Find("F1_EnvironmentArt_Preview");var binding=art.GetComponent<F1BakedLightingBinding>();if(!binding)binding=art.AddComponent<F1BakedLightingBinding>();
  binding.maps=job.colors.Select((p,i)=>new F1BakedLightingBinding.Map{color=AssetDatabase.LoadAssetAtPath<Texture2D>(p),direction=AssetDatabase.LoadAssetAtPath<Texture2D>(job.dirs[i]),shadowMask=AssetDatabase.LoadAssetAtPath<Texture2D>(job.masks[i])}).ToArray();
  var surfaces=new List<F1BakedLightingBinding.Surface>();var probes=new List<F1BakedLightingBinding.Probe>();
  foreach(var item in job.items){GlobalObjectId id;if(!GlobalObjectId.TryParse(item.id,out id))continue;var r=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Renderer;if(!r)continue;if(item.baked)surfaces.Add(new F1BakedLightingBinding.Surface{renderer=r,index=item.index,scaleOffset=item.offset});else probes.Add(new F1BakedLightingBinding.Probe{renderer=r,coefficients=item.sh});}
  binding.surfaces=surfaces.ToArray();binding.probes=probes.ToArray();binding.Apply();EditorUtility.SetDirty(binding);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  File.WriteAllText(Path.Combine(Output,"v4-bake-summary.txt"),"Lightmaps: "+binding.maps.Length+"\nBound static renderers: "+surfaces.Count+"\nDynamic baked probe samples: "+probes.Count+"\nExisting scene lightmaps retained; original source lighting assets untouched.\nElapsed seconds: "+(EditorApplication.timeSinceStartup-job.started).ToString("F1"));Debug.Log("Independent corridor lighting applied: "+surfaces.Count+" surfaces, "+probes.Count+" dynamic samples.");
 }
 [MenuItem("Tools/GoUnder Art/Apply Completed Corridor Bake")]
 public static void ApplyCompletedBake(){if(Lightmapping.isRunning)throw new Exception("Bake still running.");Apply(JsonUtility.FromJson<Job>(File.ReadAllText(Path.Combine(Output,"v4-bake-result.json"))));}
 [MenuItem("Tools/GoUnder Art/Tune Corridor Light Energy")]
 public static void TuneEnergy()
 {
  var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview)throw new Exception("Expected preview.");
  var rig=GameObject.Find("F1_EnvironmentArt_Preview/V4 Lighting rig");
  foreach(var l in rig.GetComponentsInChildren<Light>()){if(l.name.StartsWith("Baked window")){l.intensity=l.transform.position.x>150?60:16;l.color=new Color(.72f,.79f,.84f);}else if(l.name.StartsWith("Baked practical")){l.intensity=45;l.color=new Color(.9f,.76f,.55f);}else if(l.name=="Realtime window key")l.intensity=14;}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("Window and practical energy calibrated to the project's enlarged world scale.");
 }
 [MenuItem("Tools/GoUnder Art/Finish Corridor Reflections")]
 public static void FinishReflections()
 {
  var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview)throw new Exception("Expected preview.");
  var art=GameObject.Find("F1_EnvironmentArt_Preview").transform;var binding=art.GetComponent<F1BakedLightingBinding>();binding.Apply();
  foreach(var old in art.GetComponentsInChildren<ReflectionProbe>())old.enabled=false;
  var rig=art.Find("V4 Lighting rig");int index=0;
  foreach(float z in new[]{210f,110f,0f})
  {
   string name="Local corridor reflection "+index;var t=rig.Find(name);if(!t){var go=new GameObject(name);t=go.transform;t.SetParent(rig,false);go.AddComponent<ReflectionProbe>();}
   t.position=new Vector3(150,16,z);var p=t.GetComponent<ReflectionProbe>();p.enabled=true;p.resolution=128;p.size=new Vector3(50,40,120);p.center=new Vector3(0,3,0);p.boxProjection=true;p.blendDistance=8;p.intensity=.55f;p.farClipPlane=180;p.mode=ReflectionProbeMode.Baked;
   string path=Root+"/Baked/CorridorReflection_"+index+++".exr";
   if(!Lightmapping.BakeReflectionProbe(p,path))throw new Exception("Reflection capture failed.");AssetDatabase.ImportAsset(path);p.mode=ReflectionProbeMode.Custom;p.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("Three local static reflection captures completed; existing source probe assets unchanged.");
 }
 [MenuItem("Tools/GoUnder Art/Capture Corridor Lighting")]
 public static void CaptureLighting()
 {
  var art=GameObject.Find("F1_EnvironmentArt_Preview");var binding=art.GetComponent<F1BakedLightingBinding>();if(binding)binding.Apply();var camera=art.transform.Find("F1_Art_ReviewCamera").GetComponent<Camera>();var source=Camera.main;var data=camera.GetUniversalAdditionalCameraData();bool post=data.renderPostProcessing;
  foreach(var t in art.GetComponentsInChildren<ReflectionProbe>())if(t.enabled&&t.mode==ReflectionProbeMode.Realtime)t.RenderProbe();
  try{camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,240),Quaternion.Euler(0,180,0));Shot(camera,"v4-entry-game.png");data.renderPostProcessing=false;Shot(camera,"v4-entry-detail.png");camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,110),Quaternion.Euler(0,180,0));Shot(camera,"v4-depth-detail.png");camera.transform.SetPositionAndRotation(new Vector3(155,14,-29),Quaternion.LookRotation(new Vector3(-5,-1,-20)));Shot(camera,"v4-door-detail.png");}
  finally{data.renderPostProcessing=post;camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());}
 }
 static void Shot(Camera c,string file){var rt=RenderTexture.GetTemporary(1600,1000,24);var old=RenderTexture.active;var target=c.targetTexture;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Output,file),tex.EncodeToPNG());}finally{c.targetTexture=target;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 [MenuItem("Tools/GoUnder Art/Verify Corridor Lighting Persistence")]
 public static void VerifyPersistence()
 {
  var art=GameObject.Find("F1_EnvironmentArt_Preview");var binding=art.GetComponent<F1BakedLightingBinding>();var maps=LightmapSettings.lightmaps;int valid=0;
  foreach(var s in binding.surfaces)if(s.renderer&&s.renderer.lightmapIndex>=0&&s.renderer.lightmapIndex<maps.Length&&maps[s.renderer.lightmapIndex].lightmapColor==binding.maps[s.index].color&&s.renderer.lightmapScaleOffset==s.scaleOffset)valid++;
  int refs=art.GetComponentsInChildren<ReflectionProbe>().Count(p=>p.enabled&&p.mode==ReflectionProbeMode.Custom&&p.customBakedTexture);
  var text="Baked maps: "+binding.maps.Length+"\nPersisted mapped areas: "+valid+" / "+binding.surfaces.Length+"\nDynamic SH samples: "+binding.probes.Length+"\nLocal reflection captures: "+refs+"\n";
  File.WriteAllText(Path.Combine(Output,"v4-persistence.txt"),text);Debug.Log(text);if(valid!=binding.surfaces.Length)throw new Exception("Some lightmap bindings did not survive reload.");
 }
}
#endif
