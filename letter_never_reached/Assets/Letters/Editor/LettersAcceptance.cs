using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

// Runs against the real scene and its UI/actions; keeps the player's snapshot isolated.
public static class LettersAcceptance
{
 static LettersGame g;static int steps,letters,nights,placements,checks;static double next;static string original;static int oldUnlock;static bool portraitMode,endingSeen,epilogueSeen;static float epilogueStarted;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(string n)=>typeof(LettersGame).GetField(n,F).GetValue(g);
 static void Set(string n,object v)=>typeof(LettersGame).GetField(n,F).SetValue(g,v);
 static object Call(string n,params object[] a)=>typeof(LettersGame).GetMethod(n,F).Invoke(g,a);
 static void Assert(bool yes,string why){if(!yes)throw new Exception(why);checks++;}
 static void Click(string label){var b=UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.isActiveAndEnabled&&b.name==label);Assert(b!=null,"Button missing: "+label);b.onClick.Invoke();}
 public static void Start(bool portrait=false){
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();Assert(g&&Application.isPlaying,"Open scene in play mode");original=PlayerPrefs.GetString("LettersSaveV2","");oldUnlock=PlayerPrefs.GetInt("LettersUnlocked",0);portraitMode=portrait;LettersValidation.SetSize(portrait?900:1600,portrait?1500:1000);
  steps=letters=nights=placements=checks=0;endingSeen=epilogueSeen=false;Set("reduceMotion",true);g.touchKeys=portrait;g.ResetGame();next=EditorApplication.timeSinceStartup+1;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Debug.Log("ACCEPTANCE_STARTED "+(portrait?"portrait":"landscape"));
 }
 static void SaveRoundTrip(){
  Call("SaveProgress");string json=PlayerPrefs.GetString("LettersSaveV2");Assert(!string.IsNullOrEmpty(json),"No checkpoint");var d=JsonUtility.FromJson<LettersGame.SaveData>(json);Assert((bool)Call("ValidSave",d),"Invalid generated checkpoint: "+g.phase);var p=g.phase;var text=g.typed;var day=g.day;var exchange=g.exchange;
  Call("Go",LettersGame.Phase.Title);g.ContinueGame();Assert(g.phase==p&&g.day==day&&g.typed==text&&g.exchange==exchange,"Continue changed task state");
 }
 static void Layout(){Canvas.ForceUpdateCanvases();var fit=LettersValidation.CheckLayout();Assert(fit=="All visible text fits.",g.phase+" layout: "+fit);}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.12;try{
  Assert(g&&Application.isPlaying,"Scene stopped");if((bool)Get("transitioning"))return;
  foreach(var im in UnityEngine.Object.FindObjectsByType<RawImage>())Assert(!im.isActiveAndEnabled||!im.texture||!im.texture.name.StartsWith("mira-")||g.phase==LettersGame.Phase.Reveal||g.phase==LettersGame.Phase.Ending,"Early protagonist reveal");
  if(g.phase==LettersGame.Phase.Quote||g.phase==LettersGame.Phase.Opening||g.phase==LettersGame.Phase.Reveal){Set("phaseStarted",Time.unscaledTime-20);return;}
  if(g.phase==LettersGame.Phase.Epilogue){
   if(!epilogueSeen){epilogueSeen=true;epilogueStarted=Time.unscaledTime;Assert(!UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.isActiveAndEnabled),"Buttons visible over final quote");}
   float elapsed=Time.unscaledTime-(float)Get("phaseStarted");
   var quote=(Text)Get("quoteText");
   if(elapsed>5&&elapsed<12)Assert(quote.color.a>.99f,"Final quote not held visibly");
   if(elapsed>17){Assert(quote.color.a==0,"Quote did not fade out");Assert(!((AudioSource)Get("piano")).isPlaying,"Ending did not become silent");Assert(((AudioSource)Get("rain")).volume==0,"Rain audible on ending black");}
   return;
  }
  if(endingSeen&&epilogueSeen&&g.phase==LettersGame.Phase.Title){Assert(Time.unscaledTime-epilogueStarted>=17,"Ending rushed to menu");FinishSuite();return;}
  Assert(++steps<300,"Progression exceeded 300 steps");Layout();SaveRoundTrip();
  if(g.phase==LettersGame.Phase.Intro){Click((int)Get("introStep")==0?"Continue":"Try the typewriter");return;}
  if(g.phase==LettersGame.Phase.Story){int ex=g.exchange;g.SelectFeeling(0);Assert(g.phase==LettersGame.Phase.Story,"Bypassed dialogue");Assert(!UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.isActiveAndEnabled&&(b.name=="Look around"||b.name=="−")),"Dialogue still has inspection control");if(g.day==6)Assert(((RawImage)Get("portrait")).texture.name.StartsWith(g.visitor==0?"soldier-":"lover-"),"Wrong Day 7 portrait");Set("conversationStarted",Time.unscaledTime-100);g.AdvanceConversation();return;}
  if(g.phase==LettersGame.Phase.Choice){g.SelectFeeling(LetterStory.Days[g.day][g.visitor].best);return;}
  if(g.phase==LettersGame.Phase.Typing||g.phase==LettersGame.Phase.NightTyping||g.phase==LettersGame.Phase.Tutorial){
   var p=g.phase;g.FinishTyping();Assert(g.phase==p,"Incomplete typing accepted");Call("OnChar",'#');g.FinishTyping();Assert(g.phase==p,"Wrong key accepted");g.Backspace();Assert(g.typed=="","Backspace did not remove mistake");
   Set("settingsOpen",true);Call("Draw");Call("OnChar",'X');g.Backspace();Assert(g.typed=="","Settings leaked keyboard input");Click("Back");
   string target=(string)Get("target");foreach(char c in target){Call("OnChar",c);Layout();}Assert(((Text)Get("targetText")).fontSize>=38,"Typing text too small");Call("OnChar",'x');g.FinishTyping();Assert(g.phase==p,"Extra character accepted");g.Backspace();Assert(g.typed==target,"Correction failed");SaveRoundTrip();g.FinishTyping();Assert(g.phase!=p,"Completed typing not accepted");if(p==LettersGame.Phase.Typing)letters++;else if(p==LettersGame.Phase.NightTyping)nights++;return;
  }
  if(g.phase==LettersGame.Phase.Ending){Assert(g.day==6&&g.delivered==11&&letters==11&&nights==6,"Incorrect seven-day totals");var d=JsonUtility.FromJson<LettersGame.SaveData>(PlayerPrefs.GetString("LettersSaveV2"));Assert(d.letters.Count==17,"Completed letters not recorded");Assert(d.unlocked==7,"Replay unlock incomplete");Assert(d.letters.Any(l=>l.day==6&&l.visitor==0&&l.text.StartsWith("Elsie, I am alive.")),"Soldier letter not saved");Assert(d.letters.Any(l=>l.day==6&&l.visitor==1&&l.address.StartsWith("Mira")),"Reunion letter missing");endingSeen=true;Click("Let the words rest");return;}
  if(g.phase==LettersGame.Phase.Night||g.phase==LettersGame.Phase.NightSeal)Assert(!UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.isActiveAndEnabled&&(b.name.Contains("Open the")||b.name.Contains("Bedroom window"))),"Night window button remains");
  var drag=UnityEngine.Object.FindObjectsByType<LetterDrag>().FirstOrDefault(d=>d.isActiveAndEnabled);
  if(drag){placements++;var rt=(RectTransform)drag.transform;var home=rt.anchoredPosition;var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rt.position)};drag.OnBeginDrag(e);e.position=new Vector2(-100,-100);drag.OnDrag(e);drag.OnEndDrag(e);Assert(!drag.IsDone&&rt.anchoredPosition==home,"Invalid placement did not restore item");
   if(placements%3==0){drag.OnPointerClick(e);drag.TapDestination();}else if(placements%3==1){e.position=RectTransformUtility.WorldToScreenPoint(null,rt.position);drag.OnBeginDrag(e);e.position=RectTransformUtility.WorldToScreenPoint(null,drag.destination.TransformPoint(drag.destination.rect.center));drag.OnDrag(e);drag.OnEndDrag(e);}else ((Action)Get("primary"))();Assert(drag.IsDone,"Valid placement failed");drag.Accept();return;
  }
  var act=(Action)Get("primary");Assert(act!=null,"No continuation: "+g.phase);act();
 }catch(Exception e){Debug.LogError("ACCEPTANCE_FAILED "+g.phase+" / day "+g.day+": "+e);Cleanup();}}
 static void FinishSuite(){
  string saved=PlayerPrefs.GetString("LettersSaveV2");Call("StartReplay",0);Call("SaveProgress");Assert(PlayerPrefs.GetString("LettersSaveV2")==saved,"Replay overwrote main save");
  PlayerPrefs.SetString("LettersSaveV2","{broken");g.ContinueGame();Assert(g.phase==LettersGame.Phase.Quote,"Corrupt save changed scene");
  Cleanup();Debug.Log($"ACCEPTANCE_PASS {(portraitMode?"portrait":"landscape")}: 7 days, 11 customer letters, 6 personal letters, {placements} placements, {steps} checkpoints, {checks} assertions. Soldier then Ada, Continue, typing accuracy and large paged text, dialogue controls, nightly dispatch without window buttons, replay, reveal, full timed quote and silent menu return.");
 }
 static void Cleanup(){EditorApplication.update-=Tick;if(original=="")PlayerPrefs.DeleteKey("LettersSaveV2");else PlayerPrefs.SetString("LettersSaveV2",original);PlayerPrefs.SetInt("LettersUnlocked",oldUnlock);PlayerPrefs.Save();Set("replaying",true);Call("Go",LettersGame.Phase.Title);}
 public static string ChoiceChecks(){
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();Set("replaying",true);g.StopAllCoroutines();Set("transitioning",false);int count=0;
  for(int day=0;day<7;day++)for(int visitor=0;visitor<LetterStory.Days[day].Length;visitor++)for(int c=0;c<3;c++){
   g.day=day;g.visitor=visitor;Call("Go",LettersGame.Phase.Choice);g.SelectFeeling(c);Assert((string)Get("target")==LetterStory.Days[day][visitor].letters[c],"Wrong emotional template");g.typed=(string)Get("target");g.FinishTyping();Assert(g.phase==LettersGame.Phase.Reaction,"No reaction for option");Layout();count++;
  }Call("Go",LettersGame.Phase.Title);return count+" emotional letter options and reactions passed.";
 }
 public static string RoomChecks(){
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();Set("replaying",true);g.day=0;g.visitor=0;Call("Go",LettersGame.Phase.Night);Call("OpenInspection","Desk");Click("Ink ribbon");Assert((bool)Get("inkReady"),"Ink did not activate");Click("Envelopes");Assert((int)Get("envelopeStyle")==1,"Envelope style did not change");Click("Dim lamp");Assert((bool)Get("lampDim"),"Lamp did not dim");Click("Brighten lamp");Click("Paper");Click("Stamps & sealing wax");Click("Ada's photograph");Click("Window");Layout();Assert(!UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.isActiveAndEnabled&&b.name=="Open the window"),"Window control remains");Click("Window dispatch box");Assert(g.phase==LettersGame.Phase.Night,"Empty mailbox advanced task");Click("‹ Look left");Click("Look right ›");Click("Bedroom");Layout();Click("Rest • after the letter is safe");Assert(g.phase==LettersGame.Phase.Night,"Rest bypassed nightly task");Click("Return");return "Ink, envelopes, lamp, paper, stamps, photograph, window, mailbox, room navigation and task guards passed.";
 }
 public static string InterfaceChecks(){g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();Call("Go",LettersGame.Phase.Title);Click("Settings");Layout();Click("Disable all sound");Assert(g.muted,"Mute did not apply");Click("Sound disabled • Enable");Click("Back");Click("Credits");Layout();Click("Back");return "Settings, mute, credits and back navigation passed.";}
}
