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

public static partial class F1CorridorArt
{
    static Material v3Paint,v3Iron,v3Brass,v3Wall,v3Base;
    [MenuItem("Tools/GoUnder Art/Build Door And Paper Detail")]
    public static void DoorPaperDetail()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=Preview||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Need saved art preview in Edit Mode.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
        if(art.Find("V3 Door and document details"))throw new Exception("V3 already present.");
        var originals=scene.GetRootGameObjects().Where(g=>g!=art.gameObject).SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c).ToArray();
        var behavior=originals.Where(c=>c is Collider||c is MonoBehaviour||c is Animator||c is AudioSource||c is Light).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
        var transforms=originals.OfType<Transform>().ToDictionary(t=>t,t=>new[]{t.localPosition,t.localEulerAngles,t.localScale});
        Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory(Root+"/Prefabs");
        var g=Group("V3 Door and document details");
        PrepareDoorMaterials(); BuildEndWall(g);BuildMovingDoor(g);BuildDocuments(g);
        var modified=behavior.Where(k=>!k.Key||EditorJsonUtility.ToJson(k.Key)!=k.Value).Select(k=>k.Key?PathOf(k.Key.transform)+":"+k.Key.GetType().Name:"Destroyed").ToArray();
        if(modified.Length>0)throw new Exception("Functional component changed: "+string.Join(";",modified));
        if(transforms.Any(k=>k.Key.localPosition!=k.Value[0]||k.Key.localEulerAngles!=k.Value[1]||k.Key.localScale!=k.Value[2]))throw new Exception("Original transform changed.");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Output,"v3-protection.txt"),"Original functional components verified unchanged: "+behavior.Count+"\nOriginal local transforms verified unchanged: "+transforms.Count+"\nOriginal door axis, controller, scripts, collision, audio and shared source materials unchanged.\nNew moving visual is a separate prefab child of original pivot.\nNew papers decorative only.\n");
        CaptureV3();Debug.Log("V3 door, end wall and authored research documents created; original functional components unchanged.");
    }
    static Material Pbr(string name,Color color,float metalness,float smoothness,Material source=null)
    {
        var path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("Universal Render Pipeline/Lit");m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metalness);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Cull",2);
        if(source)
        {
            m.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));var bump=source.GetTexture("_BumpMap");
            if(bump){m.SetTexture("_BumpMap",bump);m.SetFloat("_BumpScale",.22f);m.EnableKeyword("_NORMALMAP");}
        }
        m.enableInstancing=true;return m;
    }
    static void PrepareDoorMaterials()
    {
        var iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/Apple/3D model/Material/Surfaces/Scratched Painted Iron_scksebop/Textures/doorFrame1Naive.mat");
        var wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/Apple/3D model/Material/Surfaces/Flaked Paint Wall_vhqkeff/Textures/hallway.mat");
        v3Paint=Pbr("V3 aged green enamel",new Color(.27f,.34f,.30f),.12f,.25f,iron);
        v3Iron=Pbr("V3 dark steel fittings",new Color(.17f,.19f,.17f),.75f,.32f,iron);
        v3Brass=Pbr("V3 worn brass hardware",new Color(.38f,.29f,.16f),.8f,.38f,iron);
        v3Wall=Pbr("V3 door wall mineral paint",new Color(.45f,.50f,.46f),0,.12f,wall);
        v3Base=Pbr("V3 dark damp wall base",new Color(.10f,.17f,.16f),0,.24f,wall);
    }
    static void BuildEndWall(Transform g)
    {
        // Hide renderers only. Existing wall collisions and all room/ending content are retained.
        foreach(string path in new[]{"Environment/cube","F1/Room/Walls/StartHalf/Cube (2)","F1/Room/Walls/StartHalf/Cube (4)"})
        {var go=GameObject.Find(path);if(!go)throw new Exception("Missing end-wall visual "+path);var r=go.GetComponent<Renderer>();if(r)r.enabled=false;}
        var w=Group("Integrated end wall and fixed door surround",g);
        Bevel("Left plaster return",new Vector3(135.25f,21.4f,-50.0f),new Vector3(16.5f,36.8f,1.2f),.10f,v3Wall,w);
        Bevel("Right plaster return",new Vector3(164.95f,21.4f,-50.0f),new Vector3(16.1f,36.8f,1.2f),.10f,v3Wall,w);
        Bevel("Plaster header",new Vector3(150.1f,32.1f,-50.0f),new Vector3(13.2f,15.4f,1.2f),.10f,v3Wall,w);
        foreach(int side in new[]{-1,1})
        {
            float x=side<0?135.25f:164.95f,ww=side<0?16.5f:16.1f;
            Bevel("Damp lower wall",new Vector3(x,7.55f,-49.30f),new Vector3(ww,9.1f,.20f),.05f,v3Base,w);
            Bevel("Wall cap detail",new Vector3(x,12.15f,-49.13f),new Vector3(ww,.14f,.22f),.04f,v3Iron,w);
            Bevel("Wall skirting",new Vector3(x,3.35f,-49.07f),new Vector3(ww,.62f,.3f),.04f,v3Iron,w);
        }
        foreach(float x in new[]{143.85f,156.45f})
        {
            Bevel("Fixed rolled steel jamb",new Vector3(x,13.5f,-49.02f),new Vector3(.65f,20.6f,1.1f),.10f,v3Paint,w);
            Bevel("Jamb reveal shadow",new Vector3(x+(x<150?.37f:-.37f),13.5f,-48.83f),new Vector3(.10f,20.1f,.12f),.025f,v3Iron,w);
            foreach(float y in new[]{5,13.5f,22})Bevel("Frame mounting bolt",new Vector3(x,y,-48.43f),new Vector3(.13f,.13f,.08f),.035f,v3Iron,w);
        }
        Bevel("Fixed head frame",new Vector3(150.15f,23.9f,-49.02f),new Vector3(13.25f,.65f,1.1f),.09f,v3Paint,w);
        Bevel("Worn threshold",new Vector3(150.15f,3.08f,-49),new Vector3(12,.14f,1.3f),.04f,v3Iron,w);
        // Small practical light replaces the white outline as the visual cue.
        var practical=new GameObject("Door reveal practical light");practical.transform.SetParent(w,false);practical.transform.position=new Vector3(150,25.3f,-46.5f);practical.transform.rotation=Quaternion.Euler(70,180,0);
        var l=practical.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.85f,.73f,.52f);l.intensity=2.2f;l.range=38;l.spotAngle=100;l.shadows=LightShadows.Soft;l.shadowBias=.035f;
    }
    static void BuildMovingDoor(Transform g)
    {
        var pivot=GameObject.Find("NaiveDoor (6)/Door3.1Pivot").transform;
        foreach(var r in pivot.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var moving=Group("DoorVisual_DistressedSteel",pivot);
        Bevel("Pressed steel door leaf",new Vector3(-5.55f,.23f,0),new Vector3(10.95f,19.5f,.46f),.12f,v3Paint,moving);
        foreach(int face in new[]{-1,1})
        {
            float z=face*.30f;
            Bevel("Inset upper panel",new Vector3(-5.55f,3.65f,z),new Vector3(8.5f,8.3f,.12f),.10f,v3Paint,moving);
            Bevel("Inset lower panel",new Vector3(-5.55f,-4.7f,z),new Vector3(8.5f,6.2f,.12f),.10f,v3Paint,moving);
            Bevel("Brushed kick plate",new Vector3(-5.55f,-8.2f,face*.36f),new Vector3(9.4f,1.7f,.08f),.035f,v3Iron,moving);
            Bevel("Handle escutcheon",new Vector3(-9.65f,-.3f,face*.37f),new Vector3(.7f,3,.10f),.08f,v3Brass,moving);
            Bevel("Pull handle",new Vector3(-9.65f,-.2f,face*.88f),new Vector3(.24f,2.15f,.24f),.10f,v3Brass,moving);
            foreach(float y in new[]{-1.2f,.8f})Bevel("Handle standoff",new Vector3(-9.65f,y,face*.61f),new Vector3(.22f,.22f,.55f),.06f,v3Brass,moving);
            Bevel("Lock cylinder",new Vector3(-9.65f,-2.35f,face*.43f),new Vector3(.32f,.48f,.16f),.10f,v3Iron,moving);
        }
        foreach(float y in new[]{-7,0,7})
        {
            Bevel("Hinge knuckle",new Vector3(-.18f,y,-.2f),new Vector3(.42f,1.35f,.52f),.17f,v3Iron,moving);
            Bevel("Hinge leaf",new Vector3(-.64f,y,-.31f),new Vector3(.55f,1.05f,.1f),.035f,v3Brass,moving);
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(moving.gameObject,Root+"/Prefabs/DoorVisual_DistressedSteel.prefab",InteractionMode.AutomatedAction);
    }
    static GameObject Bevel(string name,Vector3 position,Vector3 size,float bevel,Material material,Transform parent)
    {
        string key=string.Join("_",new[]{size.x,size.y,size.z,bevel}.Select(v=>v.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)));
        string path=Root+"/Meshes/Bevel_"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=RoundedBox(size,bevel);AssetDatabase.CreateAsset(mesh,path);}
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.allowOcclusionWhenDynamic=false;return go;
    }
    static Mesh RoundedBox(Vector3 size,float bevel)
    {
        var v=new List<Vector3>();var ns=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var half=size*.5f;bevel=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.9f);var inner=half-Vector3.one*bevel;
        for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1})
        {
            int u=(axis+1)%3,w=(axis+2)%3,start=v.Count;float[] us={-half[u],-inner[u],inner[u],half[u]},ws={-half[w],-inner[w],inner[w],half[w]};
            for(int j=0;j<4;j++)for(int i=0;i<4;i++)
            {
                Vector3 p=Vector3.zero;p[axis]=sign*half[axis];p[u]=us[i];p[w]=ws[j];var q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));var n=(p-q).normalized;v.Add(q+n*bevel);ns.Add(n);uv.Add(new Vector2((us[i]/half[u]+1)*.5f,(ws[j]/half[w]+1)*.5f));
            }
            for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=start+j*4+i,b=a+1,c=a+4,d=c+1;if(sign>0)triangles.AddRange(new[]{a,b,c,b,d,c});else triangles.AddRange(new[]{a,c,b,b,c,d});}
        }
        var m=new Mesh{name="Bevelled manufactured surface"};m.SetVertices(v);m.SetNormals(ns);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateBounds();m.RecalculateTangents();return m;
    }
    static void BuildDocuments(Transform g)
    {
        var old=art.Find("06 Suspended evidence echoes - decorative only");foreach(var r in old.GetComponentsInChildren<Renderer>())r.enabled=false;
        var v2=art.Find("V2 Distressed details");
        foreach(var r in v2.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Combined ")&&(r.sharedMaterial.name=="Water marked paper"||r.sharedMaterial.name=="Graphite print"))r.enabled=false;
        var anchors=v2.GetComponentsInChildren<Transform>().Where(t=>t.name=="Suspended document fragment").Concat(old.Cast<Transform>()).ToArray();
        var docs=Group("Curled research sheets - decorative only",g);var mats=new Material[4];var meshes=new Mesh[4];
        for(int i=0;i<4;i++)
        {
            string basename=Root+"/Textures/Papers/UMA_research_"+(i+1).ToString("00");
            var imp=(TextureImporter)AssetImporter.GetAtPath(basename+"_Normal.png");imp.textureType=TextureImporterType.NormalMap;imp.maxTextureSize=2048;imp.SaveAndReimport();
            var al=(TextureImporter)AssetImporter.GetAtPath(basename+"_Albedo.png");al.maxTextureSize=2048;al.anisoLevel=8;al.mipmapEnabled=true;al.SaveAndReimport();
            mats[i]=Pbr("V3 research sheet "+(i+1),Color.white,0,.10f);mats[i].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(basename+"_Albedo.png"));mats[i].SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(basename+"_Normal.png"));mats[i].SetFloat("_BumpScale",.4f);mats[i].EnableKeyword("_NORMALMAP");mats[i].SetFloat("_Cull",0);
            var path=Root+"/Meshes/CurledPaper_"+i+".asset";meshes[i]=PaperMesh(i);AssetDatabase.CreateAsset(meshes[i],path);
        }
        for(int i=0;i<anchors.Length;i++)
        {
            var go=new GameObject("UMA research sheet "+(i+1).ToString("00"));go.transform.SetParent(docs,false);go.transform.SetPositionAndRotation(anchors[i].position,anchors[i].rotation);go.AddComponent<MeshFilter>().sharedMesh=meshes[i%4];var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mats[i%4];r.allowOcclusionWhenDynamic=false;
        }
    }
    static Mesh PaperMesh(int variant)
    {
        const int nx=10,ny=14;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
        {
            float u=(float)x/nx,w=(float)y/ny;float z=.13f*Mathf.Pow(u-.5f,2)*4+.055f*Mathf.Sin(w*6+variant)+.025f*Mathf.Abs(u-.5f);
            v.Add(new Vector3((u-.5f)*2.1f,(w-.5f)*2.97f,z));uv.Add(new Vector2(u,w));
            if(x<nx&&y<ny){int a=y*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;tri.AddRange(new[]{a,c,b,b,c,d});}
        }
        var m=new Mesh{name="Curled paper "+variant};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    [MenuItem("Tools/GoUnder Art/Capture Door Paper Detail")]
    public static void CaptureV3()
    {
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;var c=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();var d=c.GetUniversalAdditionalCameraData();var source=Camera.main;bool post=d.renderPostProcessing;
        try
        {
            c.transform.SetPositionAndRotation(new Vector3(150,15.5f,38),Quaternion.Euler(0,180,0));Capture(c,"v3-endwall-game.png");d.renderPostProcessing=false;Capture(c,"v3-endwall-detail.png");
            c.transform.position=new Vector3(155,14,-29);c.transform.LookAt(new Vector3(150,13,-49));Capture(c,"v3-door-close.png");
            var paper=art.Find("V3 Door and document details/Curled research sheets - decorative only/UMA research sheet 05");
            c.transform.position=paper.position+paper.rotation*new Vector3(0,0,-4.1f);c.transform.LookAt(paper.position,paper.up);Capture(c,"v3-paper-close.png");
            c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);d.renderPostProcessing=post;Capture(c,"v3-entry-game.png");
        }
        finally{d.renderPostProcessing=post;c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());}
    }
    [MenuItem("Tools/GoUnder Art/Refine Door PBR Lighting")]
    public static void RefineDoorPbr()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected art preview Edit Mode.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
        var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/V3 aged green enamel.mat");
        m.SetColor("_BaseColor",new Color(.48f,.57f,.49f));m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Papers/AgedEnamel_Albedo.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Papers/AgedEnamel_MetalSmooth.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.5f);m.SetFloat("_BumpScale",.12f);EditorUtility.SetDirty(m);
        var brass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/V3 worn brass hardware.mat");brass.SetTexture("_BaseMap",null);brass.SetColor("_BaseColor",new Color(.53f,.39f,.21f));brass.SetFloat("_Smoothness",.42f);EditorUtility.SetDirty(brass);
        var g=art.Find("V3 Door and document details");
        if(!g.Find("Door reflected fill"))
        {
            var fill=Group("Door reflected fill",g);fill.position=new Vector3(150,16,-40);var l=fill.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.64f,.73f,.68f);l.intensity=3.8f;l.range=27;l.shadows=LightShadows.None;
        }
        if(!g.Find("Door local reflection"))
        {
            var p=Group("Door local reflection",g);p.position=new Vector3(150,16,-29);var probe=p.gameObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.NoTimeSlicing;probe.resolution=128;probe.size=new Vector3(52,40,60);probe.center=new Vector3(0,3,-8);probe.boxProjection=true;probe.intensity=.8f;probe.farClipPlane=180;probe.RenderProbe();
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);CaptureV3();Debug.Log("Door enamel PBR and local reflection/fill refined; original pendant unchanged.");
    }
    [MenuItem("Tools/GoUnder Art/Finish Door Paper Materials")]
    public static void FinishDoorPaperMaterials()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected preview.");
        for(int i=1;i<=4;i++){var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/V3 research sheet "+i+".mat");m.shader=Shader.Find("GoUnder/Art/Thin Research Paper");EditorUtility.SetDirty(m);}
        var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/Papers/AgedEnamel_MetalSmooth.png");importer.sRGBTexture=false;importer.SaveAndReimport();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);CaptureV3();Debug.Log("Thin paper double-sided PBR and packed metal map configured.");
    }
}
#endif
