#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class F1DarkRoomAudit
{
 const string Output=@"C:\Users\Hongxin\Documents\Codex\2026-11-06\referenced-chatgpt-conversation-this-is-an-2\outputs\corridor";
 static string PathOf(Transform t){return t.parent?PathOf(t.parent)+"/"+t.name:t.name;}
 [MenuItem("Tools/GoUnder Art/Exclude Dark Shell Environment Reflections")]
 public static void Fix()
 {
  var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/EnvironmentArt/F1/Preview/MainSceneF1_ArtPreview.unity")throw new Exception("Saved art preview Edit Mode required");
  var shell=GameObject.Find("F1/Room/Walls");var renderers=shell.GetComponentsInChildren<Renderer>(true);var sb=new StringBuilder();
  foreach(var r in renderers){sb.AppendLine(PathOf(r.transform)+": "+r.reflectionProbeUsage+" -> Off");Undo.RecordObject(r,"Exclude dark room shell from reflections");r.reflectionProbeUsage=ReflectionProbeUsage.Off;EditorUtility.SetDirty(r);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(System.IO.Path.Combine(Output,"dark-room-fix.txt"),sb.ToString());Debug.Log("Disabled environment reflections only on "+renderers.Length+" enclosure renderers. Materials, objects, lights and triggers unchanged.");
 }
 [MenuItem("Tools/GoUnder Art/Audit Dark Room")]
 public static void Audit()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
  var room=GameObject.Find("F1/Room");var art=GameObject.Find("F1_EnvironmentArt_Preview");
  var sb=new StringBuilder();
  foreach(var r in room.GetComponentsInChildren<Renderer>(true))if(r.gameObject.activeInHierarchy){sb.AppendLine(PathOf(r.transform)+" bounds="+r.bounds+" layer="+r.gameObject.layer+" reflection="+r.reflectionProbeUsage+" lightmap="+r.lightmapIndex);foreach(var m in r.sharedMaterials)if(m)sb.AppendLine("  "+AssetDatabase.GetAssetPath(m)+" shader="+m.shader.name+" color="+(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():""));}
  foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())sb.AppendLine("LIGHT "+PathOf(l.transform)+" enabled="+l.enabled+" pos="+l.transform.position+" range="+l.range+" intensity="+l.intensity+" mask="+l.cullingMask);
  foreach(var p in UnityEngine.Object.FindObjectsOfType<ReflectionProbe>())sb.AppendLine("PROBE "+PathOf(p.transform)+" enabled="+p.enabled+" bounds="+p.bounds+" intensity="+p.intensity);
  var lights=art.GetComponentsInChildren<Light>().Where(l=>l.enabled).ToArray();var probes=art.GetComponentsInChildren<ReflectionProbe>().Where(p=>p.enabled).ToArray();
  var go=new GameObject("Temporary dark room audit camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
  try{
   foreach(float z in new[]{-80f,-180f,-300f}){
    camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,z),Quaternion.Euler(8,180,0));
    Shot(camera,"dark-"+(-z)+"-current",sb);
    foreach(var p in probes)p.enabled=false;Shot(camera,"dark-"+(-z)+"-no-art-reflection",sb);
    foreach(var l in lights)l.enabled=false;Shot(camera,"dark-"+(-z)+"-no-art-light-or-reflection",sb);
    var rs=room.transform.Find("Walls").GetComponentsInChildren<Renderer>();var rp=rs.Select(r=>r.reflectionProbeUsage).ToArray();var lp=rs.Select(r=>r.lightProbeUsage).ToArray();
    try{foreach(var r in rs)r.reflectionProbeUsage=ReflectionProbeUsage.Off;Shot(camera,"dark-"+(-z)+"-no-shell-specular",sb);foreach(var r in rs)r.lightProbeUsage=LightProbeUsage.Off;Shot(camera,"dark-"+(-z)+"-no-shell-probes",sb);}
    finally{for(int i=0;i<rs.Length;i++){rs[i].reflectionProbeUsage=rp[i];rs[i].lightProbeUsage=lp[i];}}
    foreach(var p in probes)p.enabled=true;foreach(var l in lights)l.enabled=true;
   }
  }finally{foreach(var p in probes)p.enabled=true;foreach(var l in lights)l.enabled=true;UnityEngine.Object.DestroyImmediate(go);}
  File.WriteAllText(System.IO.Path.Combine(Output,"dark-room-audit.txt"),sb.ToString());Debug.Log("Dark room audit completed; temporary changes restored.");
 }
 static void Shot(Camera c,string name,StringBuilder sb){var rt=RenderTexture.GetTemporary(800,500,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(800,500,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,800,500),0,0);t.Apply();var pixels=t.GetPixels32();sb.AppendLine(name+" mean="+pixels.Average(p=>(p.r+p.g+p.b)/3.0)+" max="+pixels.Max(p=>Math.Max(p.r,Math.Max(p.g,p.b))));File.WriteAllBytes(System.IO.Path.Combine(Output,name+".png"),t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}finally{c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}}
}
#endif
