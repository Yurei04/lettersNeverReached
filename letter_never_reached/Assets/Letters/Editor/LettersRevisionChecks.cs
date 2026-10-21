using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class LettersRevisionChecks
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static LettersGame g;
 static object Get(string n)=>typeof(LettersGame).GetField(n,F).GetValue(g);
 static void Set(string n,object v)=>typeof(LettersGame).GetField(n,F).SetValue(g,v);
 static object Call(string n,params object[] args)=>typeof(LettersGame).GetMethod(n,F).Invoke(g,args);
 static int checks;
 static void Check(bool yes,string why){if(!yes)throw new Exception(why);checks++;}
 public static string MigrationAndAudio()
 {
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();checks=0;Set("replaying",true);
  var old=new LettersGame.SaveData{version=2,day=6,visitor=0,phase=(int)LettersGame.Phase.Typing,delivered=9,target=LetterStory.Days[6][1].letters[0],typed="Mira,"};
  Call("MigrateSave",old);Check(old.version==3&&old.visitor==1&&old.typed=="Mira,","Old reunion typing did not migrate");Check((bool)Call("ValidSave",old),"Migrated typing invalid");
  old=new LettersGame.SaveData{version=2,day=6,phase=(int)LettersGame.Phase.Ending,delivered=10};old.letters.Add(new LettersGame.SavedLetter{day=6,visitor=0,text=LetterStory.Days[6][1].letters[0],address="Mira"});
  Call("MigrateSave",old);Check(old.visitor==1&&old.letters[0].visitor==1,"Old reunion archive not preserved");Check((bool)Call("ValidSave",old),"Old completed game cannot continue");
  old=new LettersGame.SaveData{version=2,day=6,phase=(int)LettersGame.Phase.Morning,delivered=9};Call("MigrateSave",old);Check(old.visitor==0,"Unopened seventh morning skipped soldier");
  old=new LettersGame.SaveData{version=2,day=5,phase=(int)LettersGame.Phase.NightSeal,nightStep=5,delivered=9};Call("MigrateSave",old);Check(old.nightStep==6&&(bool)Call("ValidSave",old),"Old window checkpoint cannot dispatch");
  Call("Restore",old);Check((int)Get("nightStep")==6,"Window checkpoint regressed");
  g.day=6;g.visitor=0;Call("Go",LettersGame.Phase.Choice);g.SelectFeeling(0);
  var keys=(AudioClip[])Get("mechanicalKeys");Check(keys.Length==6&&keys.Select(c=>c.name).Distinct().Count()==6,"Key variants missing");
  foreach(var clip in keys.Concat(new[]{(AudioClip)Get("spaceKey"),(AudioClip)Get("backspaceKey"),(AudioClip)Get("carriageReturn")})) {var data=new float[clip.samples];clip.GetData(data,0);Check(data.All(v=>!float.IsNaN(v)&&Mathf.Abs(v)<1)&&data.Any(v=>Mathf.Abs(v)>.02f),"Invalid/silent audio "+clip.name);}
  var keySource=(AudioSource)Get("typewriterKeys");var mechanism=(AudioSource)Get("typewriterMechanism");
  float originalEffects=(float)Get("effectsVolume");bool originalMute=g.muted;
  Set("effectsVolume",0f);Call("ApplyAudio");Check(keySource.volume==0&&mechanism.volume==0,"Effects slider misses typewriter");
  Set("effectsVolume",.6f);g.muted=true;Call("ApplyAudio");Check(AudioListener.volume==0,"Mute misses typewriter");g.muted=originalMute;Set("effectsVolume",originalEffects);Call("ApplyAudio");
  Call("OnChar",'E');Check(keySource.clip.name.StartsWith("Typebar"),"Typed key not synchronized");Set("lastKeyAt",-1f);g.Backspace();Check(keySource.clip.name=="Backspace ratchet","Backspace has no ratchet");
  string target=(string)Get("target");var lines=(System.Collections.Generic.List<int>)Get("writingLineStarts");int margin=lines[1];g.typed=target.Substring(0,margin-1);Call("ResetCarriage");mechanism.Stop();Call("OnChar",target[margin-1]);Check(mechanism.isPlaying&&mechanism.clip.name=="Margin bell and carriage return","Rendered line did not trigger bell/carriage");
  mechanism.Stop();g.Backspace();Call("OnChar",target[margin-1]);Check(!mechanism.isPlaying,"Correcting the same margin repeats bell");
  g.touchKeys=true;Call("Draw");Set("lastKeyAt",-1f);var q=UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.isActiveAndEnabled&&b.name=="q");int length=g.typed.Length;q.onClick.Invoke();Check(g.typed.Length==length+1,"Touch key inserted twice");Check(keySource.clip.name.StartsWith("Typebar"),"Touch typing lacks mechanical sound");
  Call("Go",LettersGame.Phase.Title);return checks+" save migration, audio generation, margin, mute/volume, correction and touch assertions passed.";
 }
 public static string LargeTextChecks()
 {
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();checks=0;Set("replaying",true);Set("largeText",true);
  for(int d=0;d<7;d++)for(int v=0;v<LetterStory.Days[d].Length;v++)for(int c=0;c<3;c++){
   g.day=d;g.visitor=v;Call("Go",LettersGame.Phase.Choice);g.SelectFeeling(c);string target=(string)Get("target");
   for(int i=0;i<=target.Length;i++){g.typed=target.Substring(0,i);Call("UpdateTyping");Canvas.ForceUpdateCanvases();var t=(Text)Get("targetText");Check(t.fontSize==44&&t.preferredHeight<=t.rectTransform.rect.height+2,"Large typing page clips at "+d+"/"+v+"/"+c+"/"+i);}
   Check((int)Get("typingPage")==(int)Get("typingPages")-1,"Final typing page inaccessible");
  }
  Set("largeText",false);Call("Go",LettersGame.Phase.Title);return checks+" large-font character/page layout assertions passed.";
 }
}
