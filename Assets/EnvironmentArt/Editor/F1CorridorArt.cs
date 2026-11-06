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

// Visual authoring only. Existing transforms, colliders, scripts and cameras are preserved.
public static partial class F1CorridorArt
{
    const string Root = "Assets/EnvironmentArt/F1";
    const string Preview = Root + "/Preview/MainSceneF1_ArtPreview.unity";
    const string Output = @"C:\Users\Hongxin\Documents\Codex\2026-11-06\referenced-chatgpt-conversation-this-is-an-2\outputs\corridor";
    static Transform art;
    static Material plaster,basecoat,stone,metal,ceiling,glass,light,amber,sea,city,windowGlow,paper,ink,salt;
    static readonly List<string> disabled = new List<string>();
    static string PathOf(Transform t) { return t.parent ? PathOf(t.parent)+"/"+t.name : t.name; }
    static Dictionary<Component,string> Guard()
    {
        return EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Component>(true))
            .Where(c=>c && (c is Collider || c is MonoBehaviour || c is Transform))
            .ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
    }
    [MenuItem("Tools/GoUnder Art/Build Corridor Preview")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainSceneF1.unity" || scene.isDirty) throw new Exception("Open saved original MainSceneF1 first; refusing to overwrite unsaved work.");
        if(File.Exists(Preview)) throw new Exception("Preview already exists; review it before rebuilding.");
        Directory.CreateDirectory(Output); Directory.CreateDirectory(Root+"/Preview"); Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Textures");
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene,Preview,true);
        scene=EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
        var guard=Guard(); disabled.Clear();
        PrepareMaterials();
        art=new GameObject("F1_EnvironmentArt_Preview").transform;
        Undo.RegisterCreatedObjectUndo(art.gameObject,"Build F1 corridor visual preview");
        foreach(var name in new[]{"F1/Corridor/Cube","F1/Corridor/Cube (1)","F1/Corridor/Glass","F1/Corridor/Glass (1)"}) DisableVisual(name);
        foreach(var name in new[]{"Environment/model","Environment/model (1)","Environment/model (2)","Environment/model (3)","Environment/model (4)","Environment/carpet","Environment/carpet (1)","Environment/carpet (2)","Environment/carpet (3)","Environment/Hallway Light","Environment/Hallway Light (1)"}) DisableVisual(name);
        Floor(); Walls(-1); Walls(1); Roof(); Exterior(); SuspendedFragments();
        // A duplicate camera is a review aid, disabled for normal rendering and never tagged MainCamera.
        var source=Camera.main; var review=new GameObject("F1_Art_ReviewCamera"); review.transform.SetParent(art,false);
        var camera=review.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled=false; camera.tag="Untagged";
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        camera.farClipPlane=1800;
        var srcData=source.GetComponent<UniversalAdditionalCameraData>(); var data=camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing=false; data.renderShadows=true;
        var changes=guard.Where(kv=>!kv.Key || EditorJsonUtility.ToJson(kv.Key)!=kv.Value).Select(kv=>kv.Key?PathOf(kv.Key.transform)+":"+kv.Key.GetType().Name:"Destroyed original component").ToArray();
        if(changes.Length>0) throw new Exception("Original nonvisual component changed: "+string.Join(";",changes));
        var artColliders=art.GetComponentsInChildren<Collider>(true);
        if(artColliders.Length!=0) throw new Exception("Visual root contains colliders.");
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Output,"preview-validation.txt"),"Original scene untouched: Assets/Scenes/MainSceneF1.unity\nPreview: "+Preview+"\nOriginal transforms, colliders and MonoBehaviours compared: "+guard.Count+"; changed: 0\nNew decorative colliders: 0\nDisabled visual components:\n"+string.Join("\n",disabled));
        Capture(camera,"sample-entry.png");
        camera.transform.position=new Vector3(150,15.5f,175); camera.transform.rotation=Quaternion.Euler(0,210,0); Capture(camera,"sample-window.png");
        camera.transform.position=new Vector3(150,15.5f,70); camera.transform.rotation=Quaternion.Euler(0,180,0); Capture(camera,"sample-depth.png");
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("F1 corridor ART PREVIEW complete. Source scene and gameplay unchanged. Review renders: "+Output);
    }
    static void DisableVisual(string path)
    {
        var go=GameObject.Find(path); if(!go) throw new Exception("Expected visual object missing: "+path);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) {r.enabled=false; disabled.Add(PathOf(r.transform)+":Renderer");}
        foreach(var l in go.GetComponentsInChildren<Light>(true)) {l.enabled=false;disabled.Add(PathOf(l.transform)+":Light");}
    }
    static Material Mat(string name,Color color,float smooth=0.1f,float metallic=0)
    {
        var path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);return m;
    }
    static Material Unlit(string name,Color c)
    {
        var path=Root+"/Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",c);return m;
    }
    static void PrepareMaterials()
    {
        plaster=Mat("Salt-grey mineral plaster",new Color(.55f,.59f,.57f));
        basecoat=Mat("Deep petrol wall base",new Color(.12f,.20f,.21f),.22f);
        stone=Mat("Basalt terrazzo",new Color(.22f,.27f,.28f),.32f);
        metal=Mat("Oxidised bronze trim",new Color(.27f,.24f,.18f),.35f,.65f);
        ceiling=Mat("Warm grey ceiling",new Color(.32f,.35f,.34f));
        glass=Mat("Smoked sea glass",new Color(.10f,.22f,.24f),.8f,.25f);
        light=Unlit("Cold diffuse light",new Color(1.4f,1.65f,1.7f));
        amber=Unlit("Warm guidance light",new Color(1.8f,1.1f,.45f));
        sea=Unlit("Distant ocean",new Color(.085f,.155f,.185f));
        city=Unlit("Drowned city silhouette",new Color(.10f,.17f,.195f));
        windowGlow=Unlit("Distant window glimmer",new Color(.29f,.34f,.31f));
        paper=Mat("Water marked paper",new Color(.64f,.61f,.49f));ink=Mat("Graphite print",new Color(.13f,.19f,.19f));
        salt=Mat("Salt tide residue",new Color(.39f,.43f,.37f));
        plaster.SetTexture("_BaseMap",Texture("mineral",0));
        stone.SetTexture("_BaseMap",Texture("terrazzo",1));
        ceiling.SetTexture("_BaseMap",Texture("ceiling",0));
    }
    static Texture2D Texture(string name,int mode)
    {
        var path=Root+"/Textures/"+name+".png";
        if(!File.Exists(path))
        {
            const int n=512; var tex=new Texture2D(n,n,TextureFormat.RGB24,false);var pix=new Color[n*n];var rnd=new System.Random(412+mode);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float nx=(float)x/n,ny=(float)y/n;
                // Periodic coordinates avoid a visible seam when tiled.
                float a=nx*Mathf.PI*2,b=ny*Mathf.PI*2;
                float noise=Mathf.PerlinNoise(Mathf.Cos(a)*2+5+Mathf.Sin(b),Mathf.Sin(a)*2+5+Mathf.Cos(b));
                float micro=Mathf.PerlinNoise(x*.13f+8,y*.13f+7);
                float v=.94f+micro*.025f+(float)rnd.NextDouble()*.025f;
                if(mode==1 && rnd.NextDouble()<.035) v-=.08f+(float)rnd.NextDouble()*.14f;
                pix[y*n+x]=new Color(v,v*.995f,v*.975f);
            }
            tex.SetPixels(pix);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.wrapMode=TextureWrapMode.Repeat;imp.maxTextureSize=512;imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Transform Group(string name,Transform parent=null)
    { var g=new GameObject(name).transform;g.SetParent(parent?parent:art,false);return g; }
    static GameObject Box(string name,Vector3 position,Vector3 size,Material mat,Transform parent=null,Quaternion? rotation=null)
    {
        var g=new GameObject(name);g.transform.SetParent(parent?parent:art,false);g.transform.localPosition=position;g.transform.localRotation=rotation??Quaternion.identity;g.transform.localScale=size;
        var mf=g.AddComponent<MeshFilter>();mf.sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;r.allowOcclusionWhenDynamic=false;GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic);return g;
    }
    static void Lamp(string name,Vector3 pos,Color color,float intensity,float range,Transform parent=null)
    {var g=new GameObject(name);g.transform.SetParent(parent?parent:art,false);g.transform.localPosition=pos;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
    static void Floor()
    {
        var g=Group("01 Floor - existing collision retained");
        for(int row=0;row<25;row++)for(int col=0;col<4;col++)
            Box("Stone slab",new Vector3(133.3f+col*11.1f,3.04f,245-row*12),new Vector3(10.98f,.13f,11.88f),stone,g);
        foreach(int side in new[]{-1,1})
        {
            Box("Perimeter bronze inlay",new Vector3(150+side*20.3f,3.13f,101),new Vector3(.10f,.025f,300),metal,g);
            Box("Recessed floor guidance",new Vector3(150+side*21.5f,3.15f,101),new Vector3(.09f,.04f,300),amber,g);
        }
    }
    static float Twist(float z) {return Mathf.Clamp01((180-z)/230)*4.2f;}
    static void Walls(int side)
    {
        var g=Group(side<0?"02 West window wall":"03 East window wall");
        // Window openings remain visual-only; original solid wall colliders are untouched.
        for(int i=0;i<5;i++)
        {
            float z=221-i*60; float x=150+side*23; float angle=Twist(z)*side;
            var bay=Group("Bay "+(i+1).ToString("00"),g); bay.localPosition=new Vector3(x,3,z);bay.localRotation=Quaternion.Euler(0,0,angle);
            Box("Lower wall",new Vector3(0,4.6f,0),new Vector3(.85f,9.2f,59.8f),basecoat,bay);
            Box("Upper wall",new Vector3(0,31,0),new Vector3(.85f,12,59.8f),plaster,bay);
            foreach(int edge in new[]{-1,1}) Box("Wall pier",new Vector3(0,18.5f,edge*23.5f),new Vector3(.85f,18.6f,12.8f),plaster,bay);
            Box("Skirting",new Vector3(-side*.55f,.65f,0),new Vector3(.35f,1.3f,60),metal,bay);
            Box("Wall cap",new Vector3(-side*.6f,9.15f,0),new Vector3(.3f,.2f,60),metal,bay);
            // Recessed window frame, sill and small mullions, with an unobstructed sea view.
            foreach(float y in new[]{9.8f,25.3f})Box("Window head or sill",new Vector3(0,y,0),new Vector3(2.4f,.65f,34.5f),metal,bay);
            foreach(int edge in new[]{-1,1})Box("Window jamb",new Vector3(0,17.5f,edge*17),new Vector3(1.6f,15.5f,.65f),metal,bay);
            Box("Window centre mullion",new Vector3(.2f*side,17.5f,0),new Vector3(.65f,15.1f,.30f),metal,bay);
            Box("Window lower rail",new Vector3(.2f*side,13.3f,0),new Vector3(.65f,.24f,34),metal,bay);
            // A thin glass edge reads as glazing without covering the exterior with an opaque sheet.
            Box("Glass edge reflection",new Vector3(-side*.08f,23.6f,-16.5f),new Vector3(.12f,2,.10f),light,bay);
            Box("Pier light recess",new Vector3(-side*.7f,20,-25),new Vector3(.35f,12,1.2f),basecoat,bay);
            Box("Pier light diffuser",new Vector3(-side*.94f,20,-25),new Vector3(.12f,10,.30f),light,bay);
            Lamp("Soft interior bounce",new Vector3(-side*5,23,-23),new Color(.72f,.84f,.86f),1.8f,48,bay);
            // Tidal residue is irregular and concentrated around the lower seam.
            for(int j=0;j<6;j++)
            {
                float len=3.5f+(j*7+i*3)%8;float height=.08f+(j%3)*.065f;
                Box("Salt residue",new Vector3(-side*.47f,6.7f+Mathf.Sin(j*2+i)*.12f,-25+j*9.2f),new Vector3(.04f,height,len),salt,bay);
            }
        }
    }
    static void Roof()
    {
        var g=Group("04 Ceiling and progressive structural drift");
        for(int i=0;i<10;i++)
        {
            float z=236-i*30;float a=Twist(z);
            var rib=Group("Ceiling bay "+i,g);rib.localPosition=new Vector3(150,38.7f,z);rib.localRotation=Quaternion.Euler(0,0,a);
            Box("Ceiling panel",Vector3.zero,new Vector3(48,1.4f,29.8f),ceiling,rib);
            Box("Cross rib",new Vector3(0,-.9f,-14.5f),new Vector3(46,.7f,.65f),metal,rib);
            foreach(int side in new[]{-1,1}) Box("Cove diffuser",new Vector3(side*18,-.85f,0),new Vector3(.22f,.12f,27),light,rib);
        }
    }
    static void Exterior()
    {
        var g=Group("05 Window exterior - visual backdrop only");
        foreach(int side in new[]{-1,1})
        {
            // Local backdrop avoids changing the global sky or the hospital lighting.
            Box("Marine haze horizon",new Vector3(150+side*450,95,100),new Vector3(2,250,1000),Unlit("Marine haze",new Color(.15f,.23f,.265f)),g);
            Box("Ocean surface",new Vector3(150+side*240,1.5f,100),new Vector3(430,.2f,1000),sea,g);
            var random=new System.Random(194+side);
            for(int i=0;i<18;i++)
            {
                float z=-290+i*48+random.Next(-12,12),h=random.Next(18,64),x=150+side*random.Next(140,345),w=random.Next(15,32),depth=random.Next(16,37);
                Box("Submerged urban block",new Vector3(x,h*.5f-9,z),new Vector3(w,h,depth),city,g);
                Box("Roof parapet",new Vector3(x,h-8.5f,z),new Vector3(w+1,.7f,depth+1),metal,g);
                if(i%3==0)Box("Roof plant",new Vector3(x+2,h-6,z),new Vector3(w*.4f,5,depth*.35f),city,g);
                // Sparse horizontal facade lines convey architecture at a distance.
                for(float y=9;y<h-10;y+=7)
                    Box("Faded facade ribbon",new Vector3(x-side*(w*.5f+.05f),y,z),new Vector3(.12f,.3f,depth*.7f),windowGlow,g);
            }
            // Foreground flood defence structure provides scale and a readable coast.
            Box("Drowned sea wall",new Vector3(150+side*82,2.8f,100),new Vector3(5,5,650),city,g);
            for(int i=0;i<14;i++)Box("Sea wall uprights",new Vector3(150+side*82,7,-210+i*48),new Vector3(.7f,4,.7f),city,g);
            for(int i=0;i<12;i++)
            {float z=-150+i*37;Box("Long ocean ripple",new Vector3(150+side*(62+i*9),1.65f,z),new Vector3(25+i*2,.03f,.14f),windowGlow,g);}
            for(int i=0;i<5;i++)Lamp("Window sky fill",new Vector3(150+side*18,21,221-i*60),new Color(.40f,.66f,.78f),1.4f,46,g);
        }
    }
    static void SuspendedFragments()
    {
        var g=Group("06 Suspended evidence echoes - decorative only");
        foreach(var entry in new[]{new Vector3(132,24,145),new Vector3(169,27,82),new Vector3(132,29,26)})
        {
            var cluster=Group("Suspended paper",g);cluster.localPosition=entry;cluster.localRotation=Quaternion.Euler(12,entry.z,18);
            Box("Paper sheet",Vector3.zero,new Vector3(2.1f,2.97f,.025f),paper,cluster);
            for(int i=0;i<9;i++)Box("Research print line",new Vector3(-.12f,1.1f-i*.23f,-.02f),new Vector3(i==0?1.5f:1.1f+(i%3)*.15f,.025f,.01f),ink,cluster);
            Box("Redacted line",new Vector3(0,-.75f,-.025f),new Vector3(1.45f,.15f,.01f),ink,cluster);
        }
    }
    [MenuItem("Tools/GoUnder Art/Refine Corridor Preview")]
    public static void Refine()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview)throw new Exception("Expected art preview scene.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
        foreach(var name in new[]{"mineral","terrazzo","ceiling"}) File.Delete(Root+"/Textures/"+name+".png");
        PrepareMaterials();
        foreach(var r in art.GetComponentsInChildren<Renderer>())r.allowOcclusionWhenDynamic=false;
        foreach(var name in new[]{"Distant ocean","Marine haze","Drowned city silhouette"})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");var color=m.GetColor("_BaseColor");m.shader=Shader.Find("GoUnder/Art/Marine Backdrop");m.SetColor("_BaseColor",color);m.SetFloat("_Mode",name=="Distant ocean"?0:name=="Marine haze"?1:2);EditorUtility.SetDirty(m);
        }
        // Additional roof and facade elements break the skyline without new gameplay geometry.
        var exterior=art.Find("05 Window exterior - visual backdrop only");
        foreach(var t in exterior.GetComponentsInChildren<Transform>().Where(t=>t.name=="Submerged urban block").ToArray())
        {
            var b=t.GetComponent<Renderer>().bounds;var x=b.center.x;var z=b.center.z;var h=b.max.y;var side=x>150?1:-1;
            for(int j=0;j<3;j++)Box("Vertical facade pilaster",new Vector3(x-side*(b.extents.x+.1f),b.center.y,z+(j-1)*b.size.z*.30f),new Vector3(.25f,b.size.y,.45f),city,exterior);
            Box("Rooftop water tank",new Vector3(x,h+2,z),new Vector3(3,4,3),city,exterior);
            Box("Rooftop antenna",new Vector3(x+4,h+5,z+2),new Vector3(.15f,10,.15f),city,exterior);
        }
        // Increase the backdrop height so upper windows never reveal an unrelated global sky.
        foreach(var t in exterior.GetComponentsInChildren<Transform>().Where(t=>t.name=="Marine haze horizon")) {t.localScale=new Vector3(2,1000,1800);t.localPosition=new Vector3(t.localPosition.x,350,100);}
        var camera=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();camera.useOcclusionCulling=false;camera.allowMSAA=true;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=0;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        var source=Camera.main;camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);Capture(camera,"sample-entry.png");
        camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,175),Quaternion.Euler(0,210,0));Capture(camera,"sample-window.png");
        camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,70),Quaternion.Euler(0,180,0));Capture(camera,"sample-depth.png");
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("F1 art preview refinement complete.");
    }
    [MenuItem("Tools/GoUnder Art/Finish Sample Review")]
    public static void FinishSample()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview)throw new Exception("Expected preview.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
        var exterior=art.Find("05 Window exterior - visual backdrop only");
        foreach(var t in exterior.GetComponentsInChildren<Transform>().Where(t=>t.name=="Marine haze horizon")) t.localScale=new Vector3(2,1000,10000);
        foreach(var t in exterior.GetComponentsInChildren<Transform>().Where(t=>t.name=="Ocean surface")) t.localScale=new Vector3(430,.2f,10000);
        var floor=art.Find("01 Floor - existing collision retained");
        if(!floor.Find("Continuous dark grout bed"))Box("Continuous dark grout bed",new Vector3(150,2.98f,101),new Vector3(46,.1f,300),AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Deep petrol wall base.mat"),floor);
        foreach(var r in art.GetComponentsInChildren<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
        var camera=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();var source=Camera.main;camera.farClipPlane=5000;
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);Capture(camera,"sample-entry.png");
        camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,175),Quaternion.Euler(0,210,0));Capture(camera,"sample-window.png");
        camera.transform.SetPositionAndRotation(new Vector3(150,15.5f,70),Quaternion.Euler(0,180,0));Capture(camera,"sample-depth.png");
        camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Debug.Log("Sample review renders saved. Preview only; not a final gameplay/performance validation.");
    }
    static void Capture(Camera camera,string name)
    {
        var rt=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var target=camera.targetTexture;var enabled=camera.enabled;Texture2D image=null;
        try{camera.targetTexture=rt;camera.enabled=false;camera.Render();RenderTexture.active=rt;image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());}
        finally{camera.targetTexture=target;camera.enabled=enabled;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
