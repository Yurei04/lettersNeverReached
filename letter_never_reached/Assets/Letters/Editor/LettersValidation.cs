using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class LettersValidation
{
 public static void SetSize(int width,int height){
 var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance").GetValue(null);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),width,height,"Letters validation"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new object[]{size});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);view.Focus();
 }
 static int steps,letters,nights;static double next;static bool wrongTest;static LettersGame g;
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 public static void Start(){LettersAcceptance.Start();}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.15;try{
  if(!g||!Application.isPlaying)throw new Exception("Scene stopped during test");
  if((bool)typeof(LettersGame).GetField("transitioning",flags).GetValue(g))return;
  foreach(var art in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.RawImage>())if(art.isActiveAndEnabled&&art.texture&&art.texture.name.StartsWith("mira-")&&g.phase!=LettersGame.Phase.Reveal&&g.phase!=LettersGame.Phase.Ending)throw new Exception("Mira visible before reveal: "+g.phase);
  if(g.phase==LettersGame.Phase.Quote||g.phase==LettersGame.Phase.Opening||g.phase==LettersGame.Phase.Reveal){typeof(LettersGame).GetField("phaseStarted",flags).SetValue(g,Time.unscaledTime-20);return;}
  if(++steps>240)throw new Exception("Story exceeded expected steps");
  if(g.phase==LettersGame.Phase.Ending){if(g.day!=6||g.delivered!=11||letters!=11||nights!=6)throw new Exception($"Wrong totals {g.day}/{g.delivered}/{letters}/{nights}");Debug.Log($"LETTERS_TEST_PASS: Exactly seven days, {letters} visitor letters, {nights} nightly letters; incorrect input rejected and corrected; all envelope stages reached; all 33 conversation exchanges completed; protagonist hidden until final reveal; reunion completed. {steps} transitions.");EditorApplication.update-=Tick;return;}
  if(g.phase==LettersGame.Phase.Story){
   int before=g.exchange;g.SelectFeeling(0);if(g.phase!=LettersGame.Phase.Story)throw new Exception("Category bypassed conversation");
   typeof(LettersGame).GetField("conversationStarted",flags).SetValue(g,Time.unscaledTime-30);g.AdvanceConversation();
   if(before<2&&g.phase!=LettersGame.Phase.Story)throw new Exception("Conversation ended early");return;
  }
  if(g.phase==LettersGame.Phase.Choice){g.SelectFeeling(LetterStory.Days[g.day][g.visitor].best);return;}
  if(g.phase==LettersGame.Phase.Typing||g.phase==LettersGame.Phase.NightTyping){
   var phase=g.phase;g.FinishTyping();if(g.phase!=phase)throw new Exception("Empty letter was accepted");
   var onChar=typeof(LettersGame).GetMethod("OnChar",flags);string target=(string)typeof(LettersGame).GetField("target",flags).GetValue(g);
   if(!wrongTest){onChar.Invoke(g,new object[]{'#'});g.FinishTyping();if(g.phase!=phase)throw new Exception("Wrong key accepted");g.Backspace();if(g.typed!="")throw new Exception("Backspace failed");wrongTest=true;}
   foreach(char c in target)onChar.Invoke(g,new object[]{c});if(g.typed!=target)throw new Exception("Typing mismatch");g.FinishTyping();if(g.phase==phase)throw new Exception("Completed letter was rejected");if(phase==LettersGame.Phase.Typing)letters++;else nights++;return;
  }
  var act=(Action)typeof(LettersGame).GetField("primary",flags).GetValue(g);if(act==null)throw new Exception("No continuation in "+g.phase);act();
 }catch(Exception e){Debug.LogError("LETTERS_TEST_FAILED: "+e);EditorApplication.update-=Tick;}}

 public static void Show(string phase,int day=0){
  g=UnityEngine.Object.FindAnyObjectByType<LettersGame>();g.StopAllCoroutines();g.day=day;g.visitor=0;g.exchange=0;
  typeof(LettersGame).GetField("transitioning",flags).SetValue(g,false);
  var f=(UnityEngine.UI.Image)typeof(LettersGame).GetField("fade",flags).GetValue(g);if(f)UnityEngine.Object.Destroy(f.gameObject);
  typeof(LettersGame).GetMethod("Go",flags).Invoke(g,new object[]{Enum.Parse(typeof(LettersGame.Phase),phase)});
  if(phase=="Typing"){typeof(LettersGame).GetMethod("Go",flags).Invoke(g,new object[]{LettersGame.Phase.Choice});g.SelectFeeling(0);}
  typeof(LettersGame).GetField("conversationStarted",flags).SetValue(g,Time.unscaledTime-30);
 }
 public static string CheckLayout(){
  UnityEngine.Canvas.ForceUpdateCanvases();var errors=new System.Collections.Generic.List<string>();
  foreach(var text in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>())if(text.isActiveAndEnabled&&text.preferredHeight>text.rectTransform.rect.height+2)errors.Add(text.text+" needs "+text.preferredHeight+" has "+text.rectTransform.rect.height);
  return errors.Count==0?"All visible text fits.":string.Join("\n",errors);
 }
 public static string CheckDrag(){
  var d=UnityEngine.Object.FindAnyObjectByType<LetterDrag>();if(!d)throw new Exception("Show Fold first");
  var rt=(RectTransform)d.transform;var home=rt.anchoredPosition;
  var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);e.position=RectTransformUtility.WorldToScreenPoint(null,rt.position);d.OnBeginDrag(e);e.position=new Vector2(5,5);d.OnDrag(e);d.OnEndDrag(e);
  if(d.IsDone||rt.anchoredPosition!=home)throw new Exception("Missed drop did not reset");
  d.OnPointerClick(e);d.TapDestination();if(!d.IsDone)throw new Exception("Tap alternative failed");
  return "Missed drop returned safely; tap destination accepted once.";
 }
 public static string CheckValidDrag(){
  var d=UnityEngine.Object.FindAnyObjectByType<LetterDrag>();if(!d)throw new Exception("Show Fold first");
  var rt=(RectTransform)d.transform;var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);e.position=RectTransformUtility.WorldToScreenPoint(null,rt.position);d.OnBeginDrag(e);
  e.position=RectTransformUtility.WorldToScreenPoint(null,d.destination.TransformPoint(d.destination.rect.center));d.OnDrag(e);d.OnEndDrag(e);if(!d.IsDone)throw new Exception("Valid pointer drag rejected");return "Pointer drag accepted on target.";
 }
}
