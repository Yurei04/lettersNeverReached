using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class LettersGame
{
 public int exchange;
 float phaseStarted,conversationStarted,nextNote;
 bool transitioning;
 Text quoteText,dialogueText;
 string spokenLine;
 RawImage mouth,customerHands;
 Vector2 customerHandsRest;
 RectTransform[] fingerJoints=new RectTransform[4];
 RectTransform rightHandRect,paperMotion,revealGroup;
 Vector2 leftRest,rightRest;
 AudioSource piano;
 AudioClip[] notes;
 AudioClip paperSound;
 LetterDrag dragItem;
 Image fade;
 void InitImmersion(){
  canvas.GetComponent<Image>().color=Color.black;
  piano=gameObject.AddComponent<AudioSource>();piano.playOnAwake=false;piano.volume=.2f;
  notes=new AudioClip[5];float[] frequencies={220,261.63f,329.63f,392,293.66f};
  for(int k=0;k<notes.Length;k++){
   int rate=22050;float[] data=new float[rate*4];float f=frequencies[k];
   for(int i=0;i<data.Length;i++){float t=(float)i/rate;float a=Mathf.Min(t/.012f,1)*Mathf.Exp(-t*1.3f);data[i]=a*(Mathf.Sin(2*Mathf.PI*f*t)*.48f+Mathf.Sin(2*Mathf.PI*f*2.003f*t)*.19f*Mathf.Exp(-t*1.7f)+Mathf.Sin(2*Mathf.PI*f*3.997f*t)*.06f*Mathf.Exp(-t*3));}
   notes[k]=AudioClip.Create("Felt piano "+k,data.Length,1,rate,false);notes[k].SetData(data,0);
  }
  paperSound=Tone(175,.65f,2);nextNote=Time.unscaledTime+1;
 }
 void UpdateImmersion(){
  float elapsed=Time.unscaledTime-phaseStarted;
  if(phase==Phase.Epilogue){UpdateEpilogue(elapsed);return;}
  if(piano&&Time.unscaledTime>nextNote){int n=(day+(int)(Time.unscaledTime/3))%notes.Length;if(notes[n])piano.PlayOneShot(notes[n],phase==Phase.Quote?.65f:.3f);nextNote=Time.unscaledTime+(phase==Phase.Quote?2.8f:7.5f);}
  rain.volume=Mathf.MoveTowards(rain.volume,(phase==Phase.Quote?0:Nighttime?.07f:.12f)*effectsVolume,Time.unscaledDeltaTime*.1f);
  if(phase==Phase.Quote){
   if(quoteText){float a=Mathf.Min(Mathf.Clamp01((elapsed-1)/1.8f),Mathf.Clamp01((13-elapsed)/1.8f));quoteText.color=new Color(cream.r,cream.g,cream.b,a);}
   if(elapsed>=13&&!transitioning)FadeTo(Phase.Morning);
  }
  if(phase==Phase.Opening){root.localScale=Vector3.one*Mathf.Lerp(reduceMotion?1:1.035f,1,Mathf.Clamp01(elapsed/4));if(elapsed>4&&!transitioning)FadeTo(Phase.Story);}
  if(phase==Phase.Reveal){
   if(revealGroup){revealGroup.localScale=Vector3.one*Mathf.Lerp(reduceMotion?1:1.3f,1,Mathf.SmoothStep(0,1,elapsed/8));var cg=revealGroup.GetComponent<CanvasGroup>();cg.alpha=Mathf.Clamp01((elapsed-.5f)/2.5f);}
   if(elapsed>11&&!transitioning)FadeTo(Phase.Ending);
  }
  if(phase==Phase.Morning&&paperMotion&&!transitioning){float t=Mathf.SmoothStep(0,1,elapsed/1.7f);paperMotion.localScale=new Vector3(Mathf.Lerp(.08f,1,t),Mathf.Lerp(.9f,1,t),1);AnimateHands();}
  if(dialogueText!=null&&phase==Phase.Story&&!OverlayOpen){int count=Mathf.Min(spokenLine.Length,Mathf.FloorToInt((Time.unscaledTime-conversationStarted)*textSpeed));dialogueText.text="“"+spokenLine.Substring(0,count)+(count==spokenLine.Length?"”":"");bool speaking=count<spokenLine.Length;if(mouth)mouth.gameObject.SetActive(speaking&&Mathf.Sin(Time.unscaledTime*19)>0);if(customerHands){customerHands.texture=portrait.texture;customerHands.rectTransform.anchoredPosition=customerHandsRest+new Vector2(0,speaking?Mathf.Sin(Time.unscaledTime*2.4f)*3:0);}if(portraitRect)portraitRect.localRotation=Quaternion.Euler(0,0,(speaking?.5f:.18f)*Mathf.Sin(Time.unscaledTime*1.7f));}
 }
 void DrawQuote(){
  root.localScale=Vector3.one;Box(root,0,0,width,height,Color.black);
  float w=tall?730:1060,h=tall?440:250;
  quoteText=Txt(root,"“"+LetterConversations.Quotes[day]+"”",(width-w)/2,(height-h)/2,w,h,tall?39:40,new Color(cream.r,cream.g,cream.b,0),FontStyle.Italic,TextAnchor.MiddleCenter);
 }
 void FadeTo(Phase next){if(transitioning)return;StartCoroutine(Transition(next));}
 IEnumerator Transition(Phase next){
  transitioning=true;
  fade=Box(canvas.transform,0,0,Screen.width,Screen.height,Color.clear);var fr=fade.rectTransform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;fade.raycastTarget=true;
  float t=0;while(t<.65f){t+=Time.unscaledDeltaTime*(reduceMotion?3:1);fade.color=new Color(0,0,0,Mathf.Clamp01(t/.65f));yield return null;}
  root.localScale=Vector3.one;Go(next);if(next==Phase.Morning)Play(paperSound);if(next==Phase.Opening)Play(bellSound);
  t=0;while(t<1.1f){t+=Time.unscaledDeltaTime*(reduceMotion?3:1);fade.color=new Color(0,0,0,1-Mathf.Clamp01(t/1.1f));yield return null;}
  Destroy(fade.gameObject);fade=null;transitioning=false;
 }
 void DrawHands(float lx,float ly,float rx,float ry,float size){
  handRect=Pic(root,hand,lx,ly,size,size*.94f).rectTransform;rightHandRect=Pic(root,rightHand,rx,ry,size,size*.94f).rectTransform;
  leftRest=handRect.anchoredPosition;rightRest=rightHandRect.anchoredPosition;
  for(int i=0;i<4;i++){float xx=(107+i*28)/320f*size,yy=(125-i*11)/300f*size*.94f;var joint=Box(handRect,xx,yy,15/320f*size,10/300f*size*.94f,new Color32(67,84,78,255)).rectTransform;Box(joint,2,2,3,3,new Color32(204,202,175,255));fingerJoints[i]=joint;}
 }
 void AnimateHands(){
  float elapsed=Time.unscaledTime-phaseStarted;
  if(handRect){handRect.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.unscaledTime*22)*keyPulse*4);float entry=phase==Phase.Morning?(1-Mathf.SmoothStep(0,1,elapsed/1.7f))*170:0;handRect.anchoredPosition=leftRest+new Vector2(0,-entry+Mathf.Sin(Time.unscaledTime*23)*keyPulse*6);}
  if(rightHandRect){rightHandRect.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(Time.unscaledTime*23)*keyPulse*3);rightHandRect.anchoredPosition=rightRest+new Vector2(0,Mathf.Sin(Time.unscaledTime*24+1)*keyPulse*5);}
  for(int i=0;i<4;i++)if(fingerJoints[i])fingerJoints[i].localScale=new Vector3(1,1+Mathf.Sin(Time.unscaledTime*22+i)*keyPulse*.7f,1);
  keyPulse=Mathf.MoveTowards(keyPulse,0,Time.unscaledDeltaTime*5);
 }
 void DrawPhotograph(){
  float x=tall?560:660,y=tall?215:555,w=tall?220:140;Box(root,x-13,y-13,w+26,w+35,new Color32(78,57,40,255));Box(root,x-5,y-5,w+10,w+18,cream);Pic(root,FindPortrait("lover-soft"),x,y,w,w,new Color(.84f,.80f,.67f));Txt(root,"ADA • BEFORE THE RIVER",x-20,y+w+34,w+40,30,14,gold,FontStyle.Normal,TextAnchor.MiddleCenter);
 }
 void DrawNewspaper(){
  // The camera looks down: only the wooden desk and two hands surround the page.
  var desk=Pic(root,office,0,112,width,height-180);desk.uvRect=new Rect(0,0,1,.28f);
  float w=tall?790:1150,h=tall?1190:765,x=(width-w)/2,y=tall?155:140;
  panel=Pic(root,paper,x,y,w,h).rectTransform;paperMotion=panel;
  Txt(panel,"THE MORNING DISPATCH",35,26,w-70,60,tall?39:44,ink,FontStyle.Bold,TextAnchor.MiddleCenter);
  Box(panel,35,98,w-70,3,ink);Txt(panel,$"TOWN EDITION    /    DAY {day+1}    /    CIVILIAN PRESS",35,111,w-70,30,16,olive,FontStyle.Normal,TextAnchor.MiddleCenter);
  Txt(panel,LetterStory.Headlines[day],40,168,w-80,tall?160:118,tall?35:38,ink,FontStyle.Bold);
  if(tall){Pic(panel,hospital,40,345,w-80,220);Txt(panel,"EASTERN SECTOR • FIELD REPORT",40,579,w-80,30,14,olive);Txt(panel,LetterStory.Articles[day],40,641,w-80,410,26,ink);}
  else {Pic(panel,hospital,40,318,410,220);Txt(panel,"EASTERN SECTOR • FIELD REPORT",40,554,410,30,14,olive);Txt(panel,"A route can move faster\nthan a letter.",40,602,410,60,23,olive,FontStyle.Italic);Box(panel,478,317,1,330,gold);Txt(panel,LetterStory.Articles[day],510,317,w-555,335,24,ink);}
  MainButton("Close the newspaper • Begin the day",()=>{if(!transitioning)StartCoroutine(CloseNewspaper());},h-87);
  DrawHands(x-120,y+h-160,x+w-95,y+h-150,tall?235:260);
 }
 IEnumerator CloseNewspaper(){
  transitioning=true;Play(paperSound);float t=0;
  while(t<.65f){t+=Time.unscaledDeltaTime;if(paperMotion)paperMotion.localRotation=Quaternion.Euler(0,Mathf.Lerp(0,78,t/.65f),0);if(handRect)handRect.anchoredPosition=leftRest+new Vector2(t*160,0);yield return null;}
  transitioning=false;FadeTo(Phase.Opening);
 }
 void DrawOpening(){
  Box(root,0,0,width,tall?105:70,Color.black);Box(root,0,height-(tall?105:70),width,tall?105:70,Color.black);
  Prop(0,tall?160:500,tall?850:650,tall?600:640,360);DrawHands(tall?95:390,tall?1040:790,tall?590:900,tall?1040:800,230);
  Txt(root,day==6?"The rain has softened.\nYou unlock the door.":"The key turns. The bell settles.\nYou open the post office.",tall?100:350,tall?500:300,tall?700:900,150,32,cream,FontStyle.Italic,TextAnchor.MiddleCenter);
 }
 void AddSpeakingMouth(float size){
  if(!portraitRect)return;mouth=Pic(portraitRect,null,size*.486f,size*.603f,size*.038f,size*.019f,new Color32(112,73,53,255));mouth.gameObject.SetActive(false);
  customerHands=Pic(portraitRect,portrait.texture,size*.26f,size*.82f,size*.48f,size*.18f);customerHands.uvRect=new Rect(.26f,0,.48f,.18f);customerHandsRest=customerHands.rectTransform.anchoredPosition;
 }
 void DrawConversation(float btn){
  Header(V.name,V.role);spokenLine=LetterConversations.Exchanges[day][visitor][exchange];
  dialogueText=Txt(panel,"",30,212,panel.sizeDelta.x-60,tall?245:290,tall?28:26,ink);
  Txt(panel,LetterConversations.Cue(day,visitor,exchange),30,tall?474:525,panel.sizeDelta.x-60,100,20,olive,FontStyle.Italic);
  Txt(panel,$"LISTENING  {exchange+1} / 3",30,btn-38,panel.sizeDelta.x-60,26,14,olive);
  MainButton(exchange==2?"Choose the words they need":"Listen • Continue",AdvanceConversation,btn);
 }
 public void AdvanceConversation(){
  if(phase!=Phase.Story||transitioning)return;
  if(Time.unscaledTime-conversationStarted<spokenLine.Length/textSpeed){conversationStarted=Time.unscaledTime-spokenLine.Length/textSpeed-1;return;}
  if(exchange<2){exchange++;conversationStarted=Time.unscaledTime;SaveProgress();Draw();}else Go(Phase.Choice);
 }
 void DrawHandling(float btn){
  int n=(int)phase-(int)Phase.Fold;
  string[] titles={"Fold the letter","Tuck the words inside","Give it an address","One stamp, carefully","Let the letter go"};
  string[] hints={"Bring the lower edge to the crease.","Slip the folded page into the envelope.",V.address,day==4||IsReunion?"Press the seal over the flap.":"Press the stamp into the upper corner.",day==4?"Return it to Ruth, to keep beside his watch.":IsReunion?"Place it in Ada's waiting hands.":"Place it in the outgoing tray."};
  Header(titles[n],$"{(n==4?6:n+1)} / 6   •   A SMALL ACT OF CARE");Body(hints[n],211,23,100);
  float w=panel.sizeDelta.x,dy=318,dw=w-170;
  RectTransform dest;
  if(n==0){dest=Pic(panel,paper,100,dy,dw,115).rectTransform;Box(dest,0,102,dw,2,gold);Txt(dest,"FOLD TO THIS CREASE",10,28,dw-20,35,16,olive,FontStyle.Normal,TextAnchor.MiddleCenter);}
  else if(n==4){dest=Box(panel,85,dy,w-140,128,new Color32(100,78,55,255)).rectTransform;Box(dest,8,8,w-156,100,new Color32(58,57,45,255));Txt(dest,day==4?"RUTH'S KEEPSAKE":IsReunion?"ADA'S HANDS":"OUTGOING",10,35,w-160,40,20,cream,FontStyle.Normal,TextAnchor.MiddleCenter);}
  else {dest=Pic(panel,envelopeArt,85,dy,w-140,145,EnvelopeTint).rectTransform;if(n==3){Txt(dest,V.address,12,65,w-225,55,17,ink);dest=Box(dest,w-220,12,60,55,new Color(.45f,.4f,.3f,.25f)).rectTransform;}}
  if(n==1)StyleButtons(470);float sy=n==1?525:tall?475:515;RectTransform item;
  if(n==0||n==1){item=Pic(panel,paper,38,sy,175,90).rectTransform;for(int j=0;j<4;j++)Box(item,16,16+j*13,140,2,olive);}
  else if(n==2){item=Box(panel,38,sy,w-150,68,cream).rectTransform;Txt(item,V.address,9,8,w-168,54,17,ink,FontStyle.Italic);}
  else if(n==3){item=Box(panel,40,sy,74,65,new Color32(139,79,59,255)).rectTransform;Txt(item,day==4||IsReunion?"♥":"07",6,4,62,55,26,cream,FontStyle.Bold,TextAnchor.MiddleCenter);}
  else {item=Pic(panel,envelopeArt,38,sy,185,90,EnvelopeTint).rectTransform;Box(item,151,10,23,25,new Color32(139,79,59,255));}
  ConnectDrag(item,dest,()=>{if(phase==Phase.Post)CompleteVisitor();else if(phase==Phase.Stamp)Go(Phase.Seal);else Go((Phase)((int)phase+1));},n==3);
  Txt(panel,"Drag to the marked place, or tap the object then its destination.",30,btn-44,w-60,44,16,olive);
  MainButton("Enter • "+(n==0?"Fold":n==1?"Enclose":n==2?"Address":n==3?"Press":"Deliver"),()=>dragItem?.Accept(),btn);
 }
 void ConnectDrag(RectTransform item,RectTransform dest,Action finished,bool press){
  item.GetComponent<Graphic>().raycastTarget=true;
  if(handRect)handRect.gameObject.SetActive(false);if(rightHandRect)rightHandRect.gameObject.SetActive(false);
  var edge=item.gameObject.AddComponent<Shadow>();edge.effectColor=new Color(.2f,.16f,.1f,.55f);edge.effectDistance=new Vector2(3,-4);
  var rim=dest.gameObject.AddComponent<Shadow>();rim.effectColor=new Color(.2f,.16f,.1f,.45f);rim.effectDistance=new Vector2(3,-4);
  item.GetComponent<Graphic>().color=new Color(1.07f,1.07f,1.07f,1);
  var support=dest.sizeDelta.x<100?(RectTransform)dest.parent:dest;Pic(support,rightHand,support.sizeDelta.x-45,30,110,103);
  var movingHand=Pic(item,hand,-42,-20,105,96).rectTransform;movingHand.SetAsLastSibling();
  dragItem=item.gameObject.AddComponent<LetterDrag>();dragItem.destination=dest;dragItem.completed=()=>{if(!transitioning)StartCoroutine(FinishHandling(item,movingHand,finished,press));};
  dest.GetComponent<Graphic>().raycastTarget=true;var targetButton=dest.gameObject.AddComponent<Button>();targetButton.onClick.AddListener(()=>dragItem?.TapDestination());
  Text dropHint=null;dragItem.rejected=()=>{Play(keySound);if(!dropHint)dropHint=Txt(panel,"Missed the mark. Try again, or use the button.",30,280,panel.sizeDelta.x-60,40,16,new Color32(155,65,45,255));};
 }
 IEnumerator FinishHandling(RectTransform item,RectTransform movingHand,Action finished,bool press){
  transitioning=true;Play(press?stampSound:paperSound);float t=0;Vector2 hp=movingHand.anchoredPosition;
  while(t<.55f){t+=Time.unscaledDeltaTime;if(movingHand)movingHand.anchoredPosition=hp+new Vector2(0,-Mathf.Sin(t/.55f*Mathf.PI)*14);if(item&&!press)item.localScale=new Vector3(1,Mathf.Lerp(1,.65f,t/.55f),1);yield return null;}
  transitioning=false;finished();
 }
 void DrawNightHandling(float btn){
  Header("For Ada","SEVENTH MEDICAL STATION • EASTERN FRONT");Body(target,210,24,195);
  var dest=Pic(panel,envelopeArt,85,425,panel.sizeDelta.x-140,100).rectTransform;
  var seal=Box(panel,40,540,70,54,new Color32(139,79,59,255)).rectTransform;Txt(seal,"♥",4,0,62,52,27,cream,FontStyle.Normal,TextAnchor.MiddleCenter);
  ConnectDrag(seal,dest,()=>Go(Phase.Waiting),true);
  Txt(panel,"Drag the seal onto the envelope.",130,545,panel.sizeDelta.x-160,40,18,olive);
  MainButton("Enter • Seal and keep it",()=>dragItem?.Accept(),btn);
 }
 void DrawReveal(){
  // Only after Ada receives her letter may the camera leave Mira's eyes.
  Box(root,0,0,width,height,new Color(0,0,0,.25f));
  revealGroup=Rect("The first view of Mira",root,width/2,height/2,width,height);revealGroup.pivot=new Vector2(.5f,.5f);revealGroup.gameObject.AddComponent<CanvasGroup>().alpha=0;
  float size=tall?420:500,cx=width/2,yy=tall?430:210;
  Pic(revealGroup,FindPortrait("mira-soft"),cx-size+35,yy,size,size);
  Pic(revealGroup,FindPortrait("lover-soft"),cx-35,yy,size,size);
  // A shared counter, her familiar mechanical hand now attached to the woman we finally see.
  Box(revealGroup,0,yy+size*.94f,width,145,new Color32(103,74,49,255));Box(revealGroup,0,yy+size*.94f,width,7,gold);
  Envelope(revealGroup,cx-60,yy+size*.99f,150,83,true);
  Box(root,0,0,width,90,Color.black);Box(root,0,height-120,width,120,Color.black);
  Txt(root,"For the first time in seven days,\nyou do not have to reach across the distance.",tall?70:300,height-110,tall?760:1000,100,28,cream,FontStyle.Italic,TextAnchor.MiddleCenter);
 }
}
