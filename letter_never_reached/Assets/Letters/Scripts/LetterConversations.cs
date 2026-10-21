// Each visitor reveals their purpose, a personal memory, then the feeling they cannot name.
public static class LetterConversations
{
 public static readonly string[] Quotes={
 "We write as though tomorrow is promised, because today is all we have.",
 "The silence between two letters can become longer than the road between two hearts.",
 "Somewhere, someone is waiting for a reply that the world may never deliver.",
 "War does not always take people away at once. Sometimes, it teaches us to lose them slowly.",
 "Every letter I send carries someone else's hope. I wonder how much of mine is left.",
 "I have learned to recognize every sound in this room, except the one I am waiting for.",
 "Perhaps the world has not ended. Perhaps someone is still finding their way home."};
 public static readonly string[][][] Exchanges={
 new[]{
 new[]{"My son Eli writes that he is keeping warm. He has always said that, even when his fingers were blue.","Before he left, we planted beans along the fence. He made the stakes too tall. Said we ought to leave room for growing.","Could we tell him the garden is growing? Something ordinary. Something to come home to. I want him to picture a life after this."},
 new[]{"Dad missed the first snow. I saved some in a jar, but it turned into water.","We always made the first snowman together. I wanted to keep his half until he came back.","Will he think I forgot? Can we tell him I tried? I still have all the stories I was going to tell him."}},
 new[]{
 new[]{"My wife Nell is driving ambulances. Before she left, I said her patients mattered more than I did.","She used to leave her keys beside mine. Now I keep listening for them. I was frightened of the empty house, and I made it her fault.","That doesn't make what I said fair. I want to take responsibility. I don't want to ask her to make me feel better."},
 new[]{"They moved Eli's unit. I don't know where. The last letter came back with the address crossed out.","I've kept his place at the table. It feels foolish sometimes, setting out another spoon. But I cannot bear to put it away.","If this catches up with him, I want him to know there is still a place for him here. Wherever he is, the gate is unlatched."}},
 new[]{
 new[]{"He is alive. Eli is alive. This hospital slip says his leg is hurt. I have read it so often the fold is coming apart.","His letter says he is sorry he cannot march. Even as a boy, he apologized when he needed help.","As if I raised him to be useful instead of loved. Can we tell him he doesn't have to earn his way home?"},
 new[]{"My friend Sam says he cannot be brave another day. Everyone keeps telling him to be strong.","Before the war, when things were hard, we sat on the back step. I didn't fix anything. I just stayed until he could breathe again.","Can we tell him something else? Something smaller than courage. That he can rest, and let someone sit beside him."}},
 new[]{new[]{"Write that he was selfish to leave. Write that I never needed him. My son ought to know what he has done.","He used to mend the gate for me. I haven't touched it since. Our last words were an argument. I keep hearing my own voice.","I just want him to knock again. I don't know if we will agree. But I don't want the letter to shut the gate."}},
 new[]{new[]{"They brought his watch. There is nowhere to send this. I know. May I write it anyway?", "When I could not get out of bed, he made breakfast. He never called it kindness. He just asked how I wanted my tea.","I never thanked him for those mornings. I don't need to pretend he is coming home. I only need somewhere to put my love."}},
 new[]{new[]{"Mum says we might leave tomorrow. Dad won't know where to find us. Can a letter leave a trail?", "We're going to the western relief station. I can carry the jar myself. Dad will remember it, even without the snow.","Please put the name of the place in the letter. I miss home, but I want him to know where to look. We haven't stopped waiting."}},
 new[]{new[]{"I was stationed at the camp beyond the river. They called it lost. My sister will think that means everyone in it.","A medic kept us moving through the dark. I kept hearing my sister say, 'Write when you get there.' I never did.","Please put 'I am alive' first. Before my name, before anything. She has waited long enough to be afraid."},new[]{"Mira. It's me. I wrote every night. The bridge went before the letters did.","I carried people west, then walked. In the dark, I remembered the lamp in your window. I kept walking toward it.","I had so many things to tell you. Now I cannot find the first word. May I be your last customer?"}}
 };
 public static string Cue(int day,int visitor,int exchange){
  if(day==6&&visitor==0)return new[]{"He sets a battered identity tag on the counter. You draw a clean sheet toward him.","His hands shake. You leave yours beside the page and wait.","He looks up. You nod once, then roll the paper into the typewriter."}[exchange];
  if(day==6)return new[]{"Ada holds the edge of the counter. Your mechanical fingers go still.","She sets six rain-worn envelopes between you. You reach toward her.","She looks into your eyes. You steady a fresh sheet with your left hand."}[exchange];
  if(day==4)return new[]{"Ruth cups the watch in both hands. You draw a sheet toward her.","Her thumb traces the worn strap. You wait without interrupting.","She meets your eyes. You lay out the last good sheet."}[exchange];
  if(day==3)return new[]{"His shoulders are rigid. You keep your hands open on the desk.","He looks down at the creased letter. You give him time.","His grip loosens. You underline his last sentence on your notepad."}[exchange];
  return new[]{"They look down at their hands. You set your pen aside and listen.","Their voice grows quieter. You turn your palm upward: go on.","They look toward you. You nod, then turn to the typewriter."}[exchange];
 }
}
