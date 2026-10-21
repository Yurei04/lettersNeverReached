using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class LettersBuilder
{
 [MenuItem("Letters/Build the seven days")]
 public static void Build(){
  AssetDatabase.Refresh();
  foreach(string f in Directory.GetFiles("Assets/Letters/Art","*.png")){var t=AssetImporter.GetAtPath(f) as TextureImporter;if(t){t.textureType=TextureImporterType.Default;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.tag="MainCamera";cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.14f,.12f);cam.orthographic=true;cam.transform.position=new Vector3(0,0,-10);
  var game=new GameObject("Letters Never Reached • Seven days").AddComponent<LettersGame>();
  game.office=Load("office");game.paper=Load("paper");game.hospital=Load("hospital");game.hand=Load("hand");game.envelopeArt=Load("envelope");game.rightHand=Load("right-hand");game.glow=Load("glow");
  game.portraits=Directory.GetFiles("Assets/Letters/Art","*-*.png").Select(f=>AssetDatabase.LoadAssetAtPath<Texture2D>(f)).ToArray();
  string[] names={"typewriter","desk-lamp","postage-stamp","letter-tray","keepsake-frame"};game.propImages=new Texture2D[5];game.propPrefabs=new GameObject[5];
  Directory.CreateDirectory("Assets/Letters/Props");
  for(int i=0;i<names.Length;i++){
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Tripo/{names[i]}/{names[i]}.obj");if(!asset)continue;
   var g=UnityEngine.Object.Instantiate(asset);g.name=names[i];g.transform.position=new Vector3(100,0,0);g.transform.rotation=Quaternion.Euler(0,i==4?90:(i==0||i==1?270:180),0);
   var bounds=new Bounds(g.transform.position,Vector3.zero);foreach(var r in g.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
   float scale=2/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);g.transform.localScale=Vector3.one*scale;
   bounds=new Bounds(g.transform.position,Vector3.zero);foreach(var r in g.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
   var coll=g.AddComponent<BoxCollider>();coll.center=g.transform.InverseTransformPoint(bounds.center);coll.size=bounds.size/scale;
   // Keep a referenced, reusable prefab of every desk object in the saved project.
   var pos=g.transform.position;g.transform.position=Vector3.zero;game.propPrefabs[i]=PrefabUtility.SaveAsPrefabAsset(g,$"Assets/Letters/Props/{names[i]}.prefab");g.transform.position=pos;
   var renderCam=new GameObject("Prop illustration camera",typeof(Camera)).GetComponent<Camera>();renderCam.clearFlags=CameraClearFlags.SolidColor;renderCam.backgroundColor=Color.clear;renderCam.orthographic=true;renderCam.orthographicSize=1.32f;renderCam.nearClipPlane=.01f;renderCam.farClipPlane=30;
   renderCam.transform.position=bounds.center+new Vector3(0,i==0?2.8f:1.4f,-5);renderCam.transform.LookAt(bounds.center);
   var light=new GameObject("Warm desk light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.color=new Color(1,.87f,.68f);light.transform.rotation=Quaternion.Euler(40,-30,0);
   RenderSettings.ambientLight=new Color(.56f,.60f,.58f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
   var rt=new RenderTexture(768,768,24,RenderTextureFormat.ARGB32);renderCam.targetTexture=rt;renderCam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(768,768,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();RenderTexture.active=prev;string path=$"Assets/Letters/Props/{names[i]}.png";File.WriteAllBytes(path,tex.EncodeToPNG());renderCam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(renderCam.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);UnityEngine.Object.DestroyImmediate(g);
   AssetDatabase.ImportAsset(path);var ti=AssetImporter.GetAtPath(path) as TextureImporter;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();game.propImages[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  PlayerSettings.companyName="Quiet Window";PlayerSettings.productName="Letters Never Reached";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.dataCaching=false;PlayerSettings.WebGL.threadsSupport=false;PlayerSettings.defaultWebScreenWidth=1600;PlayerSettings.defaultWebScreenHeight=1000;PlayerSettings.runInBackground=true;
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/LettersNeverReached.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LettersNeverReached.unity",true)};AssetDatabase.SaveAssets();EditorApplication.ExecuteMenuItem("Window/General/Game");Debug.Log("LETTERS_BUILD_READY: Seven-day scene saved.");
 }
 static Texture2D Load(string n){return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Letters/Art/"+n+".png");}
}
