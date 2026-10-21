using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Improvements to the original illustrated game. Story data and existing scene references stay intact.
public partial class LettersGame
{
 const string SaveKey="LettersSaveV2";
 [Serializable] public class SavedLetter { public int day,visitor,style; public bool personal; public string text,address; }
 [Serializable] public class SaveData {
  public int version=3,day,visitor,phase,exchange,choice,delivered,understood,nightStep,style,introStep,unlocked;
  public string typed="",target=""; public bool inkReady,windowOpen,lampDim; public List<SavedLetter> letters=new List<SavedLetter>();
 }
 List<SavedLetter> keptLetters=new List<SavedLetter>();
 int unlocked,nightStep,envelopeStyle,introStep,archivePage;
 bool lampDim,settingsOpen,creditsOpen,replayOpen,inspecting,inkReady,windowOpen,replaying,reduceMotion,largeText;
 string inspectArea="Desk",feedback="",menuNotice="";
 float musicVolume=.65f,effectsVolume=.7f,textSpeed=37,saveAt=-1,feedbackUntil;
 RectTransform roomView; float lookOffset,lookTarget;
 bool OverlayOpen => archiveOpen||restartOpen||settingsOpen||creditsOpen||replayOpen||inspecting;
 bool CanInput => !transitioning&&!OverlayOpen&&phase!=Phase.Epilogue;
 Color EnvelopeTint => envelopeStyle==1?new Color(.77f,.85f,.8f):envelopeStyle==2?new Color(.88f,.77f,.69f):Color.white;
 void InitUpdates(){
  musicVolume=PlayerPrefs.GetFloat("LettersMusic",.65f);effectsVolume=PlayerPrefs.GetFloat("LettersEffects",.7f);textSpeed=PlayerPrefs.GetFloat("LettersTextSpeed",37);
  reduceMotion=PlayerPrefs.GetInt("LettersStill",0)==1;largeText=PlayerPrefs.GetInt("LettersLarge",0)==1;
  unlocked=PlayerPrefs.GetInt("LettersUnlocked",0);phase=Phase.Title;ApplyAudio();
 }
 void ApplyAudio(){piano.volume=musicVolume*.3f;sound.volume=effectsVolume;AudioListener.volume=muted?0:1;ApplyTypewriterVolume();}
 void SaveOptions(){PlayerPrefs.SetFloat("LettersMusic",musicVolume);PlayerPrefs.SetFloat("LettersEffects",effectsVolume);PlayerPrefs.SetFloat("LettersTextSpeed",textSpeed);PlayerPrefs.SetInt("LettersStill",reduceMotion?1:0);PlayerPrefs.SetInt("LettersLarge",largeText?1:0);PlayerPrefs.SetInt("LettersMuted",muted?1:0);PlayerPrefs.Save();ApplyAudio();}
 void UpdateUpdates(){
  if(saveAt>=0&&Time.unscaledTime>=saveAt){saveAt=-1;SaveProgress();}
  if(roomView){lookOffset=reduceMotion?lookTarget:Mathf.Lerp(lookOffset,lookTarget,Time.unscaledDeltaTime*5);roomView.anchoredPosition=new Vector2(lookOffset,0);}
 }
 void MarkSave(){saveAt=Time.unscaledTime+.4f;}
 void OnApplicationPause(bool paused){if(paused){SaveProgress();PlayerPrefs.Save();}}
 void OnApplicationFocus(bool focused){if(!focused)SaveProgress();}
 void OnApplicationQuit(){SaveProgress();}
 SaveData Snapshot(){return new SaveData{day=day,visitor=visitor,phase=(int)phase,exchange=exchange,choice=choice,delivered=delivered,understood=understood,nightStep=nightStep,style=envelopeStyle,introStep=introStep,unlocked=unlocked,typed=typed,target=target,inkReady=inkReady,windowOpen=windowOpen,lampDim=lampDim,letters=new List<SavedLetter>(keptLetters)};}
 void SaveProgress(){
  if(replaying||phase==Phase.Title)return;
  PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Snapshot()));PlayerPrefs.SetInt("LettersUnlocked",Mathf.Max(unlocked,day));PlayerPrefs.Save();
 }
 bool ValidSave(SaveData s){
  if(s==null||s.version!=3||s.day<0||s.day>6||s.visitor<0||s.visitor>=LetterStory.Days[s.day].Length||!Enum.IsDefined(typeof(Phase),s.phase)||s.phase==(int)Phase.Title||s.choice<0||s.choice>2||s.exchange<0||s.exchange>2||s.nightStep<0||s.nightStep>6||s.style<0||s.style>2||s.introStep<0||s.introStep>1||s.delivered<0||s.delivered>11||s.understood<0||s.understood>s.delivered||s.letters==null||s.letters.Count>17||s.unlocked<0||s.unlocked>7||s.typed==null||s.target==null)return false;
  var p=(Phase)s.phase;if(s.day==6&&(p==Phase.Night||p==Phase.NightTyping||p==Phase.NightSeal||p==Phase.Waiting))return false;
  if((p==Phase.Reveal||p==Phase.Ending||p==Phase.Epilogue)&&(s.day!=6||s.visitor!=1||s.delivered<10))return false;
  if(p==Phase.Typing&&s.target!=LetterStory.Days[s.day][s.visitor].letters[s.choice])return false;
  if(p==Phase.NightTyping&&s.target!=LetterStory.NightLetters[s.day])return false;
  if(p==Phase.Tutorial&&s.target!="Ada, I am here.")return false;
  if(s.typed.Length>s.target.Length+8||s.target.Length>1000)return false;
  foreach(var l in s.letters)if(l==null||l.day<0||l.day>6||l.text==null||l.address==null||l.style<0||l.style>2||l.visitor<0||l.visitor>=LetterStory.Days[l.day].Length||l.personal&&l.day==6)return false;
  return true;
 }
 bool TryReadSave(out SaveData data){data=null;try{if(!PlayerPrefs.HasKey(SaveKey))return false;data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));MigrateSave(data);return ValidSave(data);}catch{return false;}}
 void Restore(SaveData s){
  day=s.day;visitor=s.visitor;phase=(Phase)s.phase;exchange=s.exchange;choice=s.choice;delivered=s.delivered;understood=s.understood;nightStep=s.nightStep;envelopeStyle=s.style;introStep=s.introStep;unlocked=Mathf.Max(s.unlocked,PlayerPrefs.GetInt("LettersUnlocked",0));typed=s.typed;target=s.target;inkReady=s.inkReady;windowOpen=s.windowOpen;lampDim=s.lampDim;keptLetters=new List<SavedLetter>(s.letters);
  EventSystem.current?.SetSelectedGameObject(null);phaseStarted=Time.unscaledTime;conversationStarted=Time.unscaledTime-100;ClearOverlays();ResetCarriage();Draw();
 }
 public void ContinueGame(){
  replaying=false;
  if(TryReadSave(out var s)){Restore(s);return;}
  if(!PlayerPrefs.HasKey(SaveKey)&&PlayerPrefs.GetInt("LettersStarted",0)==1){day=Mathf.Clamp(PlayerPrefs.GetInt("LettersDay",0),0,6);visitor=0;delivered=Mathf.Clamp(PlayerPrefs.GetInt("LettersPosted",0),0,9);understood=Mathf.Clamp(PlayerPrefs.GetInt("LettersHeard",0),0,delivered);keptLetters.Clear();for(int i=0;i<day&&i<6;i++)keptLetters.Add(new SavedLetter{day=i,personal=true,text=LetterStory.NightLetters[i],address="Ada • Seventh Medical Station"});StartDay();return;}
  menuNotice="No usable save was found. Start begins a new story.";Draw();
 }
 void ClearOverlays(){archiveOpen=restartOpen=settingsOpen=creditsOpen=replayOpen=inspecting=false;roomView=null;}
 public void ReturnToMenu(){if(transitioning)return;SaveProgress();ClearOverlays();replaying=false;FadeTo(Phase.Title);}
 void NewGame(){
  StopAllCoroutines();if(fade)Destroy(fade.gameObject);fade=null;transitioning=false;ClearOverlays();replaying=false;
  day=visitor=delivered=understood=exchange=choice=nightStep=envelopeStyle=introStep=0;typed=target="";keptLetters.Clear();inkReady=windowOpen=false;FadeTo(Phase.Intro);
 }
 void DrawMenu(){
  Pic(root,office,0,0,width,height);Box(root,0,0,width,height,new Color(.035f,.065f,.06f,.72f));
  float w=tall?760:720,x=(width-w)/2,y=tall?265:112;
  Txt(root,"LETTERS\nNEVER REACHED",x,y,w,170,58,cream,FontStyle.Normal,TextAnchor.MiddleCenter);
  Txt(root,"A POST OFFICE • SEVEN DAYS OF WAITING",x,y+186,w,40,17,gold,FontStyle.Normal,TextAnchor.MiddleCenter);
  Txt(root,"Some words need a little help finding their way.",x,y+245,w,65,24,cream,FontStyle.Italic,TextAnchor.MiddleCenter);
  float by=y+340;Btn(root,"Start",x+90,by,w-180,62,()=>{if(TryReadSave(out _)||PlayerPrefs.GetInt("LettersStarted",0)==1){restartOpen=true;Draw();}else NewGame();},true,25);
  bool saved=TryReadSave(out var s)||(!PlayerPrefs.HasKey(SaveKey)&&PlayerPrefs.GetInt("LettersStarted",0)==1);
  Btn(root,saved?"Continue • Day "+((s!=null?s.day:PlayerPrefs.GetInt("LettersDay",0))+1):"Continue • No saved story",x+90,by+78,w-180,62,ContinueGame,false,23);
  Btn(root,"Settings",x+90,by+156,(w-198)/2,58,()=>{settingsOpen=true;Draw();});Btn(root,"Credits",x+108+(w-198)/2,by+156,(w-198)/2,58,()=>{creditsOpen=true;Draw();});
  if(unlocked>0)Btn(root,"Revisit a completed day",x+90,by+230,w-180,48,()=>{replayOpen=true;Draw();},false,19);
  Txt(root,menuNotice,x,y+650,w,70,20,cream,FontStyle.Normal,TextAnchor.MiddleCenter);
  DrawOverlays();
 }
 RectTransform Modal(string heading,float h=720){Box(root,0,0,width,height,new Color(0,0,0,.8f)).raycastTarget=true;float w=tall?810:850;var p=Pic(root,paper,(width-w)/2,(height-h)/2,w,h).rectTransform;Txt(p,heading,35,35,w-70,65,34,ink);if(!reduceMotion)StartCoroutine(PanelArrives(p));return p;}
 void DrawOverlays(){
  if(settingsOpen)DrawSettings();else if(creditsOpen){var p=Modal("Credits",510);Txt(p,"LETTERS NEVER REACHED\n\nMira & Ada's story of listening, love, and waiting.\n\nIllustrated scenes, local prop art, and a quiet\nprocedural piano score.\n\nThank you for giving these words your time.",35,125,p.sizeDelta.x-70,270,24,ink);Btn(p,"Back",35,420,p.sizeDelta.x-70,58,()=>{creditsOpen=false;Draw();},true);}
  else if(replayOpen)DrawReplay();
  if(archiveOpen)DrawSavedArchive();if(restartOpen)RestartDialog();
 }
 System.Collections.IEnumerator PanelArrives(RectTransform p){var cg=p.gameObject.AddComponent<CanvasGroup>();float t=0;while(t<.16f&&p){t+=Time.unscaledDeltaTime;cg.alpha=Mathf.Lerp(.65f,1,t/.16f);yield return null;}if(p)cg.alpha=1;}
 void VolumeRow(RectTransform p,string label,float y,float value,Action<float> change){float w=p.sizeDelta.x;Txt(p,label+"  "+Mathf.RoundToInt(value*100)+"%",35,y,w-260,50,23,ink);Btn(p,"−",w-210,y,75,48,()=>{change(Mathf.Clamp01(value-.1f));SaveOptions();Draw();});Btn(p,"+",w-120,y,75,48,()=>{change(Mathf.Clamp01(value+.1f));SaveOptions();Draw();});}
 void DrawSettings(){var p=Modal("Settings",770);float w=p.sizeDelta.x;VolumeRow(p,"Piano music",125,musicVolume,v=>musicVolume=v);VolumeRow(p,"Typewriter & ambience",195,effectsVolume,v=>effectsVolume=v);
  Btn(p,muted?"Sound disabled • Enable":"Disable all sound",35,273,w-70,55,()=>{muted=!muted;SaveOptions();Draw();});
  Btn(p,"Dialogue speed: "+(textSpeed>=1000?"Instant":textSpeed<30?"Gentle":"Standard"),35,346,w-70,55,()=>{textSpeed=textSpeed<30?37:textSpeed<1000?1000:22;SaveOptions();Draw();});
  Btn(p,"Reduced motion: "+(reduceMotion?"On":"Off"),35,419,w-70,55,()=>{reduceMotion=!reduceMotion;SaveOptions();Draw();});
  Btn(p,"Large writing: "+(largeText?"On":"Off"),35,492,w-70,55,()=>{largeText=!largeText;SaveOptions();Draw();});
  Txt(p,"No timer. Use Enter instead of dragging.\nEvery object also supports tap, then tap destination.",35,572,w-70,80,21,olive);
  Btn(p,"Back",35,677,w-70,58,()=>{settingsOpen=false;Draw();},true);
 }
 void DrawReplay(){var p=Modal("Revisit a completed day",800);Txt(p,"Replay is separate. Continue keeps your main story.",35,110,p.sizeDelta.x-70,55,21,olive);for(int i=0;i<=Mathf.Min(unlocked-1,6);i++){int d=i;Btn(p,"Day "+(d+1)+" • "+LetterStory.Themes[d],35,180+i*65,p.sizeDelta.x-70,54,()=>StartReplay(d));}Btn(p,"Back",35,720,p.sizeDelta.x-70,52,()=>{replayOpen=false;Draw();},true);}
 void StartReplay(int d){replaying=true;delivered=understood=0;for(int i=0;i<d;i++)delivered+=LetterStory.Days[i].Length;ClearOverlays();day=d;visitor=exchange=choice=nightStep=0;typed=target="";inkReady=windowOpen=false;keptLetters=new List<SavedLetter>();Go(Phase.Quote);}
 void DrawIntroduction(){
  Pic(root,office,0,0,width,height);Box(root,0,0,width,height,new Color(.02f,.04f,.04f,.75f));var p=Modal(introStep==0?"Before the first morning":"A place for words",tall?1000:760);float w=p.sizeDelta.x;
  string story=introStep==0?"You are Mira. Before this small post office, you were a medic.\n\nA bombardment took your voice and your left hand. Your mechanical hand has learned the weight of paper, the resistance of a key.\n\nAda worked beside you. She is still a medic, now stationed beyond the river.":"People come here with words they cannot find. You listen, choose the feeling beneath their story, and help them write.\n\nAt night, you write to Ada. For six days, there is no reply.\n\nThere is no timer. Take as long as each letter needs.";
  Txt(p,story,38,130,w-76,tall?590:435,tall?30:27,ink);DrawHands(tall?55:255,height-190,tall?660:1150,height-190,150);
  Btn(p,introStep==0?"Continue":"Try the typewriter",35,p.sizeDelta.y-100,w-70,62,()=>{if(introStep==0){introStep++;SaveProgress();Draw();}else{typed="";target="Ada, I am here.";Go(Phase.Tutorial);}},true);
 }
 void DrawTutorial(){
  Header("A few words first","TUTORIAL • YOUR PRACTICE PAGE");
  targetText=Txt(panel,"",30,214,panel.sizeDelta.x-60,tall&&touchKeys?80:150,largeText?44:38,ink);progressText=Txt(panel,"",30,tall&&touchKeys?300:365,panel.sizeDelta.x-60,80,20,olive);typedText=Txt(panel,"",30,tall&&touchKeys?360:440,panel.sizeDelta.x-60,62,23,ink);
  Txt(panel,"Match capitals and punctuation. Backspace corrects mistakes.",30,tall&&touchKeys?430:505,panel.sizeDelta.x-60,70,19,ink);
  Btn(panel,touchKeys?"Hide keys":"Touch keys",30,panel.sizeDelta.y-145,panel.sizeDelta.x-60,43,()=>{touchKeys=!touchKeys;Draw();});MainButton("Finish the practice line",FinishTyping,panel.sizeDelta.y-80);UpdateTyping();
 }
 void DrawTutorialPack(){Header("One small act of care","TUTORIAL • DRAG OR TAP");Body("Drag the page to the envelope. Or tap the page, then tap the envelope. Enter also completes a placement.",212,25,150);var dest=Pic(panel,envelopeArt,200,400,panel.sizeDelta.x-240,115).rectTransform;var item=Pic(panel,paper,40,535,150,85).rectTransform;ConnectDrag(item,dest,()=>StartDay(),false);MainButton("Place the practice page",()=>dragItem?.Accept(),panel.sizeDelta.y-90);}
 void Notify(string message){feedback=message;feedbackUntil=Time.unscaledTime+5;}
 void OpenInspection(string area="Desk"){if(transitioning)return;inspecting=true;inspectArea=area;lookTarget=lookOffset=0;Draw();}
 void DrawInspection(){
  // Bounded horizontal eye movement over an illustrated room; no 3D rotation or zoom.
  roomView=Rect("First-person room",root,0,0,width,height);Pic(roomView,office,-80,0,width+160,height);Box(roomView,-80,0,width+160,height,new Color(.04f,.06f,.10f,(Nighttime?.5f:.22f)+(lampDim?.15f:0)));
  if(inspectArea=="Window")DrawRoomWindow(roomView, width*.2f,180,width*.6f,tall?650:300);
  else if(inspectArea=="Bedroom"){
   Box(roomView,width*.13f,270,width*.74f,360,new Color32(78,61,48,255));Box(roomView,width*.16f,290,width*.68f,240,new Color32(128,137,125,255));Box(roomView,width*.2f,305,190,80,cream);Txt(roomView,"THE ROOM ABOVE THE OFFICE",80,180,width-160,50,30,cream);
   Btn(roomView,"Rest • after the letter is safe",width*.2f,tall?660:600,width*.6f,65,()=>{Notify(phase==Phase.Waiting?"The letter is safely recorded. You may let the night pass.":"First, finish tonight's letter at the desk.");Draw();});
  }else{
   PropInRoom(0,width*.25f,320,width*.5f,350);Btn(roomView,"Typewriter",width*.33f,tall?690:600,width*.34f,58,()=>{inspecting=false;if(phase==Phase.Night){typed="";target=LetterStory.NightLetters[day];Go(Phase.NightTyping);}else{Notify(IsTyping?"The page is ready. Type the highlighted line.":"Listen first, then choose the words for this page.");Draw();}},true);
   Btn(roomView,"Ink ribbon",80,240,200,58,()=>{inkReady=true;Notify("You turn the spool. The ink ribbon is ready.");SaveProgress();Draw();},inkReady);
   Btn(roomView,"Paper",80,320,180,58,()=>{Notify("One clean sheet, ready for the next letter.");Draw();});
   Btn(roomView,"Envelopes",width-300,240,220,58,()=>{envelopeStyle=(envelopeStyle+1)%3;Notify("Envelope selected: "+StyleName()+". Saved with your next letter.");SaveProgress();Draw();});
   Btn(roomView,"Stamps & sealing wax",width-350,320,270,58,()=>{Notify("Postage and sealing wax are ready at the letter preparation desk.");Draw();});
   Btn(roomView,lampDim?"Brighten lamp":"Dim lamp",80,430,180,58,()=>{lampDim=!lampDim;Notify(lampDim?"The lamp settles into a softer glow.":"You turn the shade toward the page. A small circle of warmth.");SaveProgress();Draw();});
   if(Nighttime)Btn(roomView,"Ada's photograph",width-350,430,270,58,()=>{Notify("Ada, before the river. The sunny chair is still waiting.");Draw();});
  }
  DrawHands(tall?90:320,height-320,tall?640:1080,height-320,185);
  Box(root,0,0,width,115,new Color32(30,40,35,250));Txt(root,"LOOK AROUND • "+inspectArea.ToUpper(),40,24,width-80,45,28,cream);Txt(root,"Your place in the letter is kept.",40,72,width-80,30,18,gold);
  Btn(root,"‹ Look left",30,height-210,200,55,()=>{lookTarget=Mathf.Clamp(lookTarget+45,-65,65);});Btn(root,"Look right ›",width-230,height-210,200,55,()=>{lookTarget=Mathf.Clamp(lookTarget-45,-65,65);});
  float bw=(width-100)/4;string[] areas={"Desk","Window","Bedroom"};for(int i=0;i<3;i++){string a=areas[i];Btn(root,a,30+i*(bw+10),height-125,bw,60,()=>{inspectArea=a;lookOffset=lookTarget=0;Draw();},inspectArea==a);}
  Btn(root,"Return",30+3*(bw+10),height-125,bw,60,()=>{inspecting=false;roomView=null;Draw();},true);
  Txt(root,feedback,50,height-(tall?390:300),width-100,tall?120:80,25,cream,FontStyle.Italic,TextAnchor.MiddleCenter);
 }
 void PropInRoom(int i,float x,float y,float w,float h){if(propImages!=null&&i<propImages.Length)Pic(roomView,propImages[i],x,y,w,h);}
 void UseProp(int i){
  EventSystem.current?.SetSelectedGameObject(null);
  if(i==0&&IsTyping){keyPulse=1;Notify("The page is ready. Type the highlighted line.");return;}
  if(i==0&&phase==Phase.Night){typed="";nightStep=0;target=LetterStory.NightLetters[day];Go(Phase.NightTyping);return;}
  if(i==1){lampDim=!lampDim;SaveProgress();Draw();return;}
  if((i==2&&phase==Phase.Stamp)||(i==3&&phase==Phase.Post)){dragItem?.Accept();return;}
  Notify(i==2?"Stamps are ready for a written, addressed letter.":i==3?"The outgoing tray receives sealed letters.":"Listen to the customer, then choose the words for your page.");OpenInspection("Desk");
 }
 void DrawNightWindow(){
  float x=tall?65:410,y=tall?170:180,w=tall?420:500,h=tall?310:295;
  Box(root,x-12,y-12,w+24,h+24,new Color32(86,65,49,255));Box(root,x,y,w,h,new Color32(34,52,68,255));
  for(int i=0;i<13;i++)Box(root,x+15+i*(w-30)/13,y+15+(i%4)*30,2,h*.5f,new Color(.6f,.7f,.8f,.3f));
  Box(root,windowOpen?x+w-12:x+w/2,y,12,h,gold);Box(root,x,y+h*.55f,w,10,gold);Box(root,x-22,y+h,w+44,20,new Color32(131,101,70,255));

 }
 void DrawRoomWindow(Transform par,float x,float y,float w,float h){
  Box(par,x-15,y-15,w+30,h+30,new Color32(89,65,48,255));Box(par,x,y,w,h,new Color32(35,53,67,255));
  for(int i=0;i<12;i++)Box(par,x+20+i*(w-40)/12,y+30+(i%4)*65,2,h*.3f,new Color(.65f,.76f,.8f,.3f));
  Box(par,windowOpen?x+w-14:x+w*.48f,y,14,h,new Color32(105,83,60,255));Box(par,x,y+h*.55f,w,14,new Color32(105,83,60,255));Box(par,x-25,y+h,w+50,24,new Color32(144,114,77,255));
  Btn(par,"Window dispatch box",x+20,y+h+45,w-40,65,()=>{if(phase==Phase.NightSeal&&nightStep>=5){nightStep=6;inspecting=false;Draw();}else{Notify("The dispatch box is beneath the sill. Finish and seal your letter at the desk.");Draw();}},true);
 }
 string StyleName(){return envelopeStyle==0?"Cream":envelopeStyle==1?"Sage":"Rose";}
 void StyleButtons(float y){float bw=(panel.sizeDelta.x-80)/3;for(int i=0;i<3;i++){int s=i;Btn(panel,new[]{"Cream","Sage","Rose"}[i],30+i*(bw+10),y,bw,43,()=>{envelopeStyle=s;SaveProgress();Draw();},s==envelopeStyle,17);}}
 void RecordLetter(bool personal){if(keptLetters.Exists(l=>l.day==day&&l.personal==personal&&(personal||l.visitor==visitor)))return;keptLetters.Add(new SavedLetter{day=day,visitor=visitor,personal=personal,style=envelopeStyle,text=personal?LetterStory.NightLetters[day]:V.letters[choice],address=personal?"Ada • Seventh Medical Station":V.address});}
 void DrawSavedArchive(){var p=Modal("Letters entrusted & kept",tall?1090:800);float w=p.sizeDelta.x;archivePage=Mathf.Clamp(archivePage,0,Mathf.Max(0,keptLetters.Count-1));if(keptLetters.Count==0)Txt(p,"No completed letters yet.\nYour first page is still waiting.",35,170,w-70,180,28,ink);else{var l=keptLetters[archivePage];Txt(p,$"DAY {l.day+1} • "+(l.personal?"MIRA TO ADA":"CUSTOMER LETTER")+"\n"+l.address,35,130,w-70,120,23,olive);Txt(p,l.text,35,290,w-70,280,largeText?33:29,ink);Txt(p,$"{archivePage+1} / {keptLetters.Count}   •   "+new[]{"Cream","Sage","Rose"}[l.style]+" envelope",35,p.sizeDelta.y-190,w-70,40,20,olive);Btn(p,"Previous",35,p.sizeDelta.y-140,(w-90)/2,48,()=>{archivePage=Mathf.Max(0,archivePage-1);Draw();});Btn(p,"Next",55+(w-90)/2,p.sizeDelta.y-140,(w-90)/2,48,()=>{archivePage=Mathf.Min(keptLetters.Count-1,archivePage+1);Draw();});}Btn(p,"Return",35,p.sizeDelta.y-75,w-70,52,()=>{archiveOpen=false;Draw();},true);}
 void DrawSeal(float btn){Header("Seal the envelope","5 / 6 • BEFORE DELIVERY");Body("Press the wax over the flap. The words are ready to travel.",213,25,100);var dest=Pic(panel,envelopeArt,90,345,panel.sizeDelta.x-160,140,EnvelopeTint).rectTransform;var seal=Box(panel,45,530,80,65,new Color32(139,79,59,255)).rectTransform;Txt(seal,"♥",0,0,80,65,30,cream,FontStyle.Normal,TextAnchor.MiddleCenter);ConnectDrag(seal,dest,()=>Go(Phase.Post),true);MainButton("Seal the envelope",()=>dragItem?.Accept(),btn);}
 void DrawNightWorkflow(float btn){
  if(nightStep==5){nightStep=6;SaveProgress();}
  string[] titles={"Fold your letter","An envelope for Ada","Address it to Ada","A little postage","Seal the flap","To the dispatch box","To the dispatch box"};
  string[] hints={"Fold the page along its crease.","Choose the paper, then tuck your letter inside.","Ada • Seventh Medical Station • Eastern Front",day>=4?"Routes are suspended. Keep the envelope ready.":"A stamp for the road east.","Press the wax into the closed flap.","The dispatch box waits beneath the sill.",day>=4?"Mail is suspended. Your letter will stay safe here.":"Leave it for collection. A letter can wait through rain."};
  Header(titles[nightStep],$"NIGHT {day+1} • {Mathf.Min(nightStep+1,6)} / 6");Body(hints[nightStep],210,24,108);

  RectTransform dest;if(nightStep==6){dest=Box(panel,90,340,panel.sizeDelta.x-160,140,new Color32(83,66,49,255)).rectTransform;Box(dest,12,18,dest.sizeDelta.x-24,35,new Color32(29,35,32,255));Txt(dest,"WINDOW DISPATCH BOX",15,75,dest.sizeDelta.x-30,55,17,cream,FontStyle.Normal,TextAnchor.MiddleCenter);}else dest=Pic(panel,nightStep==0?paper:envelopeArt,95,340,panel.sizeDelta.x-160,130,EnvelopeTint).rectTransform;
  if(nightStep==1)StyleButtons(475);
  RectTransform item;float sy=nightStep==1?530:515;
  if(nightStep==0||nightStep==1)item=Pic(panel,paper,40,sy,160,85).rectTransform;
  else if(nightStep==2){item=Box(panel,40,sy,panel.sizeDelta.x-90,60,cream).rectTransform;Txt(item,"Ada • Seventh Medical Station",8,6,item.sizeDelta.x-16,50,19,ink);}
  else if(nightStep==6)item=Pic(panel,envelopeArt,40,sy,170,90,EnvelopeTint).rectTransform;
  else {item=Box(panel,40,sy,75,65,new Color32(139,79,59,255)).rectTransform;Txt(item,nightStep==4?"♥":"07",0,0,75,65,28,cream,FontStyle.Normal,TextAnchor.MiddleCenter);}
  ConnectDrag(item,dest,()=>{if(nightStep==6){RecordLetter(true);windowOpen=false;Go(Phase.Waiting);}else{nightStep=nightStep==4?6:nightStep+1;SaveProgress();Draw();}},nightStep==3||nightStep==4);
  MainButton(nightStep==6?"Place in the dispatch box":"Complete this step",()=>dragItem?.Accept(),btn);
 }
}
