using System.Collections;
using UnityEngine;

public class PlaySampleDialogue : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Dialogue DialogueMono;
    int waitTime = 2;
    const string SampleText = @"Hello, This is some Sample Text for Dialogue.cs.
<typewriter=25>Variables and Tag names will be Capitalized.</typewriter>

<TypeWriter=10> This text should play a TypeWrite anim at SPEED 10 </TypeWriter>
<Wavy=3,5> The text here should make each character turn the whole text into a wave with a SPEED of 3 and AMPLITUDE of 5 </Wavy>
<Shake=4,1> The text here should Shake with a XFORCE of 4 and YFORCE of 1. </Shake>
<Pause=2> The Dialogue should Pause here for 3 SECONDS.
<Color=(175,175,175)> The Color of this text should be GRAY. </Color>
<ColorTransition=(255,255,255), (0,0,0)> The text here should be Gradient Colored from WHITE to BLACK. </ColorTransition>
<Magnify=3,2,4> The Text here should be magnified with a SPEED of 3, SCALE of 2, LENGTH of 4. </Magnify>";
    void Start()
    {   
        Debug.Log("Dialogue will start playing in ");
        DialogueMono = transform.GetComponent<Dialogue>();
        DialogueMono.QueueDialogue(SampleText, KeyCode.Space);
        StartCoroutine(DoPlayDialogue(waitTime));
    }
    IEnumerator DoPlayDialogue(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        DialogueMono.PlayNext();
    }
}
