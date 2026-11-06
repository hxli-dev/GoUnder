#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static partial class F1CorridorArt
{
    [MenuItem("Tools/GoUnder Art/Build Distressed Revision")]
    public static void DistressedRevision()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=Preview || scene.isDirty)throw new Exception("Need saved art preview in Edit Mode.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;
        if(art.Find("V2 Distressed details"))throw new Exception("V2 already exists.");
        var original=scene.GetRootGameObjects().Where(g=>g!=art.gameObject).SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c&&(c is Transform||c is Collider||c is MonoBehaviour||c is Light)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
        PrepareMaterials();
        var details=Group("V2 Distressed details");
        foreach(var r in art.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Salt residue"||r.name=="Recessed floor guidance"))r.enabled=false;
        WeatherMaterial(plaster,.80f);WeatherMaterial(basecoat,.95f);WeatherMaterial(ceiling,.85f);WeatherMaterial(metal,.85f);WeatherMaterial(stone,.55f);
        foreach(var root in new[]{art.Find("02 West window wall"),art.Find("03 East window wall")})
        {
            int side=root.name.Contains("West")?-1:1;int i=0;
            foreach(Transform bay in root)
            {
                float a=Mathf.Lerp(.7f,9.0f,i/4f)*side+Mathf.Sin(i*1.7f)*1.8f;
                bay.localRotation=Quaternion.Euler(0,Mathf.Sin(i*2.3f)*1.1f,a);i++;
            }
        }
        int index=0;
        foreach(Transform bay in art.Find("04 Ceiling and progressive structural drift"))
        {
            bay.localRotation=Quaternion.Euler(Mathf.Sin(index*1.2f)*1.2f,0,Mathf.Lerp(0,9,index/9f));
            foreach(Transform child in bay)if(child.name=="Cove diffuser")
            {child.GetComponent<Renderer>().sharedMaterial=index%3==1?basecoat:amber;if(index%3==0)child.localScale=new Vector3(.16f,.1f,11);}
            index++;
        }
        foreach(var l in art.GetComponentsInChildren<Light>()) {l.intensity*=.65f;l.color=Color.Lerp(l.color,new Color(.48f,.62f,.57f),.35f);}
        light.SetColor("_BaseColor",new Color(.65f,.79f,.77f));amber.SetColor("_BaseColor",new Color(.9f,.51f,.22f));
        var exterior=art.Find("05 Window exterior - visual backdrop only");
        foreach(var r in exterior.GetComponentsInChildren<Renderer>())r.enabled=false;
        DetailedCoast(details);IndustrialFixture(details);FracturesAndDebris(details);RearClosure(details);
        var changed=original.Where(kv=>!kv.Key||EditorJsonUtility.ToJson(kv.Key)!=kv.Value).Select(kv=>kv.Key?PathOf(kv.Key.transform)+":"+kv.Key.GetType().Name:"Destroyed original").ToArray();
        if(changed.Length>0)throw new Exception("Original gameplay/light/effect changed: "+string.Join(";",changed));
        if(details.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Unexpected decorative collision.");
        CombineVisualGeometry(details);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Path.Combine(Output,"v2-validation.txt"),"Original transforms, colliders, MonoBehaviours and Lights compared: "+original.Count+"; changed: 0\nLamp original Light, flicker, audio, volumetric beam retained. Only four old mesh renderers hidden.\nNew decorative colliders: 0\nRear closure is VISUAL ONLY; no collision/progression change.\nFormal MainSceneF1 unchanged.\n");
        ReviewV2();Debug.Log("Distressed corridor revision completed; original gameplay and lamp effects preserved.");
    }
    static void WeatherMaterial(Material m,float amount)
    {
        var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Apple/3D model/Material/Surfaces/Flaked Paint Wall_vhqkeff/Textures/hallway.mat");
        m.shader=Shader.Find("GoUnder/Art/Weathered Surface");m.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));m.SetFloat("_Weathering",amount);m.SetFloat("_Scale",m==stone?.16f:.12f);EditorUtility.SetDirty(m);
    }
    static void IndustrialFixture(Transform parent)
    {
        var lamp=GameObject.Find("Environment/Lampara");
        foreach(Transform t in lamp.transform) {var r=t.GetComponent<MeshRenderer>();if(r)r.enabled=false;}
        var spot=lamp.transform.Find("Spot Light (1)");
        var g=Group("Corroded marine bulkhead pendant",parent);g.position=spot.position;g.rotation=Quaternion.FromToRotation(Vector3.down,spot.forward);
        // Turned housing: bevelled reflector, stepped ballast can and steel protective cage.
        Lathe("Stepped reflector",new[]{new Vector2(1.4f,4.8f),new Vector2(2.0f,4.6f),new Vector2(2.1f,2.7f),new Vector2(2.8f,2.1f),new Vector2(3.5f,.65f),new Vector2(3.65f,.25f),new Vector2(3.6f,0),new Vector2(3.25f,.08f),new Vector2(2.5f,1.2f),new Vector2(.9f,2.3f)},metal,g);
        Lathe("Thick protective rim",new[]{new Vector2(3.5f,.2f),new Vector2(3.8f,.15f),new Vector2(3.85f,-.15f),new Vector2(3.45f,-.2f)},metal,g);
        Lathe("Diffused recessed lens",new[]{new Vector2(0,-.05f),new Vector2(3.35f,-.05f)},amber,g);
        Box("Suspension stem",new Vector3(0,5.6f,0),new Vector3(.35f,2,.35f),metal,g);
        Box("Mounting plate",new Vector3(0,6.7f,0),new Vector3(3,.4f,3),metal,g);
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*3.4f,-.1f,Mathf.Sin(a)*3.4f);Box("Steel cage rib",p,new Vector3(.13f,.18f,6.5f),metal,g,Quaternion.Euler(0,i*60,0));}
        for(int i=0;i<4;i++)Box("Ballast cooling fin",new Vector3(0,3.0f+i*.37f,0),new Vector3(4.4f,.12f,3.4f),metal,g);
    }
    static void Lathe(string name,Vector2[] profile,Material m,Transform parent)
    {
        int count=profile.Length,segments=48;var v=new Vector3[count*(segments+1)];var uv=new Vector2[v.Length];var tris=new List<int>();
        for(int j=0;j<count;j++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;int k=j*(segments+1)+i;v[k]=new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x);uv[k]=new Vector2((float)i/segments,(float)j/count);if(j<count-1&&i<segments){int b=k+segments+1;tris.AddRange(new[]{k,b,k+1,k+1,b,b+1});}}
        var mesh=new Mesh{name=name};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=m;
    }
    static void DetailedCoast(Transform parent)
    {
        var g=Group("Drowned coastal district - layered architecture",parent);
        var sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Marine haze.mat");
        var water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Distant ocean.mat");
        var masonry=Unlit("V2 Coastal masonry",new Color(.08f,.12f,.14f));masonry.shader=Shader.Find("GoUnder/Art/Marine Backdrop");masonry.SetFloat("_Mode",2);
        var recess=Unlit("V2 Dead window recess",new Color(.018f,.035f,.045f));recess.shader=masonry.shader;recess.SetFloat("_Mode",2);
        var concrete=Unlit("V2 Coastal concrete edge",new Color(.13f,.16f,.16f));concrete.shader=masonry.shader;concrete.SetFloat("_Mode",2);
        var sphere=new GameObject("Continuous sky dome - no box edges");sphere.transform.SetParent(g,false);sphere.transform.position=new Vector3(150,0,100);sphere.transform.localScale=Vector3.one*1500;sphere.AddComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Sphere.fbx");sphere.AddComponent<MeshRenderer>().sharedMaterial=sky;
        Box("Continuous ocean horizon",new Vector3(150,1.6f,100),new Vector3(2400,.1f,2400),water,g);
        var random=new System.Random(3861);
        foreach(int side in new[]{-1,1})for(int i=0;i<12;i++)
        {
            float z=-180+i*56+random.Next(-14,15),x=150+side*random.Next(115,370);int floors=random.Next(4,11),cols=random.Next(3,6);float w=cols*5.5f,h=floors*5.5f,dep=random.Next(18,34),bottom=-10;
            var b=Group("Abandoned coastal building",g);b.localPosition=new Vector3(x,0,z);b.localRotation=Quaternion.Euler(0,random.Next(-12,13),i%5==0?side*3:0);
            Box("Salt concrete mass",new Vector3(0,bottom+h*.5f,0),new Vector3(w,h,dep),masonry,b);
            for(int f=0;f<floors;f++)
            {
                float y=bottom+3+f*5.5f;
                for(int c=0;c<cols;c++)
                {
                    float zz=-dep*.5f+.9f+c*(dep-1.8f)/Mathf.Max(1,cols-1);bool broken=random.NextDouble()<.28;
                    Box("Recessed window",new Vector3(-side*(w*.5f+.035f),y,zz),new Vector3(.11f,3.1f,2.6f),recess,b);
                    if(!broken)Box("Window sill",new Vector3(-side*(w*.5f+.24f),y-1.7f,zz),new Vector3(.55f,.18f,3.0f),concrete,b);
                    if(f%3==1&&!broken){Box("Balcony floor",new Vector3(-side*(w*.5f+1.4f),y-1.8f,zz),new Vector3(2.8f,.3f,3.6f),concrete,b);Box("Balcony rail",new Vector3(-side*(w*.5f+2.6f),y-.4f,zz),new Vector3(.12f,.14f,3.5f),masonry,b);}
                }
            }
            Box("Roof coping",new Vector3(0,bottom+h+.25f,0),new Vector3(w+.6f,.6f,dep+.6f),concrete,b);
            Box("Roof access stairwell",new Vector3(w*.2f,bottom+h+2,dep*.2f),new Vector3(w*.25f,4,dep*.3f),masonry,b);
            Box("Antenna mast",new Vector3(-w*.2f,bottom+h+5,-dep*.2f),new Vector3(.12f,10,.12f),concrete,b);
            Box("Antenna cross arm",new Vector3(-w*.2f,bottom+h+8,-dep*.2f),new Vector3(.12f,.12f,5),concrete,b);
        }
        // Short broken segments read as coastal ruins, without long parallel placeholder blocks.
        for(int i=0;i<9;i++) {float z=-80+i*39;Box("Broken quay segment",new Vector3(210+(i%3)*7,1.2f,z),new Vector3(9,2.7f,18),masonry,g,Quaternion.Euler(0,i*13,0));}
    }
    static void RearClosure(Transform parent)
    {
        var g=Group("Rear wall visual termination - collision unchanged",parent);
        Box("Rear infill",new Vector3(150,21.5f,250.8f),new Vector3(47.6f,37,.8f),plaster,g);
        Box("Rear base",new Vector3(150,7.6f,250.2f),new Vector3(47.6f,9.2f,.5f),basecoat,g);
        foreach(int side in new[]{-1,1})Box("Rear seam",new Vector3(150+side*14,22,250.1f),new Vector3(.2f,34,.3f),metal,g);
        // The existing curtains remain, now backed by a visible wall instead of a background canyon.
    }
    static void FracturesAndDebris(Transform parent)
    {
        var g=Group("Peeling surfaces and suspended memory debris",parent);var random=new System.Random(55);
        // Broken ceiling strips and hanging service cables are kept above head clearance.
        for(int i=0;i<12;i++)
        {
            float z=185-i*18,x=150+(i%2==0?-1:1)*random.Next(8,18);
            Box("Dislocated ceiling panel",new Vector3(x,34+random.Next(0,3),z),new Vector3(4+random.Next(5),.25f,8+random.Next(6)),ceiling,g,Quaternion.Euler(random.Next(-12,13),random.Next(-8,9),random.Next(-18,19)));
            for(int k=0;k<3;k++)Box("Exposed ceiling conduit",new Vector3(x+k*.35f,37,z),new Vector3(.10f,.1f,14),metal,g,Quaternion.Euler(0,0,6));
        }
        for(int i=0;i<27;i++)
        {
            int side=i%2==0?-1:1;float z=205-i*8.5f;var t=Group("Suspended document fragment",g);t.localPosition=new Vector3(150+side*random.Next(10,20),random.Next(19,32),z);t.localRotation=Quaternion.Euler(random.Next(-35,36),random.Next(360),random.Next(-45,46));
            Box("Water damaged sheet",Vector3.zero,new Vector3(2.1f,2.97f,.025f),paper,t);
            for(int j=0;j<7;j++)Box("Faded research print",new Vector3(0,.95f-j*.26f,-.025f),new Vector3(j%3==0?1.1f:1.6f,.035f,.01f),ink,t);
        }
        for(int i=0;i<7;i++)
        {
            int side=i%2==0?-1:1;var t=Group("Detached empty frame",g);t.localPosition=new Vector3(150+side*18,23+i%3*2,160-i*27);t.localRotation=Quaternion.Euler(i*5,i*11,side*(12+i*3));
            foreach(int s in new[]{-1,1}){Box("Frame vertical",new Vector3(s*2,0,0),new Vector3(.22f,5,.3f),metal,t);Box("Frame horizontal",new Vector3(0,s*2.5f,0),new Vector3(4.3f,.22f,.3f),metal,t);}
        }
        // Narrow irregular fractures follow the wall surface, rather than regular decorative dashes.
        foreach(var wall in new[]{art.Find("02 West window wall"),art.Find("03 East window wall")})
        {
            int side=wall.name.Contains("West")?-1:1;
            foreach(Transform bay in wall)
            {
                for(int i=0;i<4;i++)
                {float y=28+random.Next(7),z=random.Next(-27,28);for(int j=0;j<4;j++){Box("Branching plaster fracture",new Vector3(-side*.46f,y-j*.8f,z+j*.4f),new Vector3(.03f,1.2f,.045f),ink,bay,Quaternion.Euler(random.Next(-40,41),0,0));}}
            }
        }
    }
    static void CombineVisualGeometry(Transform root)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name!="Continuous sky dome - no box edges").ToArray();
        Directory.CreateDirectory(Root+"/Meshes");AssetDatabase.Refresh();
        int index=0;
        foreach(var group in renderers.GroupBy(r=>new {Material=r.sharedMaterial,Shadows=r.shadowCastingMode}))
        {
            var combine=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var path=Root+"/Meshes/V2_"+(index++)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool fresh=!mesh;if(fresh)mesh=new Mesh{name="V2 combined "+index,indexFormat=IndexFormat.UInt32};else mesh.Clear();mesh.CombineMeshes(combine,true,true);if(fresh)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            var go=new GameObject("Combined "+group.Key.Material.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key.Material;renderer.shadowCastingMode=group.Key.Shadows;renderer.allowOcclusionWhenDynamic=false;
            foreach(var r in group)r.enabled=false;
        }
    }
    [MenuItem("Tools/GoUnder Art/Capture Distressed Revision")]
    public static void ReviewV2()
    {
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;var c=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();
        var source=Camera.main;c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);Capture(c,"v2-entry.png");
        c.transform.SetPositionAndRotation(new Vector3(150,15.5f,110),Quaternion.Euler(0,180,0));Capture(c,"v2-depth.png");
        c.transform.SetPositionAndRotation(new Vector3(150,17,28),Quaternion.Euler(-22,180,0));Capture(c,"v2-lamp.png");
        c.transform.SetPositionAndRotation(new Vector3(150,15.5f,190),Quaternion.Euler(0,240,0));Capture(c,"v2-exterior.png");
        c.transform.SetPositionAndRotation(new Vector3(150,15.5f,225),Quaternion.Euler(0,0,0));Capture(c,"v2-rear.png");
        c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
    [MenuItem("Tools/GoUnder Art/Polish Distressed Revision")]
    public static void PolishV2()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected preview Edit Mode.");
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;var details=art.Find("V2 Distressed details");
        foreach(var t in details.Cast<Transform>().Where(t=>t.name.StartsWith("Combined ")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var r in details.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
        var fixture=details.Find("Corroded marine bulkhead pendant");var spot=GameObject.Find("Environment/Lampara/Spot Light (1)").transform;fixture.position=spot.position+spot.forward*5.2f;
        var cage=fixture.Cast<Transform>().Where(t=>t.name=="Steel cage rib").ToArray();
        for(int i=0;i<cage.Length;i++)
        {
            cage[i].GetComponent<Renderer>().enabled=i<3;
            float x=(i-1)*1.65f;cage[i].localPosition=new Vector3(x,-.25f,0);cage[i].localRotation=Quaternion.identity;cage[i].localScale=new Vector3(.14f,.16f,2*Mathf.Sqrt(Mathf.Max(0,3.4f*3.4f-x*x)));
        }
        foreach(var r in fixture.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        CombineVisualGeometry(details);
        // The fixture batch must not shadow the original spotlight above its retained position.
        foreach(var r in details.GetComponentsInChildren<Renderer>())r.allowOcclusionWhenDynamic=false;
        var cam=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();var src=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();var dst=cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(src){dst.volumeLayerMask=src.volumeLayerMask;dst.renderPostProcessing=src.renderPostProcessing;}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ReviewV2();
        Debug.Log("V2 fixture lowered into view; screenshots now use the original camera volume mask and post effects.");
    }
    [MenuItem("Tools/GoUnder Art/Capture V2 Material Details")]
    public static void CaptureMaterialDetails()
    {
        art=GameObject.Find("F1_EnvironmentArt_Preview").transform;var details=art.Find("V2 Distressed details");int index=0;
        foreach(var mf in details.GetComponentsInChildren<MeshFilter>())
            if(mf.sharedMesh&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mf.sharedMesh))) {AssetDatabase.CreateAsset(mf.sharedMesh,Root+"/Meshes/Fixture_source_"+(index++)+".asset");}
        var c=art.Find("F1_Art_ReviewCamera").GetComponent<Camera>();var d=c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();bool post=d.renderPostProcessing;var source=Camera.main;
        try
        {
            d.renderPostProcessing=false;
            c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);Capture(c,"v2-entry-detail.png");
            c.transform.SetPositionAndRotation(new Vector3(150,15.5f,110),Quaternion.Euler(0,180,0));Capture(c,"v2-depth-detail.png");
            c.transform.position=new Vector3(156,28,4);c.transform.LookAt(details.Find("Corroded marine bulkhead pendant").position+Vector3.up*1.5f);Capture(c,"v2-lamp-detail.png");
            c.transform.SetPositionAndRotation(new Vector3(150,15.5f,190),Quaternion.Euler(0,240,0));Capture(c,"v2-exterior-detail.png");
        }
        finally{d.renderPostProcessing=post;c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());}
        Debug.Log("V2 technical detail views saved without global post effects; review camera setting restored.");
    }
}
#endif
