using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class LettersGame
{
 // Version 2 stored Ada as visitor zero. Keep an in-progress reunion at Ada;
 // only an unopened seventh morning begins with the new soldier encounter.
 void MigrateSave(SaveData s)
 {
  if(s==null||s.version!=2)return;
  if(s.day==6 && s.phase!=(int)Phase.Quote && s.phase!=(int)Phase.Morning && s.phase!=(int)Phase.Opening) s.visitor=1;
  if(s.letters!=null)foreach(var letter in s.letters)if(letter!=null&&letter.day==6&&!letter.personal)letter.visitor=1;
  if(s.nightStep==5)s.nightStep=6;
  s.version=3;
 }

 readonly List<int> writingLineStarts=new List<int>();
 int typingPage,typingPages,furthestBellPosition;
 void UpdateTypingPage(int correct)
 {
  // Measure the actual font at the current width. Show whole lines in large
  // type, advancing the page only when the caret leaves the visible lines.
  var settings=targetText.GetGenerationSettings(new Vector2(targetText.rectTransform.rect.width,10000));
  settings.richText=false;settings.fontStyle=FontStyle.Bold;settings.verticalOverflow=VerticalWrapMode.Overflow;
  settings.generateOutOfBounds=true;
  var generator=new TextGenerator();generator.Populate(target,settings);
  writingLineStarts.Clear();
  foreach(var line in generator.lines)writingLineStarts.Add(line.startCharIdx);
  if(writingLineStarts.Count==0)writingLineStarts.Add(0);
  int activeLine=0;
  for(int i=1;i<writingLineStarts.Count;i++)if(correct>=writingLineStarts[i])activeLine=i;
  int linesPerPage=Mathf.Max(1,Mathf.FloorToInt(targetText.rectTransform.rect.height/(targetText.fontSize*1.35f)));
  typingPage=activeLine/linesPerPage;
  typingPages=Mathf.CeilToInt((float)writingLineStarts.Count/linesPerPage);
  int first=typingPage*linesPerPage;
  string page="";
  // Explicit visual line breaks keep the moving bold caret from reflowing
  // neighbouring words, and make the margin bell match the line on the page.
  for(int line=first;line<Mathf.Min(first+linesPerPage,writingLineStarts.Count);line++)
  {
   int from=writingLineStarts[line],to=line+1<writingLineStarts.Count?writingLineStarts[line+1]:target.Length;
   if(line>first)page+="\n";
   int split=Mathf.Clamp(correct,from,to);
   page+="<color=#647755>"+Escape(target.Substring(from,split-from))+"</color>";
   if(correct>=from&&correct<to)page+="<color="+(correct<typed.Length?"#AD5946":"#A47A35")+"><b>"+Escape(target.Substring(split,1))+"</b></color>"+Escape(target.Substring(split+1,to-split-1));
   else if(split<to)page+=Escape(target.Substring(split,to-split));
  }
  // The inserted visual breaks are never part of the text the player types.
  targetText.text=page;
 }

 AudioSource typewriterKeys,typewriterMechanism;
 AudioClip[] mechanicalKeys;
 AudioClip spaceKey,backspaceKey,carriageReturn;
 int lastKeyVariant=-1;
 float lastKeyAt=-1;
 void InitTypewriter()
 {
  typewriterKeys=gameObject.AddComponent<AudioSource>();typewriterKeys.playOnAwake=false;
  typewriterMechanism=gameObject.AddComponent<AudioSource>();typewriterMechanism.playOnAwake=false;
  mechanicalKeys=new AudioClip[6];
  for(int i=0;i<mechanicalKeys.Length;i++)mechanicalKeys[i]=MechanicalClip("Typebar strike "+i,i,0,.115f);
  spaceKey=MechanicalClip("Space bar escapement",17,1,.14f);
  backspaceKey=MechanicalClip("Backspace ratchet",23,2,.13f);
  carriageReturn=MechanicalClip("Margin bell and carriage return",29,3,.85f);
 }
 AudioClip MechanicalClip(string name,int seed,int kind,float duration)
 {
  const int rate=44100;var samples=new float[Mathf.CeilToInt(duration*rate)];var rng=new System.Random(410+seed);
  float previousNoise=0;
  for(int i=0;i<samples.Length;i++)
  {
   float t=(float)i/rate,noise=(float)rng.NextDouble()*2-1,high=noise-previousNoise;previousNoise=noise;
   float v=0;
   if(kind==3)
   {
    // A struck metal bell with inharmonic partials, followed by the sliding
    // carriage, roller ratchet, and the stop at the left margin.
    v=(Mathf.Sin(2*Mathf.PI*1568*t)*.24f+Mathf.Sin(2*Mathf.PI*4077*t)*.065f)*Mathf.Exp(-t*6)*Mathf.Min(t/.0015f,1);
    if(t>.14f&&t<.53f){float u=t-.14f;v+=noise*.075f*Mathf.Sin(u/.39f*Mathf.PI);float ratchet=u% .026f;v+=high*.09f*Mathf.Exp(-ratchet*420);}
    float stop=t-.54f;if(stop>=0)v+=(noise*.18f+Mathf.Sin(stop*2*Mathf.PI*165)*.23f)*Mathf.Exp(-stop*70);
   }
   else
   {
    float pitch=kind==1?110:kind==2?340:175+seed*13;
    // Key bottoming, typebar contacting the platen, then the spring return.
    v=(high*.14f+Mathf.Sin(t*2*Mathf.PI*pitch)*.23f)*Mathf.Exp(-t*105);
    float strike=t-(kind==1?.019f:.011f);
    if(strike>=0)v+=(high*(kind==1?.07f:.26f)+Mathf.Sin(strike*2*Mathf.PI*(930+seed*27))*.1f)*Mathf.Exp(-strike*170);
    float release=t-.044f;if(release>=0)v+=(noise*.12f+Mathf.Sin(release*2*Mathf.PI*610)*.08f)*Mathf.Exp(-release*130);
   }
   samples[i]=Mathf.Clamp(v,-.85f,.85f)*Mathf.Min((duration-t)/.008f,1);
  }
  var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
 }
 void ApplyTypewriterVolume(){if(typewriterKeys)typewriterKeys.volume=effectsVolume*.75f;if(typewriterMechanism)typewriterMechanism.volume=effectsVolume*.65f;}
 void ResetCarriage(){furthestBellPosition=typed.Length;lastKeyAt=-1;}
 void TypewriterAction(bool erasing)
 {
  if(!typewriterKeys)return;
  // One voice per mechanism prevents accumulating clicks during fast input
  // or a native keyboard text replacement. Touch and physical keys share it.
  if(Time.unscaledTime-lastKeyAt>=.028f)
  {
   lastKeyAt=Time.unscaledTime;
   int variant=(lastKeyVariant+UnityEngine.Random.Range(1,mechanicalKeys.Length))%mechanicalKeys.Length;lastKeyVariant=variant;
   typewriterKeys.clip=erasing?backspaceKey:typed.Length>0&&typed[typed.Length-1]==' '?spaceKey:mechanicalKeys[variant];typewriterKeys.Play();
  }
  if(erasing||typed.Length<=furthestBellPosition)return;
  bool margin=typed==target;
  foreach(int start in writingLineStarts)if(start>furthestBellPosition&&start<=typed.Length)margin=true;
  furthestBellPosition=typed.Length;
  if(margin&&!typewriterMechanism.isPlaying){typewriterMechanism.clip=carriageReturn;typewriterMechanism.Play();}
 }

 const string FinalQuote="Some letters never reached their destination.\nBut you did.";
 void DrawEpilogue()
 {
  root.localScale=Vector3.one;Box(root,0,0,width,height,Color.black).raycastTarget=true;
  float w=tall?730:1080,h=tall?420:300;
  quoteText=Txt(root,"“"+FinalQuote+"”",(width-w)/2,(height-h)/2,w,h,tall?40:44,new Color(cream.r,cream.g,cream.b,0),FontStyle.Italic,TextAnchor.MiddleCenter);
 }
 void UpdateEpilogue(float elapsed)
 {
  // Two quiet seconds, a slow arrival, eight seconds to read, then silence.
  float opacity=Mathf.Min(Mathf.Clamp01((elapsed-2)/2.5f),Mathf.Clamp01((16.5f-elapsed)/4));
  if(quoteText)quoteText.color=new Color(cream.r,cream.g,cream.b,opacity);
  rain.volume=Mathf.MoveTowards(rain.volume,0,Time.unscaledDeltaTime*.16f);
  piano.volume=musicVolume*.3f*Mathf.Clamp01((16.5f-elapsed)/4);
  if(elapsed<12.5f&&Time.unscaledTime>=nextNote){piano.PlayOneShot(notes[elapsed<7?0:4],.32f);nextNote=Time.unscaledTime+5;}
  if(elapsed>=16.5f){piano.Stop();sound.Stop();typewriterKeys.Stop();typewriterMechanism.Stop();}
  if(elapsed>=19&&!transitioning){nextNote=Time.unscaledTime+5;ApplyAudio();ReturnToMenu();}
 }
}
