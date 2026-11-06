#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class F1ArtWorkbench
{
    const string Output = @"C:\Users\Hongxin\Documents\Codex\2026-11-06\referenced-chatgpt-conversation-this-is-an-2\outputs\corridor";
    static string Request { get { return Path.GetFullPath("Library/F1ArtRequest.txt"); } }
    static double next;
    static F1ArtWorkbench() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        next = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request)) return;
        var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
        try { Directory.CreateDirectory(Output); if (command == "audit") Audit(); }
        catch (Exception e) { File.WriteAllText(Path.Combine(Output,"workbench-error.txt"), e.ToString()); Debug.LogException(e); }
    }
    [Serializable] public class Record
    {
        public string path,asset; public bool active; public Vector3 position,scale,center,size;
        public string[] components,materials; public long id;
    }
    [Serializable] public class Report { public string scene; public bool dirty; public Record[] objects; }
    static string PathOf(Transform t) { return t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name; }
    static void Audit()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.path.EndsWith("MainSceneF1.unity")) throw new Exception("Expected MainSceneF1, got "+scene.path);
        var rows = new List<Record>();
        foreach (var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        {
            var r=t.GetComponent<Renderer>(); var mf=t.GetComponent<MeshFilter>();
            rows.Add(new Record { path=PathOf(t),active=t.gameObject.activeInHierarchy,position=t.position,scale=t.lossyScale,
                center=r?r.bounds.center:Vector3.zero,size=r?r.bounds.size:Vector3.zero,
                asset=mf&&mf.sharedMesh?AssetDatabase.GetAssetPath(mf.sharedMesh):"",
                components=t.GetComponents<Component>().Select(c=>c?c.GetType().Name:"MISSING").ToArray(),
                materials=r?r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m):"NULL").ToArray():new string[0],
                id=GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId > long.MaxValue ? 0 : (long)GlobalObjectId.GetGlobalObjectIdSlow(t).targetObjectId });
        }
        File.WriteAllText(Path.Combine(Output,"scene-audit.json"),JsonUtility.ToJson(new Report {scene=scene.path,dirty=scene.isDirty,objects=rows.ToArray()},true));
        var camera=Camera.main;
        if(camera) Render(camera,"before.png");
        Debug.Log("F1 art audit complete; scene unchanged.");
    }
    static void Render(Camera source,string name)
    {
        var go=new GameObject("Art capture temporary camera"); go.hideFlags=HideFlags.HideAndDontSave;
        var c=go.AddComponent<Camera>(); c.CopyFrom(source); c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        var rt=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32); var old=RenderTexture.active;
        Texture2D image=null;
        try { c.targetTexture=rt; c.Render(); RenderTexture.active=rt; image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); image.Apply(); File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG()); }
        finally { RenderTexture.active=old; c.targetTexture=null; RenderTexture.ReleaseTemporary(rt); if(image) UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif
