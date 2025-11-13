using System.Collections;
using UnityEngine;

public class StartCutscene : MonoBehaviour
{
    public static bool HasPlayedCutscene = false;
    public Dialogue dialogueMono;
    public CanvasGroup CanvaGroup;
    public GameObject cutsceneBg;
    public string cutsceneText = "...<break><typewriter=20>Where am I?</typewriter>";
    public float dialogueAutoPlayWait = 4;
    public float fadeDuration = 2;
    void Start()
    {
        if (!(dialogueMono && CanvaGroup && cutsceneBg)) { Debug.Log("vars not set for cutscene."); return; }
        if (!HasPlayedCutscene) StartCoroutine(DoCutscene());
    }

    IEnumerator DoCutscene()
    {
        HasPlayedCutscene = true;
        dialogueMono.Play(cutsceneText, dialogueAutoPlayWait);
        yield return new WaitForSeconds(dialogueAutoPlayWait * (1 + CountStringOccurrences(cutsceneText, "<break>")));
        
        float startAlpha = CanvaGroup.alpha;
        float targetAlpha = 0f;
        float currentTime = 0f;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            CanvaGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, currentTime / fadeDuration);
            yield return null; 
        }

        CanvaGroup.alpha = targetAlpha;
        FindFirstObjectByType<Player>().ObtainAchievement("First Steps"); 
        cutsceneBg.SetActive(false); 
    }
    public static int CountStringOccurrences(string text, string substring)
    {
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(substring, i)) != -1)
        {
            count++;
            i += substring.Length; 
        }
        return count;
    }
}
