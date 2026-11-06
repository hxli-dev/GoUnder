#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class F1DoorVisualVerification
{
    const string Preview="Assets/EnvironmentArt/F1/Preview/MainSceneF1_ArtPreview.unity";
    const string Report=@"C:\Users\Hongxin\Documents\Codex\2026-11-06\referenced-chatgpt-conversation-this-is-an-2\outputs\corridor\v3-door-runtime-test.json";
    static double started;static bool invoked;static Vector3 before;static bool rayHit;
    [Serializable] class Result {public bool isolatedScene,originalDoorScriptCalled,openedFlag,animatorOpenParameter,closedRayHitsDoor,doorRotated,leafFollowsPivot;public float rotationDegrees,visualRelativeDrift;public string scope;}
    static Vector3 leafBefore;
    static F1DoorVisualVerification(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
    [MenuItem("Tools/GoUnder Art/Test Isolated Door Opening")]
    public static void StartTest()
    {
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!=Preview||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Need saved art preview.");
        var original=GameObject.Find("NaiveDoor (6)");if(!original)throw new Exception("Door absent.");
        // Create a door-only test scene. No GameManager, inventory, player or save systems are loaded.
        var clone=UnityEngine.Object.Instantiate(original);clone.name="DoorVerificationSubject";
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,test);
        EditorSceneManager.SetActiveScene(test);
        var camera=new GameObject("Verification Camera");var c=camera.AddComponent<Camera>();camera.AddComponent<AudioListener>();camera.transform.position=new Vector3(150,15,-20);camera.transform.LookAt(new Vector3(150,13,-49));
        EditorSceneManager.SaveScene(test,"Assets/EnvironmentArt/F1/Preview/F1_DoorVerification.unity");
        EditorSceneManager.CloseScene(scene,true);
        SessionState.SetBool("GoUnder.Art.DoorTest",true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool("GoUnder.Art.DoorTest",false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;invoked=false;}
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool("GoUnder.Art.DoorTest",false);
            EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(Preview,OpenSceneMode.Single);
        }
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool("GoUnder.Art.DoorTest",false)||started<=0)return;
        double elapsed=EditorApplication.timeSinceStartup-started;
        try
        {
            var root=GameObject.Find("DoorVerificationSubject");if(!root)return;var pivot=root.transform.Find("Door3.1Pivot");var door=pivot.GetComponent<DoorInteract>();var collider=pivot.GetComponent<Collider>();var visual=pivot.Find("DoorVisual_DistressedSteel");
            if(!invoked&&elapsed>.8)
            {
                before=pivot.localEulerAngles;leafBefore=visual.localPosition;Physics.SyncTransforms();RaycastHit hit;
                rayHit=Physics.Raycast(collider.bounds.center+Vector3.forward*12,Vector3.back,out hit,24)&&hit.collider==collider;
                door.PlayOpenDoor();invoked=true;
            }
            if(invoked&&elapsed>4)
            {
                float angle=Mathf.Abs(Mathf.DeltaAngle(before.y,pivot.localEulerAngles.y));float drift=Vector3.Distance(leafBefore,visual.localPosition);
                var result=new Result{isolatedScene=true,originalDoorScriptCalled=true,openedFlag=door.opened,animatorOpenParameter=pivot.GetComponent<Animator>().GetBool("OpenDoor"),closedRayHitsDoor=rayHit,doorRotated=angle>60,rotationDegrees=angle,visualRelativeDrift=drift,leafFollowsPivot=drift<.0001f&&visual.parent==pivot,scope="Isolated original DoorInteract + Animator + Collider + new visual prefab; no PlayerInteraction, floor progression, inventory or save systems tested."};
                File.WriteAllText(Report,JsonUtility.ToJson(result,true));Debug.Log("Door visual verification: "+JsonUtility.ToJson(result));EditorApplication.isPlaying=false;started=0;
            }
        }
        catch(Exception e){File.WriteAllText(Report,e.ToString());Debug.LogException(e);EditorApplication.isPlaying=false;started=0;}
    }
}
#endif
