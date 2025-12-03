using System.Collections;
using UnityEngine;

public class PlaySampleDialogue : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Dialogue DialogueMono;
    int waitTime = 3;
    [TextArea] public string SampleText = @"Hello, This is some Sample Text for Dialogue.cs. You can Press Enter to skip by default.
<typewriter=30>Variables and Tag names will be Capitalized in this text for easy viewing.</typewriter>

<TypeWriter=10> This text should play a TypeWrite anim at SPEED 10 and pause HERE.<Pause=5> Hello! </TypeWriter>
<typewriter=40><Wavy=3,5> The text here should make each character turn the whole text into a wave with a SPEED of 3 and AMPLITUDE of 5 </Wavy>
<Shake=2,4> The text here should Shake with a XFORCE of 2 and YFORCE of 4. </Shake>
<Color=(175,175,175)> The Color of this text should be GRAY. </Color>
<ColorGradient=(255,255,255), (0,0,0)> The text here should be Gradient Colored from WHITE to BLACK. </ColorGradient>
<ColorTransition=(255,255,255), (0,0,0)> The text here should be Transition Colored from WHITE to BLACK. </ColorTransition>
<Magnify=10,2,4> The Text here should be magnified with a SPEED of 10, SCALE of 2, LENGTH of 4. </Magnify></typewriter>
Press Space to Proceed.";
    [TextArea] public string SampleText2 = @"<typewriter=40><b><color=#00FF00>Never</color></b> gonna <Wavy=3,6>give you up</Wavy>,<Pause=1>
Never gonna <Shake=2,4>let you down</Shake>,<Pause=1>
Never gonna <color=#FF0000><i>run around</i></color> and desert you.<Pause=1>

Never gonna <Magnify=8,2,3>make you cry</Magnify>,<Pause=1>
Never gonna <Color=(0,128,255)>say goodbye</Color>,<Pause=1>
Never gonna <ColorTransition=(255,255,255),(0,0,0)>tell a lie and hurt you</ColorTransition><pause=2>
Rickroll, also Here's a bomb :) <explode>BOOM</explode>.
</typewriter>";
    [TextArea] public string SampleText3 = @"
    <typewriter=40><stutter=4,Oops>umm-m-m-m-mmm, I stuttered!
    <rain>Rainning Down On You</rain>     
    <quake> QUAKING, ANGRY</quake>      
    <slam>SLAMMMMMM DUNK</slam>     
    <collapse>SOOO tired...</collapse>     
    <gravity> WOAH </gravity>     
    <contruct>Building. Constructing.</contruct>     
    <drop>its a bird, its a plane, no, its just... No clue.</drop>     
    <explode> AN EXPLOSION </explode>     
    <wavy><colorgradient> colorful </colorgradient></wavy></typewriter>     
    ";
    public string SampleText4 = "That's it, there is more but its buggy.";
    void Start()
    {   
        Debug.Log("Dialogue will start playing in ");
        DialogueMono = transform.GetComponent<Dialogue>();
        DialogueMono.QueueDialogue(SampleText);
        DialogueMono.QueueDialogue(SampleText2);
        DialogueMono.QueueDialogue(SampleText3);
        DialogueMono.QueueDialogue(SampleText4);
        StartCoroutine(DoPlayDialogue(waitTime));
    }
    IEnumerator DoPlayDialogue(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        DialogueMono.PlayNext();
    }
}
