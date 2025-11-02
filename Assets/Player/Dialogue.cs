#pragma warning disable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


/// <summary> 
/// Generates text for dialogues or any styled text. This uses TMP.<br/>
/// Needs to be attached to a CANVAS.
/// Tags do not have to be capitalized. <br/>
///
/// <code>
/// Use PlayNext to play the next text in queue.
/// Use QueueDialogue to queue the next text to play.
/// Use SetDialogue to set the current dialogue(s).
/// 
/// &lt;TypeWriter=3&gt; This text should play a TypeWrite anim at SPEED 3 &lt;/TypeWriter&gt;
/// &lt;Wavy=3,5&gt; The text here should make each character turn the whole text into a wave with a SPEED of 3 and AMPLITUDE of 5 &lt;/Wavy&gt;
/// &lt;Shake=4,1&gt; The text here should Shake with a XFORCE of 4 and YFORCE of 1. &lt;/Shake&gt;
/// &lt;Pause=3&gt; The Dialogue should Pause here for 3 SECONDS.
/// &lt;Color=(175,175,175)&gt; The Color of this text should be Default + GRAY. &lt;/Color&gt;
/// &lt;ColorTransition=(255,255,255), (0,0,0)&gt; The text here should be Gradient Colored from WHITE to BLACK. &lt;/ColorTransition&gt;
/// &lt;Magnify=3,2,4&gt; The Text here should be magnified with a SPEED of 3, SCALE of 2, LENGTH of 4. &lt;/Magnify&gt;
/// 
/// TMP has default tags:
/// &lt;b&gt; for bold
/// &lt;i&gt; for italics
/// &lt;u&gt; for underline
/// &lt;color&gt; for color (uses =#hex)
/// etc...
/// Full tmp tags at https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextSupportedTags.html
/// </code>
/// 
/// </summary>
public class Dialogue : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string InitialDialogueText = "";
    [SerializeField] private Transform CharacterHeadshotTransform;

    // If these variables aren't set then nothing will happen to the text style.
    [SerializeField] private TMP_FontAsset Font;
    [SerializeField] private int FontSize;
    [SerializeField] private Color DefaultFontColor = Color.white;
    [SerializeField] private float AutoPlayWait = -1; // wont autoplay <0
    [SerializeField] private KeyCode DefaultAdvanceKey = KeyCode.Space; // set none to disable.
    [SerializeField] private bool RequeueOnDisable = false; // puts dequeued dialog back in. (repeatable dialog)
    [SerializeField] private bool CanReplay = true;
    private bool TextCompletedPlaying = false;
    private bool HasPlayed = false;

    private Queue<(string text, KeyCode key)> dialogueQueue = new Queue<(string, KeyCode)>();
    private List<TextEffect> activeEffects = new List<TextEffect>();

    // dependencies
    private Coroutine dialogueRoutine;
    private KeyCode currentAdvanceKey;
    private bool advanceRequested = false; // mostly to prevent corutine from stopping which means animations stop.

    // Fast-forward key & state
    [SerializeField] private KeyCode FastForwardKey = KeyCode.Return;
    private bool fastForwardHold = false;   // true while FF key held
    private bool fastForwardPulse = false;  // one-shot FF (press)

    // ignore common tmp tags
    private static readonly HashSet<string> TMPAllowedTags = new HashSet<string>
    {
        "b","/b",
        "i","/i",
        "u","/u",
        "s","/s",
        "size","/size",
        "font","/font",
        "align","/align",
        "line-height","/line-height",
        "voffset","/voffset",
        "cspace","/cspace",
        "mspace","/mspace",
        "indent","/indent",
        "margin","/margin",
        "style","/style",
        "nobr","/nobr",
        "br",
        "sup","/sup",
        "sub","/sub",
        "allcaps","/allcaps",
        "smallcaps","/smallcaps",
        "uppercase","/uppercase",
        "lowercase","/lowercase",
        "link","/link",
        "sprite"
    }; // color will not a part of this set, as there is a custom color tag.

    // is this a tmp tag?
    private bool IsTMPTag(string tagContent)
    {
        string tagName = tagContent.Split(new char[] { '=', ' ' }, 2)[0].ToLower();
        return TMPAllowedTags.Contains(tagName);
    }

    public int GetDialogueQueueSize()
    {
        return dialogueQueue.Count;
    }

    public bool FinishedDisplayingText => TextCompletedPlaying;

    void Start()
    {
        if (dialogueText == null)
        {
            dialogueText = GetComponent<TextMeshProUGUI>();
            if (InitialDialogueText!=null && InitialDialogueText != " " && InitialDialogueText != "")
            {
                QueueDialogue(InitialDialogueText);
                PlayNext();
            }
        }
    }
    /// <summary>
    /// Sets autoplay, < 0 disables this, > 0 waits this amount before next.
    /// </summary>
    /// <param name="wait_Time">Time For next dialogue</param>
    public void SetAutoPlay(float wait_Time)
    {
        if (AutoPlayWait == 0) AutoPlayWait = -1;
        AutoPlayWait = wait_Time;
    }
    /// <summary>
    /// Appends a new string to be converted to on screen dialogue.
    /// Needs PlayNext() to work. key defaults to DefaultAdvanceKey.
    ///
    /// Will use DefaultAdvanceKey if KeyCode.None.
    /// Set DefaultAdvanceKey to None if you do not want the user to be able to advance themselves by default.
    /// </summary>
    public void QueueDialogue(string newText, KeyCode key)
    {
        if (key == KeyCode.None)
        {
            key = DefaultAdvanceKey;
        }

        dialogueQueue.Enqueue((newText, key));
    }

    /// <summary>
    /// Appends a new string to be converted to on screen dialogue.
    /// Needs PlayNext() to work. Will use DefaultAdvanceKey as key.
    /// </summary>
    public void QueueDialogue(string newText)
    {
        dialogueQueue.Enqueue((newText, DefaultAdvanceKey));
    }
    /// <summary>
    /// Appends a new string to be converted to on screen dialogue.
    /// Needs PlayNext() to work. key defaults to DefaultAdvanceKey.
    /// 
    /// Will try to convert int key into KeyCode.
    /// Will use DefaultAdvanceKey if fails.
    /// Set DefaultAdvanceKey to None if you do not want the user to be able to advance themselves by default.
    /// </summary>
    public void QueueDialogue(string newText, int key = 0)
    {
        if (key == 0)
        {
            dialogueQueue.Enqueue((newText, DefaultAdvanceKey));
        }
        KeyCode keyc = (KeyCode)key;
        dialogueQueue.Enqueue((newText, keyc));
    }

    public void SetHeadshot(Sprite img)
    {
        CharacterHeadshotTransform.GetComponent<Image>().sprite = img;
    }
    public void ExitDialogue()
    {
        gameObject.SetActive(false);
        dialogueRoutine = null;
    }
    
    void StopDialogueRoutine()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }
    }
    void ClearDialogue() {
        dialogueQueue.Clear();
    }
    /// <summary>
    /// Resets the dialogueQueue to contain text in passed text and key.
    /// Accepts a list of strings/single string as param 1
    /// Accepts a list of keys/single key as param 2
    /// </summary>
    public void SetDialogue(string text, KeyCode key = KeyCode.None)
    {
        StopDialogueRoutine();
        ClearDialogue();
        QueueDialogue(text, key);
    }
    /// <summary>
    /// Resets the dialogueQueue to contain text in passed text and key.
    /// Accepts a list of strings/single string as param 1
    /// Accepts a list of keys/single key as param 2
    /// </summary>
    public void SetDialogue(List<string> listOfText, KeyCode key)
    {
        StopDialogueRoutine();
        ClearDialogue();
        foreach (string text in listOfText)
        {
            QueueDialogue(text, key);
        }
    }
    /// <summary>
    /// Resets the dialogueQueue to contain text in passed text and key.
    /// Accepts a list of strings/single string as param 1
    /// Accepts a list of keys/single key as param 2
    /// </summary>
    public void SetDialogue(List<string> listOfText, List<KeyCode> keys)
    {
        StopDialogueRoutine();
        ClearDialogue();
        for (int i = 0; i< listOfText.Count; i++)
        {
            QueueDialogue(listOfText[i], keys[i]);
        }
    }
    void BeginSpeaking()
    {
        dialogueText.text = "";

        // apply chosen style.
        if (Font != null)
            dialogueText.font = Font;

        if (FontSize > 0)
            dialogueText.fontSize = FontSize;

        dialogueText.faceColor = DefaultFontColor;


        if (dialogueQueue.Count == 0) return;
        var (txt, key) = dialogueQueue.Dequeue();
        currentAdvanceKey = key;

        ParseTags(txt, out string clean, out activeEffects);

        advanceRequested = false;
        dialogueRoutine = StartCoroutine(RunDialogue(clean));
    }
    public void DebugPrint(string cleantxt)
    {
        // DEBUG: dump parsed text + effects
        string debuginfo = $"CleanText: \"{cleantxt}\"\n";
        foreach (var e in activeEffects)
        {
            debuginfo += $"Effect: {e.GetType().Name} start={e.startIndex} end={e.endIndex}\n";
            if (e is TextEffectTypewriter typewriter)
            {
                debuginfo += $"  Pauses: {typewriter.pauseIndices.Count}\n";
                for (int i = 0; i < typewriter.pauseIndices.Count; i++)
                {
                    debuginfo += $"    Pause at {typewriter.pauseIndices[i]} dur={typewriter.pauseDurations[i]}\n";
                }
            }
        }
        Debug.Log(debuginfo);
    }
    IEnumerator RunDialogue(string text)
    {
        TextCompletedPlaying = false;
        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        TMP_MeshInfo[] originalInfo = dialogueText.textInfo.CopyMeshInfoVertexData();



        var typewriters = new List<TextEffectTypewriter>();
        foreach (var e in activeEffects) if (e is TextEffectTypewriter tt) typewriters.Add(tt);
        typewriters.Sort((a, b) => a.startIndex.CompareTo(b.startIndex));
        foreach (var tt in typewriters) { tt.timer = 0f; tt.revealed = 0; }

        bool[] visibleMask = null;
        bool dialogueComplete = false;

        float t = 0f;

        if (typewriters.Count == 0) TextCompletedPlaying = true;

        while (!dialogueComplete)
        {
            float delta = Time.deltaTime;

            if (fastForwardHold)
            {
                foreach (var tt in typewriters) tt.CompleteInstantly();
            }
            else if (fastForwardPulse)
            {
                TextEffectTypewriter earliest = null;
                foreach (var tt in typewriters)
                {
                    if (tt.revealed < (tt.endIndex - tt.startIndex + 1))
                    {
                        earliest = tt;
                        break;
                    }
                }
                if (earliest != null) earliest.CompleteInstantly();
                fastForwardPulse = false;
            }
            else
            {
                foreach (var tt in typewriters)
                {
                    if (tt.revealed < (tt.endIndex - tt.startIndex + 1))
                    {
                        tt.UpdateProgress(delta);
                    }
                }
            }
            

            dialogueText.ForceMeshUpdate();
            var info = dialogueText.textInfo;
            int totalChars = info.characterCount;

            if (originalInfo == null || originalInfo.Length != info.meshInfo.Length)
                originalInfo = dialogueText.textInfo.CopyMeshInfoVertexData();

            if (visibleMask == null || visibleMask.Length < totalChars) visibleMask = new bool[totalChars];

            for (int i = 0; i < totalChars; i++)
            {
                bool isRevealed = true;
                foreach (var tt in typewriters)
                {
                    if (i >= tt.startIndex && i <= tt.endIndex)
                    {
                        int charsToReveal = tt.revealed;
                        int lastRevealedIndex = tt.startIndex + charsToReveal - 1;

                        if (charsToReveal <= 0 || i > lastRevealedIndex)
                        {
                            isRevealed = false;
                        }
                        break;
                    }
                }

                visibleMask[i] = isRevealed;
            }


            dialogueText.maxVisibleCharacters = info.characterCount;
            dialogueText.ForceMeshUpdate();
            info = dialogueText.textInfo;

            if (info.characterCount == 0)
            {
                t += delta;
                yield return null;
                continue;
            }

            Color ctmp = DefaultFontColor;
            Color32 defaultCol32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.r * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.g * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.b * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.a * 255f), 0, 255)
            );

            for (int mi = 0; mi < info.meshInfo.Length; mi++)
            {
                var cols = info.meshInfo[mi].colors32;
                var origCols = (originalInfo.Length > mi) ? originalInfo[mi].colors32 : null;
                if (cols == null || cols.Length == 0) continue;

                if (origCols != null && origCols.Length == cols.Length)
                {
                    for (int k = 0; k < cols.Length; k++) cols[k] = origCols[k];
                }
                else
                {
                    for (int k = 0; k < cols.Length; k++) cols[k] = defaultCol32;
                }
            }

            for (int ci = 0; ci < info.characterCount; ci++)
            {
                int mat = info.characterInfo[ci].materialReferenceIndex;
                int vIdx = info.characterInfo[ci].vertexIndex;
                var cols = info.meshInfo[mat].colors32;
                if (cols == null) continue;

                if (visibleMask[ci])
                {
                    for (int q = 0; q < 4; q++) cols[vIdx + q] = defaultCol32;
                }
                else
                {
                    Color32 trans = defaultCol32;
                    trans.a = 0;
                    for (int q = 0; q < 4; q++) cols[vIdx + q] = trans;
                }
            }

            for (int i = 0; i < activeEffects.Count; i++)
            {
                var e = activeEffects[i];
                e.startIndex = Mathf.Clamp(e.startIndex, 0, info.characterCount - 1);
                e.endIndex = Mathf.Clamp(e.endIndex, 0, info.characterCount - 1);
                e.Apply(dialogueText, info, originalInfo, t);
            }

            for (int i = 0; i < info.meshInfo.Length; i++)
            {
                var meshInfo = info.meshInfo[i];
                var mesh = meshInfo.mesh;
                mesh.vertices = meshInfo.vertices;
                mesh.colors32 = meshInfo.colors32;
                dialogueText.UpdateGeometry(mesh, i);
            }

            bool allVisible = true;
            if (visibleMask == null || visibleMask.Length < totalChars) allVisible = false;
            else
            {
                for (int i = 0; i < totalChars; i++)
                {
                    if (!visibleMask[i]) { allVisible = false; break; }
                }
            }

            if (!allVisible)
            {
                t += delta;
                yield return null;
                continue;
            }

            if(allVisible) TextCompletedPlaying = true;

            bool autoplayEnabled = AutoPlayWait > 0f;
            float autoplayEnd = autoplayEnabled ? Time.time + AutoPlayWait : 0f;
            bool shouldAdvance = false;

            while (true)
            {
                if (advanceRequested)
                {
                    advanceRequested = false;
                    shouldAdvance = true;
                    break;
                }

                if (autoplayEnabled && Time.time >= autoplayEnd)
                {
                    shouldAdvance = true;
                    break;
                }

                t += Time.deltaTime;
                dialogueText.ForceMeshUpdate();
                info = dialogueText.textInfo;

                for (int i = 0; i < activeEffects.Count; i++)
                {
                    var e = activeEffects[i];
                    e.startIndex = Mathf.Clamp(e.startIndex, 0, info.characterCount - 1);
                    e.endIndex = Mathf.Clamp(e.endIndex, 0, info.characterCount - 1);
                    e.Apply(dialogueText, info, originalInfo, t);
                }

                for (int i = 0; i < info.meshInfo.Length; i++)
                {
                    var meshInfo = info.meshInfo[i];
                    var mesh = meshInfo.mesh;
                    mesh.vertices = meshInfo.vertices;
                    mesh.colors32 = meshInfo.colors32;
                    dialogueText.UpdateGeometry(mesh, i);
                }

                yield return null;
            }

            fastForwardPulse = false;
            fastForwardHold = false;
            dialogueRoutine = null;

            if (shouldAdvance && dialogueQueue.Count > 0)
            {
                yield return new WaitForEndOfFrame(); // Prevent input carry-over
                PlayNext();
            }
        }
    }
    string[] SplitTopLevelArgs(string s)
    {
        if (string.IsNullOrEmpty(s)) return new string[0];
        var parts = new List<string>();
        var sb = new System.Text.StringBuilder();
        int depth = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '(')
            {
                depth++;
                sb.Append(c);
            }
            else if (c == ')')
            {
                depth = Mathf.Max(0, depth - 1);
                sb.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                parts.Add(sb.ToString().Trim());
                sb.Length = 0;
            }
            else
            {
                sb.Append(c);
            }
        }
        if (sb.Length > 0) parts.Add(sb.ToString().Trim());
        return parts.ToArray();
    }
    public bool GetHasPlayedState => HasPlayed;
    /// <summary>
    /// Plays the next text in queue.
    /// </summary>
    public void PlayNext(bool disableOnComplete = false)
    {
        Debug.Log("Playing Next dialogue");
        if (CanReplay && !HasPlayed)
        {
            HasPlayed = true;
        }
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        fastForwardPulse = false;
        fastForwardHold = false;
        advanceRequested = false;

        if (dialogueQueue.Count > 0)
        {
            BeginSpeaking();
        }
        else if (disableOnComplete)
        {
            gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Clears queue and Plays this specific text. Will search for <break> tags. text after a <break> will be queued.
    /// </summary>
    /// <param name="txt"></param>
    public void Play(string txt = "", float autoplay = -1, bool disableOnComplete = false)
    {
        Debug.Log("Playing text");
        ClearDialogue();
        SetAutoPlay(autoplay);
        string[] parts = txt.Split(new string[] { "<break>" }, 0);

        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
                QueueDialogue(trimmed);
        }
        PlayNext(disableOnComplete);
    }
    /// <summary>
    /// Skips the current typewriter effect but stays on the current dialogue.
    /// </summary>
    public void SkipToEndOfCurrentDialogue()
    {
        if (dialogueRoutine != null)
        {
            foreach (var effect in activeEffects)
            {
                if (effect is TextEffectTypewriter typewriter)
                {
                    typewriter.CompleteInstantly();
                }
            }
            
            dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
            dialogueText.ForceMeshUpdate();
            
        }
    }
    private float pressTime;
    void Update()
    {
        if (Input.GetKeyDown(FastForwardKey))
        {
            pressTime = Time.time;
            if (dialogueRoutine != null)
            {
                fastForwardPulse = true;
                fastForwardHold = true;
                SkipToEndOfCurrentDialogue();
            }
        }
        if (Input.GetKey(FastForwardKey))
        {
            if ((Time.time - pressTime) > 0.15)
                fastForwardHold = true;
        }
        else
        {
            fastForwardHold = false;
        }

        if (Input.GetKeyDown(currentAdvanceKey))
        {
            if (dialogueRoutine != null)
            {
                advanceRequested = true;
            }
            else
            {
                fastForwardHold = false;
                fastForwardPulse = false;
                PlayNext();
            }
        }
        if (Input.GetKeyUp(currentAdvanceKey))
        {
            advanceRequested = false;
        }


    }
    private bool IsCharacterRevealed(int charIndex, List<TextEffectTypewriter> typewriters)
    {
        if (typewriters == null || typewriters.Count == 0) return true;
        
        foreach (var tt in typewriters)
        {
            if (charIndex >= tt.startIndex && charIndex <= tt.endIndex)
            {
                return tt.revealed > 0 && charIndex <= tt.startIndex + tt.revealed - 1;
            }
        }
        
        return true;
    }
    void OnDisable()
    {
        StopDialogueRoutine();
        ClearDialogue();
    }
    /// <summary>
    /// Disables this below functionality.
    /// The default key is used whenever no key is passed to QueueDialogue() or SetDialogue().
    /// </summary>
    public void DisableDefaultKey()
    {
        DefaultAdvanceKey = KeyCode.None;
    }
    /// <summary>
    /// The default key is used whenever no key is passed to QueueDialogue() or SetDialogue().
    /// Setting default key to None will disable skipping for future dialog.
    /// </summary>
    public void SetDefaultKey(KeyCode key)
    {
        DefaultAdvanceKey = key;
    }

    void ParseTags(string input, out string cleanText, out List<TextEffect> effects)
    {
        effects = new List<TextEffect>();
        List<TextEffectPause> pauses = new List<TextEffectPause>();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        Stack<(string tagName, int startIndex, string paramStr)> openTags = new Stack<(string, int, string)>();

        bool insideTag = false;
        string tagBuffer = "";

        int visIndex = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '<')
            {
                insideTag = true;
                tagBuffer = "";
                continue;
            }
            if (c == '>' && insideTag)
            {
                insideTag = false;
                bool closing = tagBuffer.StartsWith("/");
                string tagContent = closing ? tagBuffer.Substring(1) : tagBuffer;

                string[] parts = tagContent.Split('=');
                string tagName = parts[0].Trim().ToLower();
                string paramStr = parts.Length > 1 ? parts[1] : "";

                if (IsTMPTag(tagContent))
                {
                    // Let TMP handle it & just append it back to text.
                    sb.Append("<" + tagContent + ">");
                    continue;
                }
                if (tagName == "pause")
                {
                    float d = ParseFloat(paramStr, 1f);
                    pauses.Add(new TextEffectPause(d) { startIndex = visIndex });
                }
                else if (!closing)
                {
                    openTags.Push((tagName, visIndex, paramStr));
                }
                else
                {
                    if (openTags.Count > 0)
                    {
                        var (openName, startIdx, pStr) = openTags.Pop();

                        if (openName != tagName)
                        {
                            // Debug.LogWarning($"Mismatched tag: opened <{openName}> but closed </{tagName}>");
                            // Option 1: ignore the closing tag, op 2 is to process them somehow. but nah.
                            continue;
                        }
                        else
                        {
                            var eff = MakeEffect(openName, pStr);
                            if (eff != null)
                            {
                                int endIdx = visIndex - 1;
                                if (endIdx >= startIdx)
                                {
                                    eff.startIndex = startIdx;
                                    eff.endIndex = endIdx;
                                    if (eff is TextEffectTypewriter typewriter)
                                    {
                                        foreach (var pause in pauses)
                                        {
                                            if (pause.startIndex >= startIdx && pause.startIndex <= endIdx)
                                            {
                                                typewriter.pauseIndices.Add(pause.startIndex - startIdx);
                                                typewriter.pauseDurations.Add(pause.Duration);
                                            }
                                        }
                                    }
                                    effects.Add(eff);
                                }
                            }
                        }
                    }
                }
                continue;
            }

            if (insideTag)
            {
                tagBuffer += c;
            }
            else
            {
                sb.Append(c);
                visIndex++;
            }
        }

        cleanText = sb.ToString();
    }


    TextEffect MakeEffect(string name, string paramStr)
    {
        // defaults if no paramstr passed.
        if (string.IsNullOrEmpty(paramStr))
        {
            switch (name)
            {
                case "typewriter": return new TextEffectTypewriter(30f);
                case "wavy": return new TextEffectWavy(5f, 5f);
                case "shake": return new TextEffectShake(2f, 2f);
                case "color": return new TextEffectColor(Color.white);
                case "colortransition": return new TextEffectColorTransition(Color.white, Color.black, 2f);
                case "magnify": return new TextEffectMagnify(3f, 2f, 5f);
                case "quake": return new TextEffectQuake(5f);
                case "drop": return new TextEffectDrop(2f, 10f, -90f);
                case "rain": return new TextEffectRain(2f, 10f, -90f);
                case "construct": return new TextEffectConstruct(50f, 1f);
                case "slam": return new TextEffectSlam(3f, 2);
                case "explode": return new TextEffectExplode(50f, 9.8f);
                case "collapse": return new TextEffectCollapse(30f, 20f);
                case "colorgradient": return new TextEffectColorGradient(Color.white, Color.black, 0, 0f);
                default: return null;
            }
        }
        switch (name)
        {
            case "pause":
                return new TextEffectPause(ParseFloat(paramStr, 0.5f));

            case "typewriter":
                return new TextEffectTypewriter(ParseFloat(paramStr, 10f));

            case "wavy":
                {
                    string[] wa = paramStr.Split(',');
                    float ws = wa.Length > 0 ? ParseFloat(wa[0], 5f) : 5f;
                    float amp = wa.Length > 1 ? ParseFloat(wa[1], 5f) : 5f;
                    return new TextEffectWavy(ws, amp);
                }

            case "shake":
                {
                    string[] sa = paramStr.Split(',');
                    float sx = sa.Length > 0 ? ParseFloat(sa[0], 2f) : 2f;
                    float sy = sa.Length > 1 ? ParseFloat(sa[1], 2f) : 2f;
                    return new TextEffectShake(sx, sy);
                }

            case "color":
                return new TextEffectColor(ParseColor(paramStr));

            case "colortransition":
                {
                    // var parts = paramStr.Split(',');
                    var parts = SplitTopLevelArgs(paramStr);
                    if (parts.Length >= 2)
                    {
                        Color c1 = ParseColor(parts[0]);
                        Color c2 = ParseColor(parts[1]);
                        float dur = parts.Length > 2 ? ParseFloat(parts[2], 2f) : 2f;
                        return new TextEffectColorTransition(c1, c2, dur);
                    }
                    break;
                }

            case "magnify":
                {
                    string[] ma = paramStr.Split(',');
                    float ms = ma.Length > 0 ? ParseFloat(ma[0], 3f) : 3f;
                    float maxSize = ma.Length > 1 ? ParseFloat(ma[1], 2f) : 2f;
                    float len = ma.Length > 2 ? ParseFloat(ma[2], 5f) : 5f;
                    return new TextEffectMagnify(ms, maxSize, len);
                }
            case "scale":
                {
                    string[] ma = paramStr.Split(',');
                    float ms = ma.Length > 0 ? ParseFloat(ma[0], 3f) : 3f;
                    float maxSize = ma.Length > 1 ? ParseFloat(ma[1], 2f) : 2f;
                    return new TextEffectMagScale(ms, maxSize);
                }
            case "bloat":
                {
                    string[] ma = paramStr.Split(',');
                    float ms = ma.Length > 0 ? ParseFloat(ma[0], 3f) : 3f;
                    float maxSize = ma.Length > 1 ? ParseFloat(ma[1], 2f) : 2f;
                    float len = ma.Length > 2 ? ParseFloat(ma[2], 5f) : 5f;
                    return new TextEffectBloat(ms, maxSize, len);
                }
            case "colorgradient":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    if (parts.Length >= 2)
                    {
                        Color c1 = ParseColor(parts[0]);
                        Color c2 = ParseColor(parts[1]);
                        int center = parts.Length > 2 ? (int)ParseFloat(parts[2], 0f) : 0;
                        float angle = parts.Length > 3 ? ParseFloat(parts[3], 0f) : 0f;
                        return new TextEffectColorGradient(c1, c2, center, angle);
                    }
                    break;
                }
            case "quake":
                {
                    return new TextEffectQuake(ParseFloat(paramStr, 5f));
                }
            case "drop":
                {
                    var parts = paramStr.Split(',');
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 2f) : 2f;
                    float bounce = parts.Length > 1 ? ParseFloat(parts[1], 10f) : 10f;
                    float angle = parts.Length > 2 ? ParseFloat(parts[2], -90f) : -90f;
                    return new TextEffectDrop(speed, bounce, angle);
                }
            case "rain":
                {
                    var parts = paramStr.Split(',');
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 2f) : 2f;
                    float bounce = parts.Length > 1 ? ParseFloat(parts[1], 10f) : 10f;
                    float angle = parts.Length > 2 ? ParseFloat(parts[2], -90f) : -90f;
                    return new TextEffectRain(speed, bounce, angle);
                }
            case "construct":
                {
                    var parts = paramStr.Split(',');
                    float length = parts.Length > 0 ? ParseFloat(parts[0], 50f) : 50f;
                    float speed = parts.Length > 1 ? ParseFloat(parts[1], 1f) : 1f;
                    return new TextEffectConstruct(length, speed);
                }
            case "slam":
                {
                    var parts = paramStr.Split(',');
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 3f) : 3f;
                    int bounces = parts.Length > 1 ? (int)ParseFloat(parts[1], 2f) : 2;
                    return new TextEffectSlam(speed, bounces);
                }
            case "explode":
                {
                    var parts = paramStr.Split(',');
                    float force = parts.Length > 0 ? ParseFloat(parts[0], 50f) : 50f;
                    float gravity = parts.Length > 1 ? ParseFloat(parts[1], 9.8f) : 9.8f;
                    return new TextEffectExplode(force, gravity);
                }
            case "collapse":
                {
                    var parts = paramStr.Split(',');
                    float rotation = parts.Length > 0 ? ParseFloat(parts[0], 30f) : 30f;
                    float drop = parts.Length > 1 ? ParseFloat(parts[1], 20f) : 20f;
                    return new TextEffectCollapse(rotation, drop);
                }

        }
        return null;
    }
    private Color ParseColor(string s)
    {
        s = s.Trim();
        if (s.StartsWith("(") && s.EndsWith(")"))
        {
            var nums = s.Trim('(', ')').Split(',');
            if (nums.Length >= 3)
            {
                float r = ParseFloat(nums[0], 255f) / 255f;
                float g = ParseFloat(nums[1], 255f) / 255f;
                float b = ParseFloat(nums[2], 255f) / 255f;
                return new Color(r, g, b);
            }
        }
        if (ColorUtility.TryParseHtmlString(s, out var htmlCol))
            return htmlCol;
        return DefaultFontColor;
    }
    float ParseFloat(string str, float def = 0f)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
        return def;
            
        float v;
        if (float.TryParse(str.Trim(), out v)) 
            return v;
            
        return def;
    }
}

// effects herree

public abstract class TextEffect
{
    public int startIndex;
    public int endIndex; // inclusive.
    /// <summary>
    /// Applies the effect
    /// </summary>
    /// <param name="tmp">UI_Component</param>
    /// <param name="info">UI_Info</param>
    /// <param name="time">Apply Time</param>
    public abstract void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] originalVerts, float time);
}


public class TextEffectTypewriter : TextEffect
{
    public float speed;
    public float timer = 0f;
    public int revealed = 0;
    public List<int> pauseIndices = new List<int>();
    public List<float> pauseDurations = new List<float>();
    private float pauseTimeRemaining = 0f;
    private int currentPauseIndex = -1;
    public TextEffectTypewriter(float s) { speed = Mathf.Max(0.0001f, s); timer = 0f; revealed = 0; }
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float t) { }
    // helper to update internal progress
    public void UpdateProgress(float deltaSeconds)
    {
        if (revealed >= (endIndex - startIndex + 1)) return;
        
        if (pauseTimeRemaining > 0f)
        {
            pauseTimeRemaining -= deltaSeconds;
            if (pauseTimeRemaining < 0f) pauseTimeRemaining = 0f;
            return;
        }
        
        if (currentPauseIndex < pauseIndices.Count - 1 && revealed == pauseIndices[currentPauseIndex + 1])
        {
            currentPauseIndex++;
            pauseTimeRemaining = pauseDurations[currentPauseIndex];
            return;
        }
        if (revealed >= (endIndex - startIndex + 1)) return;
        timer += deltaSeconds;
        int target = Mathf.FloorToInt(timer * speed);
        target = Mathf.Clamp(target, 0, endIndex - startIndex + 1);
        revealed = target;
    }
    public void CompleteInstantly()
    {
        revealed = endIndex - startIndex + 1;
        timer = (float)revealed / speed;
        pauseTimeRemaining = 0f;
    }
}

public class TextEffectWavy : TextEffect
{
    float waveSpeed, amplitude;
    public TextEffectWavy(float speed, float amp) { waveSpeed = speed; amplitude = amp; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            // dont process alpha-0 chars.
            int mat = info.characterInfo[i].materialReferenceIndex;
            int vIndex = info.characterInfo[i].vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;
            if (cols[vIndex].a == 0) continue;
            
            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            Vector3 offset = new Vector3(0, Mathf.Sin(time * waveSpeed + i) * amplitude, 0);
            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = orig[info.characterInfo[i].materialReferenceIndex].vertices[vIndex + j] + offset;
        }
    }
}


public class TextEffectShake : TextEffect
{
    float strengthX, strengthY;
    public TextEffectShake(float sx, float sy) { strengthX = sx; strengthY = sy; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            // skip unrevealed char by typewriter
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            
            if (cols == null || cols.Length <= vIndex || cols[vIndex].a == 0) continue;
            // end
            

            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[mat].vertices;
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(i, time * 3f) - 0.5f) * strengthX,
                (Mathf.PerlinNoise(i + 100, time * 3f) - 0.5f) * strengthY,
                0
            );
            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = orig[mat].vertices[vIndex + j] + offset;
        }
    }
}


public class TextEffectColor : TextEffect
{
    Color color;
    public TextEffectColor(Color c) { color = c; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            int mat = info.characterInfo[i].materialReferenceIndex;
            int vIndex = info.characterInfo[i].vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;
            if (cols[vIndex].a == 0) continue;

            if (!info.characterInfo[i].isVisible) continue;
            for (int j = 0; j < 4; j++)
                cols[vIndex + j] = color;
        }
    }
}

/// <summary>
/// makes text color change gradually
/// </summary>
public class TextEffectColorTransition : TextEffect
{
    Color a, b;
    float duration;
    float timeOffset;
    
    public TextEffectColorTransition(Color c1, Color c2, float dur) 
    { 
        a = c1; 
        b = c2; 
        duration = Mathf.Max(0.1f, dur);
        timeOffset = Random.Range(0f, duration); // Random offset for variety
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        float t = (Mathf.Sin((time + timeOffset) * (2 * Mathf.PI / duration)) + 1f) * 0.5f;
        Color c = Color.Lerp(a, b, t);
        
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;
            if (cols[vIndex].a == 0) continue;

            for (int j = 0; j < 4; j++)
                cols[vIndex + j] = c;
        }
    }
}
/// <summary>
/// Static gradient of colors.
/// </summary>
public class TextEffectColorGradient : TextEffect
{
    Color a, b;
    int center;
    float angle;
    
    public TextEffectColorGradient(Color c1, Color c2, int centerIndex = 0, float ang = 0f)
    {
        a = c1;
        b = c2;
        center = centerIndex;
        angle = ang * Mathf.Deg2Rad; // Convert to radians
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (info.characterCount == 0) return;
        
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        
        float minDist = float.MaxValue;
        float maxDist = float.MinValue;
        List<float> distances = new List<float>();
        
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex || cols[vIndex].a == 0) continue;
          
            Vector2 charPos = new Vector2(
                (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f,
                (charInfo.bottomLeft.y + charInfo.topRight.y) * 0.5f
            );
            
            Vector2 centerPos = Vector2.zero;
            if (center >= 0 && center < info.characterCount)
            {
                var centerChar = info.characterInfo[center];
                centerPos = new Vector2(
                    (centerChar.bottomLeft.x + centerChar.topRight.x) * 0.5f,
                    (centerChar.bottomLeft.y + centerChar.topRight.y) * 0.5f
                );
            }
            
            float dist = Vector2.Dot(charPos - centerPos, direction);
            distances.Add(dist);
            minDist = Mathf.Min(minDist, dist);
            maxDist = Mathf.Max(maxDist, dist);
        }
        
        if (Mathf.Approximately(minDist, maxDist)) return;
        
        int distIndex = 0;
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            
            float t = Mathf.InverseLerp(minDist, maxDist, distances[distIndex++]);
            Color c = Color.Lerp(a, b, t);
            
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;

            for (int j = 0; j < 4; j++)
                cols[vIndex + j] = c;
        }
    }
}
public class TextEffectQuake : TextEffect
{
    float intensity;
    Vector3[] originalPositions;
    bool initialized = false;

    public TextEffectQuake(float intensity = 5f)
    {
        this.intensity = intensity;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            originalPositions = new Vector3[info.characterCount * 4];
            for (int i = 0; i < info.characterCount; i++)
            {
                if (i >= orig.Length) continue;
                for (int j = 0; j < 4; j++)
                {
                    originalPositions[i * 4 + j] = orig[info.characterInfo[i].materialReferenceIndex].vertices[info.characterInfo[i].vertexIndex + j];
                }
            }
        }

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(i * 0.1f, time * 10f) - 0.5f,
                Mathf.PerlinNoise(i * 0.1f + 100f, time * 10f) - 0.5f,
                0
            ) * intensity * 2f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = originalPositions[i * 4 + j];
                verts[vIndex + j] = origPos + offset;
            }
        }
    }
}
public class TextEffectDrop : TextEffect
{
    float speed;
    float bounceAmount;
    float angle;
    float[] dropTimings;
    bool initialized = false;

    public TextEffectDrop(float speed = 2f, float bounceAmount = 10f, float angle = -90f)
    {
        this.speed = speed;
        this.bounceAmount = bounceAmount;
        this.angle = angle * Mathf.Deg2Rad;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            dropTimings = new float[endIndex - startIndex + 1];
            // All characters drop together
            for (int i = 0; i < dropTimings.Length; i++)
            {
                dropTimings[i] = time;
            }
        }

        Vector2 dropDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            float elapsed = time - dropTimings[i - startIndex];
            float dropProgress = Mathf.Clamp01(elapsed * speed);

            // Bouncing effect
            float bounce = Mathf.Sin(dropProgress * Mathf.PI * 4f) * (1f - dropProgress) * bounceAmount;
            Vector2 offset = dropDirection * (dropProgress * 100f + bounce);

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = origPos + (Vector3)offset;
            }
        }
    }
}
public class TextEffectRain : TextEffect
{
    float speed;
    float bounceAmount;
    float angle;
    float[] dropTimings;
    bool initialized = false;

    public TextEffectRain(float speed = 2f, float bounceAmount = 10f, float angle = -90f)
    {
        this.speed = speed;
        this.bounceAmount = bounceAmount;
        this.angle = angle * Mathf.Deg2Rad;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            dropTimings = new float[endIndex - startIndex + 1];
            // Random drop timings for each character
            for (int i = 0; i < dropTimings.Length; i++)
            {
                dropTimings[i] = time + Random.Range(0f, 2f); // Staggered start times
            }
        }

        Vector2 dropDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            // skip unrevealed char by typewriter
            
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            
            if (cols == null || cols.Length <= vIndex || cols[vIndex].a == 0) continue;
            // end
            var verts = info.meshInfo[mat].vertices;

            float elapsed = Mathf.Max(0f, time - dropTimings[i - startIndex]);
            float dropProgress = Mathf.Clamp01(elapsed * speed);

            float bounce = Mathf.Sin(dropProgress * Mathf.PI * 4f) * (1f - dropProgress) * bounceAmount;
            Vector2 offset = dropDirection * (dropProgress * 100f + bounce);

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = origPos + (Vector3)offset;
            }
        }
    }
}
public class TextEffectConstruct : TextEffect
{
    float length;
    float speed;
    float[] constructTimings;
    bool initialized = false;

    public TextEffectConstruct(float length = 50f, float speed = 1f)
    {
        this.length = length;
        this.speed = speed;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            constructTimings = new float[endIndex - startIndex + 1];
            int centerIndex = startIndex + (endIndex - startIndex) / 2;

            for (int i = startIndex; i <= endIndex; i++)
            {
                float distanceFromCenter = Mathf.Abs(i - centerIndex);
                constructTimings[i - startIndex] = time + (distanceFromCenter * 0.1f);
            }
        }

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            float elapsed = Mathf.Max(0f, time - constructTimings[i - startIndex]);
            float constructProgress = Mathf.Clamp01(elapsed * speed);

            Vector3 center = (orig[mat].vertices[vIndex] + orig[mat].vertices[vIndex + 2]) * 0.5f;
            Vector3 scatterDirection = (orig[mat].vertices[vIndex] - center).normalized;
            Vector3 startPos = center + scatterDirection * length;

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                Vector3 targetPos = origPos;
                Vector3 currentStartPos = startPos + (origPos - center);
                verts[vIndex + j] = Vector3.Lerp(currentStartPos, targetPos, constructProgress);
            }
        }
    }
}
public class TextEffectSlam : TextEffect
{
    float speed;
    int bounceCount;
    float[] slamTimings;
    bool initialized = false;

    public TextEffectSlam(float speed = 3f, int bounceCount = 2)
    {
        this.speed = speed;
        this.bounceCount = bounceCount;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            slamTimings = new float[endIndex - startIndex + 1];
            for (int i = 0; i < slamTimings.Length; i++)
            {
                slamTimings[i] = time;
            }
        }

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            float elapsed = time - slamTimings[i - startIndex];
            float slamProgress = Mathf.Clamp01(elapsed * speed);

            float scale = 1f;
            if (slamProgress < 0.3f)
            {
                scale = Mathf.Lerp(3f, 1f, slamProgress / 0.3f);
            }
            else
            {
                float bounceProgress = (slamProgress - 0.3f) / 0.7f;
                scale = 1f + Mathf.Sin(bounceProgress * Mathf.PI * bounceCount) * 0.3f * (1f - bounceProgress);
            }

            Vector3 center = (orig[mat].vertices[vIndex] + orig[mat].vertices[vIndex + 2]) * 0.5f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = center + (origPos - center) * scale;
            }
        }
    }
}
public class TextEffectExplode : TextEffect
{
    float explosionForce;
    float gravity;
    float delay = 3; //todo add as param
    Vector2[] velocities;
    bool initialized = false;

    public TextEffectExplode(float explosionForce = 50f, float gravity = 9.8f)
    {
        this.explosionForce = explosionForce;
        this.gravity = gravity;
    }
    // todo delay explosion.
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            velocities = new Vector2[endIndex - startIndex + 1];
            Vector2 center = Vector2.zero;
            int count = 0;

            for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
            {
                var charInfo = info.characterInfo[i];
                Vector2 charCenter = new Vector2(
                    (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f,
                    (charInfo.bottomLeft.y + charInfo.topRight.y) * 0.5f
                );
                center += charCenter;
                count++;
            }
            if (count > 0) center /= count;

            for (int i = 0; i < velocities.Length; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                velocities[i] = direction * explosionForce * Random.Range(0.5f, 1.5f);
            }
        }

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            // skip unrevealed char by typewriter
            if (!charInfo.isVisible) continue;
            
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            
            if (cols == null || cols.Length <= vIndex || cols[vIndex].a == 0) continue;
            // end
            var verts = info.meshInfo[mat].vertices;

            float elapsed = time;
            velocities[i - startIndex].y -= gravity * elapsed;

            Vector2 displacement = velocities[i - startIndex] * elapsed;

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = origPos + (Vector3)displacement;
            }
        }
    }
}
public class TextEffectCollapse : TextEffect
{
    float rotationIntensity;
    float dropDistance;
    float[] rotations;
    bool initialized = false;
    
    public TextEffectCollapse(float rotationIntensity = 30f, float dropDistance = 20f)
    {
        this.rotationIntensity = rotationIntensity;
        this.dropDistance = dropDistance;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            rotations = new float[endIndex - startIndex + 1];
            for (int i = 0; i < rotations.Length; i++)
            {
                rotations[i] = Random.Range(-rotationIntensity, rotationIntensity);
            }
        }
        
        float collapseProgress = Mathf.Clamp01(time * 2f);
        
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            
            // Apply rotation and drop
            float rotation = rotations[i - startIndex] * collapseProgress;
            float drop = dropDistance * collapseProgress;
            
            Vector3 center = (orig[mat].vertices[vIndex] + orig[mat].vertices[vIndex + 2]) * 0.5f;
            Quaternion rot = Quaternion.Euler(0, 0, rotation);
            
            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                Vector3 rotatedPos = center + rot * (origPos - center);
                Vector3 finalPos = rotatedPos + Vector3.down * drop;
                verts[vIndex + j] = finalPos;
            }
        }
    }
}
public class TextEffectBloat : TextEffect
{
    float magSpeed, maxScale, length;
    public TextEffectBloat(float speed, float scale, float len) { magSpeed = speed; maxScale = scale; length = len; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            int mat = info.characterInfo[i].materialReferenceIndex;
            int vIndex = info.characterInfo[i].vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;
            if (cols[vIndex].a == 0) continue;

            if (!info.characterInfo[i].isVisible) continue;
            float phase = (time * magSpeed + i) % length / length;
            float scale = 1f + Mathf.Sin(phase * Mathf.PI * 2f) * (maxScale - 1f);

            var verts = info.meshInfo[mat].vertices;

            Vector3 center = Vector3.zero;
            for (int j = 0; j < 4; j++)
                center += orig[mat].vertices[vIndex + j];
            center /= 4f;

            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = center + (orig[mat].vertices[vIndex + j] - center) * scale;
        }
    }
}
public class TextEffectMagScale : TextEffect
{
    float magSpeed, maxScale;
    public TextEffectMagScale(float speed, float scale) { magSpeed = speed; maxScale = scale; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;
            for (int j = 0; j < 4; j++)
                center += verts[vIndex + j];
            count += 4;
        }
        center /= Mathf.Max(1, count);

        float scale = 1f + (Mathf.Sin(time * magSpeed) * 0.5f + 0.5f) * (maxScale - 1f);
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            int mat = info.characterInfo[i].materialReferenceIndex;
            int vIndex = info.characterInfo[i].vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            if (!info.characterInfo[i].isVisible) continue;

            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = center + (verts[vIndex + j] - center) * scale;
        }

    }
}


public class TextEffectMagnify : TextEffect
{
    float speed, maxScale, falloff;
    public TextEffectMagnify(float speed, float scale, float len)
    {
        this.speed = speed; maxScale = scale; falloff = len;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        int span = endIndex - startIndex + 1;
        float lensPos = startIndex + (time * speed % span);
        if (lensPos > endIndex + falloff)
        {
            lensPos = startIndex - falloff;
        }
        for (int i = startIndex; i <= endIndex; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            Vector3[] origVerts = orig[mat].vertices;

            float dist = Mathf.Abs(i - lensPos);

            if (dist > falloff)
            {
                for (int j = 0; j < 4; j++)
                    verts[vIndex + j] = origVerts[vIndex + j];
                continue;
            }
            

            float t = 1f - (dist / falloff);
            t = Mathf.SmoothStep(0f, 1f, t);
            float scale = Mathf.Lerp(1f, maxScale, t);
            Vector3 center = (origVerts[vIndex] + origVerts[vIndex + 2]) / 2f;
            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = center + (origVerts[vIndex + j] - center) * scale;
        }
    }
}
public class TextEffectPause : TextEffect
{
    float duration;
    public TextEffectPause(float time) { duration = Mathf.Max(0.01f, time); }
    public float Duration => duration;

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float t) { }
}

