using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public sealed class F1BakedLightingBinding : MonoBehaviour
{
 [Serializable] public class Map{public Texture2D color,direction,shadowMask;}
 [Serializable] public class Surface{public Renderer renderer;public int index;public Vector4 scaleOffset;}
 [Serializable] public class Probe{public Renderer renderer;public float[] coefficients;}
 public Map[] maps;public Surface[] surfaces;public Probe[] probes;
 void OnEnable(){Apply();}
 public void Apply()
 {
  if(maps==null||maps.Length==0||!maps[0].color)return;
  var existing=LightmapSettings.lightmaps??new LightmapData[0];int offset=-1;
  for(int i=0;i<existing.Length;i++)if(existing[i].lightmapColor==maps[0].color){offset=i;break;}
  if(offset<0){offset=existing.Length;var combined=new LightmapData[offset+maps.Length];Array.Copy(existing,combined,offset);for(int i=0;i<maps.Length;i++)combined[offset+i]=new LightmapData{lightmapColor=maps[i].color,lightmapDir=maps[i].direction,shadowMask=maps[i].shadowMask};LightmapSettings.lightmaps=combined;}
  LightmapSettings.lightmapsMode=LightmapsMode.CombinedDirectional;
  if(surfaces!=null)foreach(var s in surfaces)if(s.renderer&&s.index>=0&&s.index<maps.Length){var mesh=s.renderer as MeshRenderer;if(mesh)mesh.receiveGI=ReceiveGI.Lightmaps;s.renderer.lightmapIndex=offset+s.index;s.renderer.lightmapScaleOffset=s.scaleOffset;}
  if(probes!=null)foreach(var p in probes){if(!p.renderer||p.coefficients==null||p.coefficients.Length!=27)continue;var sh=new SphericalHarmonicsL2();for(int c=0;c<3;c++)for(int j=0;j<9;j++)sh[c,j]=p.coefficients[c*9+j];var block=new MaterialPropertyBlock();p.renderer.GetPropertyBlock(block);block.CopySHCoefficientArraysFrom(new[]{sh});block.CopyProbeOcclusionArrayFrom(new[]{Vector4.one});p.renderer.lightProbeUsage=LightProbeUsage.CustomProvided;p.renderer.SetPropertyBlock(block);}
 }
}
