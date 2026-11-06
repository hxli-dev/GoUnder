using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// In-memory PlayerPrefs/Resources doubles: these tests do not exercise Unity or gameplay.
namespace UnityEngine {
 public class Object { public string name; }
 public class ScriptableObject : Object {}
 public class Sprite : Object {}
 public class Transform : Object {}
 public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
 public class SerializeField : Attribute {}
 public class TextAreaAttribute : Attribute { public TextAreaAttribute(int min,int max) {} }
 public static class PlayerPrefs {
  static Dictionary<string,object> data = new Dictionary<string,object>();
  public static string GetString(string k,string fallback="") { return data.ContainsKey(k) ? (string)data[k] : fallback; }
  public static int GetInt(string k,int fallback=0) { return data.ContainsKey(k) ? (int)data[k] : fallback; }
  public static void SetString(string k,string v) {data[k]=v;}
  public static void SetInt(string k,int v) {data[k]=v;}
  public static void DeleteKey(string k) {data.Remove(k);}
  public static bool HasKey(string k) {return data.ContainsKey(k);}
  public static void Save() {}
 }
 public static class Resources {
  public static ItemData_SO[] Items;
  public static T[] LoadAll<T>(string path) {return (T[])(object)Items;}
 }
 public static class Debug { public static void LogError(object message) {} }
}
class Program {
 static int passed;
 static void Check(bool condition,string name) {if(!condition) throw new Exception("FAILED: "+name); Console.WriteLine("Passed: "+name);passed++;}
 static ItemData_SO Item(string id,string file,string display) {
  var item=new ItemData_SO {name=file,itemName=display};
  typeof(ItemData_SO).GetField("itemId",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,id);return item;
 }
 static void Main() {
  GameProgress.NewGame(); Check(GameProgress.PrepareContinue()=="MainSceneF3","no save starts Floor 3");
  PlayerPrefs.SetInt("isInFloor2",1);Check(GameProgress.PrepareContinue()=="MainSceneF2","legacy Floor 2 migration");
  PlayerPrefs.SetString(GameProgress.InventoryKey,"floor3-complete");GameProgress.EnterFloor("MainSceneF2");
  PlayerPrefs.SetString(GameProgress.InventoryKey,"mid-floor2");Check(GameProgress.PrepareContinue()=="MainSceneF2","explicit Floor 2 checkpoint");
  Check(PlayerPrefs.GetString(GameProgress.InventoryKey)=="floor3-complete","restore entry inventory not unsolved mid-floor inventory");
  PlayerPrefs.SetString(GameProgress.InventoryKey,"floor2-complete");GameProgress.EnterFloor("MainSceneF1");
  PlayerPrefs.SetString(GameProgress.InventoryKey,"ending-file");Check(GameProgress.PrepareContinue()=="MainSceneF1","Floor 1 restart supported");
  Check(PlayerPrefs.GetString(GameProgress.InventoryKey)=="floor2-complete","Floor 1 inventory snapshot retained");
  GameProgress.EnterFloor("Menu Scene");Check(GameProgress.PrepareContinue()=="MainSceneF1","menu cannot overwrite floor checkpoint");
  PlayerPrefs.SetString("BackgroundPref","user-volume");PlayerPrefs.SetString("xAxis","user-sensitivity");PlayerPrefs.SetString("ExposurePref","user-brightness");
  PlayerPrefs.SetString("AtlasItem_19","legacy-item");GameProgress.NewGame();
  Check(!PlayerPrefs.HasKey(GameProgress.InventoryKey)&&!PlayerPrefs.HasKey("AtlasItem_19")&&!PlayerPrefs.HasKey("isInFloor2"),"new game clears only progress including legacy slots");
  Check(PlayerPrefs.GetString("BackgroundPref")=="user-volume"&&PlayerPrefs.GetString("xAxis")=="user-sensitivity"&&PlayerPrefs.GetString("ExposurePref")=="user-brightness","new game preserves user settings");
  var communication=Item("communication-guid","CommunicationCode","Communication Code");var evelyn=Item("evelyn-guid","Evelyn","Evelyn");Resources.Items=new[]{communication,evelyn};
  Check(ItemData_SO.LoadItemDataByName("communication-guid")==communication,"stable ID restore");
  Check(ItemData_SO.LoadItemDataByName("Communication Code")==communication,"legacy spaced Communication Code restore");
  Check(ItemData_SO.LoadItemDataByName("CommunicationCode")==communication,"legacy resource filename restore");
  Check(ItemData_SO.LoadItemDataByName("evelyn-guid")==evelyn,"Evelyn stable ID restore");
  communication.itemName="Renamed display";Check(ItemData_SO.LoadItemDataByName("communication-guid")==communication,"display rename does not break ID");
  Check(ItemData_SO.LoadItemDataByName("")==null&&ItemData_SO.LoadItemDataByName("unknown")==null,"invalid IDs do not return an arbitrary item");
  Console.WriteLine("Passed "+passed+" isolated logic assertions; Unity runtime NOT TESTED.");
 }
}
