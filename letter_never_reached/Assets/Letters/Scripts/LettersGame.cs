using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public partial class LettersGame : MonoBehaviour
{
 public Texture2D office,paper,hospital,hand,envelopeArt,rightHand,glow;
 public Texture2D[] portraits;
 public Texture2D[] propImages;
 public GameObject[] propPrefabs;
 public enum Phase { Title, Morning, Story, Choice, Typing, Reaction, Fold, Envelope, Address, Stamp, Post, Night, NightTyping, NightSeal, Waiting, Ending, Quote, Opening, Reveal, Intro, Tutorial, TutorialPack, Seal, Epilogue }
 public Phase phase=Phase.Quote;
 public int day=0, visitor=0, delivered=0, understood=0, choice=0;
 public string typed="";
 public bool touchKeys, muted;
 Canvas canvas; RectTransform root, panel,portraitRect,handRect; RawImage portrait; Text targetText,progressText,typedText; Font font;
 float width=1600,height=1000; bool tall; Vector2 lastSize; float blinkAt,blinkEnd,keyPulse; bool blink;
 string target=""; Action primary; AudioSource sound,rain; AudioClip keySound,stampSound,bellSound; TouchScreenKeyboard native;
 Color ink=new Color32(62,65,53,255), cream=new Color32(238,221,185,255),olive=new Color32(74,91,75,255),gold=new Color32(190,158,99,255);
 List<RectTransform> drops=new List<RectTransform>(); bool archiveOpen,restartOpen;
 LetterStory.Visitor V {get{return LetterStory.Days[day][visitor];}}
 bool IsTyping {get{return phase==Phase.Typing||phase==Phase.NightTyping||phase==Phase.Tutorial;}}
 bool IsReunion => day==6 && visitor==LetterStory.Days[6].Length-1;
 bool Nighttime {get{return phase==Phase.Night||phase==Phase.NightTyping||phase==Phase.NightSeal||phase==Phase.Waiting;}}
 void Awake(){
  font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  var go=new GameObject("Letters canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var surround=go.AddComponent<UnityEngine.UI.Image>();surround.color=new Color32(33,40,35,255);surround.raycastTarget=false;root=Rect("Illustrated stage",go.transform,0,0,1600,1000);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);
  var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
  if(EventSystem.current==null){var ev=new GameObject("Input",typeof(EventSystem),typeof(InputSystemUIInputModule));}
  sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;rain=gameObject.AddComponent<AudioSource>();rain.loop=true;rain.volume=.12f;
  keySound=Tone(130,.045f,1);stampSound=Tone(75,.15f,2);bellSound=Tone(660,.45f,0);rain.clip=RainClip();rain.Play();
  if(Keyboard.current!=null)Keyboard.current.onTextInput+=OnChar;
  day=Mathf.Clamp(PlayerPrefs.GetInt("LettersDay",0),0,6);delivered=PlayerPrefs.GetInt("LettersPosted",0);understood=PlayerPrefs.GetInt("LettersHeard",0);
  if(PlayerPrefs.GetInt("LettersStarted",0)==0)day=delivered=understood=0;muted=PlayerPrefs.GetInt("LettersMuted",0)==1;AudioListener.volume=muted?0:1;InitImmersion();InitTypewriter();InitUpdates();phaseStarted=Time.unscaledTime;Layout();
 }
 void OnDestroy(){if(Keyboard.current!=null)Keyboard.current.onTextInput-=OnChar;}
 AudioClip Tone(float hz,float len,int kind){int rate=22050;float[] a=new float[(int)(rate*len)];var rng=new System.Random(13);for(int i=0;i<a.Length;i++){float t=(float)i/rate;float env=Mathf.Exp(-t*(kind==0?10:40));a[i]=(Mathf.Sin(t*hz*Mathf.PI*2)*(kind==0?.4f:.18f)+(float)(rng.NextDouble()-.5)*(kind==0?0:.5f))*env;}var c=AudioClip.Create("Desk sound",a.Length,1,rate,false);c.SetData(a,0);return c;}
 AudioClip RainClip(){int rate=22050;float[] a=new float[rate*3];var r=new System.Random(4);float v=0;for(int i=0;i<a.Length;i++){v=v*.7f+(float)(r.NextDouble()-.5)*.3f;a[i]=v;}var c=AudioClip.Create("Rain at the window",a.Length,1,rate,false);c.SetData(a,0);return c;}
 void Play(AudioClip clip){if(clip)sound.PlayOneShot(clip,.6f);}
 void Update(){
  UpdateUpdates();UpdateImmersion();
  if(lastSize!=new Vector2(Screen.width,Screen.height))Layout();
  if(transitioning||phase==Phase.Quote||phase==Phase.Opening||phase==Phase.Reveal||phase==Phase.Epilogue)return;
  if(Keyboard.current!=null&&CanInput){if(IsTyping&&Keyboard.current.enterKey.wasPressedThisFrame&&!archiveOpen&&!restartOpen)FinishTyping();if(IsTyping&&Keyboard.current.backspaceKey.wasPressedThisFrame)Backspace();else if(!IsTyping&&Keyboard.current.enterKey.wasPressedThisFrame&&!archiveOpen&&!restartOpen)primary?.Invoke();}
  if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&OverlayOpen){ClearOverlays();Draw();}
  if(IsTyping&&CanInput&&native!=null&&native.active&&typed!=native.text){int before=typed.Length;typed=native.text.Substring(0,Mathf.Min(native.text.Length,target.Length+8));MarkSave();UpdateTyping();TypewriterAction(typed.Length<before);}
  if(portraitRect!=null){var p=portraitRect.anchoredPosition;p.y=-portraitBaseY+Mathf.Sin(Time.time*1.5f)*2;portraitRect.anchoredPosition=p;}
  if(Time.time>blinkAt&&!blink){blink=true;blinkEnd=Time.time+.13f;SetPortrait();}if(blink&&Time.time>blinkEnd){blink=false;blinkAt=Time.time+UnityEngine.Random.Range(3,6);SetPortrait();}
  if(!reduceMotion)AnimateHands();
  foreach(var d in drops){var p=d.anchoredPosition;p.y-=Time.deltaTime*(50+d.sizeDelta.y*4);p.x-=Time.deltaTime*9;if(p.y<-(tall?650:560)){p.y=-100;p.x=UnityEngine.Random.Range(tall?80:430,tall?820:1190);}d.anchoredPosition=p;}
 }
 float portraitBaseY;
 void Layout(){lastSize=new Vector2(Screen.width,Screen.height);tall=Screen.height>Screen.width;width=tall?900:1600;height=tall?1500:1000;canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(width,height);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=(float)Screen.width/Screen.height>width/height?1:0;root.sizeDelta=new Vector2(width,height);root.anchoredPosition=Vector2.zero;touchKeys=touchKeys||Application.isMobilePlatform;Draw();}
 RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 Image Box(Transform par,float x,float y,float w,float h,Color c){var r=Rect("Panel",par,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
 RawImage Pic(Transform par,Texture tex,float x,float y,float w,float h,Color? color=null){var r=Rect(tex?tex.name:"Art",par,x,y,w,h);var im=r.gameObject.AddComponent<RawImage>();im.texture=tex;im.color=color??Color.white;im.raycastTarget=false;return im;}
 Text Txt(Transform par,string s,float x,float y,float w,float h,int size,Color c,FontStyle style=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft){var r=Rect("Text",par,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=s;t.fontSize=size;t.color=c;t.fontStyle=style;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;t.supportRichText=true;return t;}
 Button Btn(Transform par,string label,float x,float y,float w,float h,Action act,bool accent=false,int size=22){var r=Rect(label,par,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=accent?olive:new Color32(216,201,166,255);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};var cb=b.colors;cb.highlightedColor=new Color(1.12f,1.1f,1.02f);cb.pressedColor=new Color(.76f,.76f,.68f);b.colors=cb;Txt(r,label,10,0,w-20,h,size,accent?cream:ink,FontStyle.Normal,TextAnchor.MiddleCenter);b.onClick.AddListener(()=>{if(transitioning)return;EventSystem.current?.SetSelectedGameObject(null);if(!IsTyping)Play(keySound);act();});return b;}
 void MainButton(string label,Action action,float y=610){primary=action;Btn(panel,label,30,y,panel.sizeDelta.x-60,62,action,true);}
 void Go(Phase next){phaseStarted=Time.unscaledTime;if(next==Phase.Story){exchange=0;conversationStarted=Time.unscaledTime;}if(native!=null){native.active=false;native=null;}phase=next;if(IsTyping)ResetCarriage();archiveOpen=restartOpen=false;inspecting=false;roomView=null;SaveProgress();Draw();}
 public void StartDay(){visitor=0;nightStep=0;inkReady=windowOpen=false;unlocked=Mathf.Max(unlocked,day);Go(Phase.Quote);}
 void Draw(){
  for(int i=root.childCount-1;i>=0;i--){root.GetChild(i).gameObject.SetActive(false);Destroy(root.GetChild(i).gameObject);}drops.Clear();primary=null;targetText=progressText=typedText=null;portrait=null;portraitRect=handRect=null;rightHandRect=null;customerHands=null;dialogueText=null;mouth=null;quoteText=null;revealGroup=null;paperMotion=null;dragItem=null;
  if(phase==Phase.Epilogue){DrawEpilogue();return;}
  if(phase==Phase.Title){DrawMenu();return;}
  if(phase==Phase.Intro){DrawIntroduction();return;}
  if(inspecting){DrawInspection();return;}
  if(phase==Phase.Quote){DrawQuote();return;}
  // The room is one illustrated stage; the paper is the interface.
  float bgW=tall?2400:1600;Pic(root,office,tall?-750:0,0,bgW,height);
  if(day>2)Box(root,0,0,width,height,new Color(.04f,.07f,.09f,(day-2)*.055f));
  if(Nighttime){var nightDesk=Pic(root,office,0,112,width,height-112);nightDesk.uvRect=new Rect(0,0,1,.28f);Box(root,0,0,width,height,new Color(.025f,.055f,.12f,.47f));}
  if(day>=3){Box(root,tall?630:1110,180,23,220,new Color32(94,75,52,220));Box(root,tall?705:1175,180,23,220,new Color32(94,75,52,220));}
  for(int i=0;i<26;i++){float x=UnityEngine.Random.Range(tall?80:430,tall?820:1190),y=UnityEngine.Random.Range(100,tall?650:560);var d=Box(root,x,y,1.6f,UnityEngine.Random.Range(12,29),new Color(.65f,.75f,.78f,.24f)).rectTransform;drops.Add(d);}
  Pic(root,glow,tall?-150:-70,tall?450:340,tall?800:750,tall?660:650,new Color(1,1,1,lampDim?.3f:1));
  // Stationery props, rendered from the local low-poly objects.
  Prop(1,tall?-20:10,tall?490:420,tall?240:300,tall?290:360);
  Prop(3,tall?650:1275,tall?570:640,260,165);
  Prop(2,tall?730:1420,tall?530:550,115,150);
  if(Nighttime){DrawNightWindow();DrawPhotograph();}
  int letters=Mathf.Min(day,6);for(int i=0;i<letters;i++)Envelope(root,tall?70:100+(i%3)*58,tall?585+i*8:795+(i/3)*26,110,64,false);
  if(phase!=Phase.Title&&phase!=Phase.Morning&&phase!=Phase.Ending&&phase!=Phase.Reveal&&phase!=Phase.Opening ){
   float px=tall?(IsTyping&&touchKeys?300:200):465,py=tall?(IsTyping&&touchKeys?135:200):164,sz=tall?(IsTyping&&touchKeys?300:500):530;if(!Nighttime&&phase!=Phase.Tutorial&&phase!=Phase.TutorialPack){portrait=Pic(root,null,px,py,sz,sz);portraitRect=portrait.rectTransform;portraitBaseY=py;SetPortrait();AddSpeakingMouth(sz);}
   if(!IsTyping||!touchKeys){Prop(0,tall?210:365,tall?555:644,tall?550:660,tall?310:365);DrawHands(tall?150:330,tall?600:775,tall?570:805,tall?605:790,tall?190:235);}else {Prop(0,tall?235:350,tall?305:490,tall?430:600,tall?235:340);DrawHands(tall?160:330,tall?340:620,tall?565:805,tall?345:620,tall?160:200);}
  }
  if(phase==Phase.Reveal){DrawReveal();return;}
  if(phase==Phase.Opening){DrawOpening();return;}
  Box(root,0,0,width,112,new Color(.10f,.13f,.115f,.92f));
  Txt(root,"LETTERS NEVER REACHED",tall?35:55,23,tall?530:720,40,tall?28:30,cream);
  Txt(root,"A POST OFFICE  /  SEVEN DAYS OF WAITING",tall?35:57,67,tall?550:650,30,14,gold);
  Txt(root,$"DAY {day+1:00} / 07",tall?645:1350,24,220,38,28,cream);
  Txt(root,LetterStory.Themes[day].ToUpper(),tall?647:1352,67,200,27,15,gold);
  Btn(root,muted?"Sound off":"Sound on",tall?35:55,height-57,130,38,()=>{muted=!muted;AudioListener.volume=muted?0:1;SaveOptions();Draw();},false,16);
  Btn(root,"Letters",tall?180:197,height-57,108,38,()=>{archiveOpen=true;Draw();},false,16);
  Btn(root,"Menu",tall?303:317,height-57,108,38,ReturnToMenu,false,16);
  Txt(root,tall?"No timer.":"TYPE to write   •   CLICK to choose   •   ENTER to continue   •   No timer",tall?710:780,height-49,tall?160:760,33,tall?15:17,cream,FontStyle.Normal,TextAnchor.MiddleRight);
  Btn(root,"Settings",tall?425:437,height-57,110,38,()=>{settingsOpen=true;Draw();},false,16);
  if(phase!=Phase.Ending&&phase!=Phase.Morning&&phase!=Phase.Story&&phase!=Phase.Choice&&phase!=Phase.Reaction)Btn(root,"Look around",tall?550:560,height-57,140,38,()=>OpenInspection(),false,16);
  if(phase==Phase.Title){Title();}
  else if(phase==Phase.Morning){Morning();}
  else if(phase==Phase.Ending){Ending();}
  else{
   if(!tall){Txt(root,Nighttime?"AFTER CLOSING":"CIVILIAN LETTER SERVICE",65,160,350,30,17,gold);Txt(root,Nighttime?"Six letters.\nOne address.":"Some words need\na little help\nfinding their way.",65,205,340,180,32,cream,FontStyle.Italic);Box(root,65,418,245,1,gold);Txt(root,Nighttime?$"{day} letters before this one.\nStill no reply.":$"{delivered} letters entrusted\n{day} nights without a reply",65,442,290,80,20,cream);Txt(root,LetterStory.Notices[day],65,545,270,72,15,gold);}
   MakePanel();DrawPhase();
  }
  if(IsTyping&&touchKeys)KeyboardPanel();
  DrawOverlays();
 }
 void Prop(int i,float x,float y,float w,float h){if(propImages!=null&&i<propImages.Length&&propImages[i]!=null){var im=Pic(root,propImages[i],x,y,w,h);if(phase!=Phase.Quote&&phase!=Phase.Opening&&phase!=Phase.Reveal){im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{if(CanInput)UseProp(i);});}}}
 void SetPortrait(){if(portrait==null)return;string who=V.portrait;string mood=blink?"blink":(phase==Phase.Reaction||phase==Phase.Post||day==6)?"soft":"sad";foreach(var t in portraits)if(t!=null&&t.name==who+"-"+mood){portrait.texture=t;return;}}
 void MakePanel(){float x=tall?45:1010,y=tall?(IsTyping&&touchKeys?430:706):165,w=tall?810:535,h=tall?(IsTyping&&touchKeys?650:704):755;Box(root,x+8,y+9,w,h,new Color(0,0,0,.25f));panel=Pic(root,paper,x,y,w,h).rectTransform;Box(panel,25,24,3,24,gold);Txt(panel,Nighttime?"PRIVATE CORRESPONDENCE":"AT THE COUNTER",40,26,w-70,30,16,olive);}
 void Title(){DrawMenu();}
 Texture2D FindPortrait(string n){foreach(var p in portraits)if(p&&p.name==n)return p;return null;}
 void Morning(){DrawNewspaper();}
 void Header(string title,string sub){Txt(panel,title,30,75,panel.sizeDelta.x-60,60,tall?37:34,ink);Txt(panel,sub,31,136,panel.sizeDelta.x-62,50,17,olive);Box(panel,30,185,panel.sizeDelta.x-60,1,new Color(.4f,.4f,.3f,.3f));}
 void Body(string s,float y=212,int size=25,float h=290){Txt(panel,s,30,y,panel.sizeDelta.x-60,h,tall?size+2:size,ink);}
 void DrawPhase(){
  float ph=panel.sizeDelta.y,btn=ph-92;
  if(phase==Phase.Tutorial){DrawTutorial();}
  else if(phase==Phase.TutorialPack){DrawTutorialPack();}
  else if(phase==Phase.Seal){DrawSeal(btn);}
  else if(phase==Phase.Story){DrawConversation(btn);}
  else if(phase==Phase.Choice){Header("What needs to be said?",V.name+"  /  CHOOSE A FEELING");Body("“"+LetterConversations.Exchanges[day][visitor][2]+"”",207,tall?23:22,tall?202:264);Txt(panel,"There is no timer. Choose what you heard.",30,tall?422:475,panel.sizeDelta.x-60,40,17,olive);for(int i=0;i<3;i++){int c=i;Btn(panel,V.feelings[i],30,(tall?472:520)+i*63,panel.sizeDelta.x-60,53,()=>SelectFeeling(c),i==0,23);}}
  else if(IsTyping){Header(phase==Phase.NightTyping?"Dear Ada,":V.feelings[choice],phase==Phase.NightTyping?"ONE MORE LETTER • NO REPLY":V.name+"  /  TYPE THE LETTER BELOW");targetText=Txt(panel,"",30,214,panel.sizeDelta.x-60,tall?172:245,largeText?44:38,ink);progressText=Txt(panel,"",30,tall?396:474,panel.sizeDelta.x-60,42,17,olive);Box(panel,30,tall?435:525,panel.sizeDelta.x-60,2,gold);typedText=Txt(panel,"",30,tall?451:540,panel.sizeDelta.x-60,48,19,ink);Btn(panel,touchKeys?"Hide keys":"Touch keys",30,btn-61,(panel.sizeDelta.x-70)/2,45,()=>{touchKeys=!touchKeys;Draw();},false,17);Btn(panel,"Erase last",40+(panel.sizeDelta.x-70)/2,btn-61,(panel.sizeDelta.x-70)/2,45,Backspace,false,17);Btn(panel,"Show completed letter",30,btn,panel.sizeDelta.x-60,62,FinishTyping,true,21);UpdateTyping();}
  else if(phase==Phase.Reaction){Header(V.name,"THE WORDS, RETURNED TO THEIR OWNER");Body("“"+V.reactions[choice]+"”",213,26,250);Txt(panel,choice==V.best?"You heard the feeling beneath the story.":"The words landed differently than you intended.\nYou can listen again and revise them.",30,470,panel.sizeDelta.x-60,90,19,olive,FontStyle.Italic);Btn(panel,"Revise the feeling",30,btn-61,panel.sizeDelta.x-60,45,()=>Go(Phase.Choice),false,18);MainButton(IsReunion?"Give Ada the letter":"Prepare the envelope",()=>Go(Phase.Fold),btn);}
  else if(phase>=Phase.Fold&&phase<=Phase.Post){Packaging(btn);}
  else if(phase==Phase.Night){Header("The room above the office","NIGHT "+(day+1)+"  /  MIRA & ADA");Body(LetterStory.NightIntros[day],213,25,300);Txt(panel,"Your left hand steadies the page.\nYour right finds the first key.",30,530,panel.sizeDelta.x-60,80,21,olive,FontStyle.Italic);MainButton("Write to Ada",()=>{typed="";nightStep=0;target=LetterStory.NightLetters[day];Go(Phase.NightTyping);},btn);}
  else if(phase==Phase.NightSeal){DrawNightWorkflow(btn);}
  else if(phase==Phase.Waiting){Header("Your letter is safe",$"{day+1} LETTERS RECORDED  /  DISPATCH BOX CHECKED");Body(day==5?"No news of Ada arrives.\n\nMira leaves the light on, not because she knows, but because she does not.\n\nTomorrow, she will open the door again.":"The stairwell settles. Footsteps pass the door.\n\nMira looks up.\n\nThen the footsteps fade, and the rain goes on.",213,26,320);MainButton(replaying?"Return to menu":"Let the night pass",()=>{if(replaying){ReturnToMenu();return;}day++;StartDay();},btn);}
 }
 public void SelectFeeling(int index){if(phase!=Phase.Choice||index<0||index>=V.feelings.Length)return;choice=index;typed="";target=V.letters[index];Go(Phase.Typing);}
 void OnChar(char c){if(!IsTyping||!CanInput||char.IsControl(c))return;if(typed.Length<target.Length+8){typed+=c;keyPulse=1;MarkSave();UpdateTyping();TypewriterAction(false);}}
 public void Backspace(){if(!IsTyping||!CanInput)return;MarkSave();if(typed.Length==0)return;typed=typed.Substring(0,typed.Length-1);if(native!=null)native.text=typed;keyPulse=1;UpdateTyping();TypewriterAction(true);}
 string Escape(string s){return s.Replace("&","&amp;").Replace("<","‹").Replace(">","›");}
 void UpdateTyping(){if(targetText==null)return;int correct=0;while(correct<typed.Length&&correct<target.Length&&typed[correct]==target[correct])correct++;UpdateTypingPage(correct);progressText.text=correct==target.Length&&typed==target?"Every word is ready. Show the letter.":correct<typed.Length?"A slipped key. Erase back to the highlighted letter.":$"{correct} / {target.Length} characters   •   Next: "+(correct>=target.Length?"erase extra characters":target[correct]==' '?"space":target[correct].ToString());string end=typed.Length>36?"…"+typed.Substring(typed.Length-36):typed;typedText.text=Escape(end)+"<color=#927944> |</color>";}
 public void FinishTyping(){if(!IsTyping||!CanInput)return;if(typed!=target){if(progressText)progressText.text="Finish the letter exactly as written. Take your time.";return;}Go(phase==Phase.Tutorial?Phase.TutorialPack:phase==Phase.NightTyping?Phase.NightSeal:Phase.Reaction);}
 void Envelope(Transform par,float x,float y,float w,float h,bool isSealed){Box(par,x+4,y+5,w,h,new Color(0,0,0,.18f));Pic(par,envelopeArt,x,y,w,h);}
 void Packaging(float btn){DrawHandling(btn);}
 public void CompleteVisitor(){if(phase!=Phase.Post||transitioning)return;RecordLetter(false);delivered++;if(choice==V.best)understood++;if(IsReunion){unlocked=7;FadeTo(Phase.Reveal);return;}visitor++;if(visitor<LetterStory.Days[day].Length){Play(bellSound);Go(Phase.Story);}else{visitor=0;FadeTo(Phase.Night);}}
 void Ending(){float x=tall?45:280,y=tall?170:155,w=tall?810:1040,h=tall?1220:790;panel=Pic(root,paper,x,y,w,h).rectTransform;Txt(panel,"THE SEVENTH DAY",40,35,w-80,40,18,olive,FontStyle.Normal,TextAnchor.MiddleCenter);Txt(panel,"At last, an answer.",40,93,w-80,90,tall?49:58,ink,FontStyle.Italic,TextAnchor.MiddleCenter);Pic(panel,FindPortrait("mira-soft"),tall?25:200,tall?205:185,tall?390:310,tall?390:310);Pic(panel,FindPortrait("lover-soft"),tall?380:500,tall?205:185,tall?390:310,tall?390:310);Txt(panel,"Mira opens the drawer. Six letters, still tied with thread.\nAda lays six rain-worn envelopes beside them.\n\nNeither set ever reached its address.\nTonight, they read them together.",55,tall?630:505,w-110,tall?300:150,tall?30:24,ink,FontStyle.Normal,TextAnchor.MiddleCenter);Txt(panel,$"Seven days. {delivered} letters entrusted.\nThank you for listening.",40,tall?975:645,w-80,70,20,olive,FontStyle.Italic,TextAnchor.MiddleCenter);MainButton("Let the words rest",()=>FadeTo(Phase.Epilogue),tall?1100:705);}
 void KeyboardPanel(){float x=tall?35:40,y=tall?1090:703,w=tall?830:900,h=tall?310:235;Box(root,x-12,y-15,w+24,h+20,new Color(.13f,.17f,.145f,.98f));string[] rows={"qwertyuiop","asdfghjkl","zxcvbnm"};float kw=w/10-5,kh=tall?70:43;for(int r=0;r<3;r++){float offset=r==0?0:r==1?kw*.45f:kw*1.4f;for(int j=0;j<rows[r].Length;j++){char c=rows[r][j];Btn(root,c.ToString(),x+offset+j*(kw+5),y+r*(kh+5),kw,kh,()=>OnChar(c),false,24);}}Btn(root,":",x+kw*8.8f,y+2*(kh+5),kw*1.2f,kh,()=>OnChar(':'));float yy=y+3*(kh+5);Btn(root,"Shift",x,yy,kw*1.5f,kh,()=>{upper=!upper;Draw();},upper,18);Btn(root,",",x+kw*1.6f,yy,kw,kh,()=>OnChar(','));Btn(root,"Space",x+kw*2.7f,yy,kw*3.8f,kh,()=>OnChar(' '),false,19);Btn(root,".",x+kw*6.6f,yy,kw,kh,()=>OnChar('.'));Btn(root,"'",x+kw*7.7f,yy,kw,kh,()=>OnChar('\''));Btn(root,"Erase",x+kw*8.8f,yy,kw*1.2f,kh,Backspace,false,17);
  // Capital letters use a persistent Shift key, like a typewriter.
  if(upper){foreach(Transform child in root){var b=child.GetComponent<Button>();if(b&&b.name.Length==1&&char.IsLetter(b.name[0])){char c=char.ToUpperInvariant(b.name[0]);b.GetComponentInChildren<Text>().text=c.ToString();b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>{OnChar(c);upper=false;Draw();});}}}
 }
 bool upper;
 void Archive(){Box(root,0,0,width,height,new Color(0,0,0,.74f)).raycastTarget=true;float w=tall?820:900,x=(width-w)/2,y=tall?170:130;var p=Pic(root,paper,x,y,w,tall?1130:740).rectTransform;Txt(p,"Letters kept beside the lamp",35,35,w-70,65,36,ink);Txt(p,"MIRA TO ADA  /  THE UNANSWERED LETTERS",35,109,w-70,35,16,olive);int count=phase==Phase.Ending?6:Mathf.Min(day+(phase==Phase.Waiting?1:0),6);if(count==0)Txt(p,"The drawer is empty. The first letter waits for tonight.",35,190,w-70,100,25,ink);for(int i=0;i<count;i++){float yy=170+i*(tall?132:76);Txt(p,$"{i+1:00}",35,yy,45,35,20,olive);Txt(p,LetterStory.NightLetters[i],92,yy,w-130,tall?118:70,tall?23:20,ink);}Btn(p,"Return to the desk",35,tall?1030:645,w-70,60,()=>{archiveOpen=false;Draw();},true);}
 void RestartDialog(){Box(root,0,0,width,height,new Color(0,0,0,.7f)).raycastTarget=true;float w=tall?780:650,x=(width-w)/2,y=(height-370)/2;var p=Pic(root,paper,x,y,w,370).rectTransform;Txt(p,"Begin the seven days again?",35,40,w-70,75,33,ink);Txt(p,"This returns to the first morning. Your current place will be lost.",35,125,w-70,100,24,ink);Btn(p,"Keep my place",35,270,(w-85)/2,60,()=>{restartOpen=false;Draw();});Btn(p,"Begin again",50+(w-85)/2,270,(w-85)/2,60,ResetGame,true);}
 public void ResetGame(){NewGame();}
}
