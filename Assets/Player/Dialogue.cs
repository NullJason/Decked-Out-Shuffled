using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary> 
/// Generates text for dialogues. This uses TMP.<br/>
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
/// &lt;Color=(175,175,175)&gt; The Color of this text should be GRAY. &lt;/Color&gt;
/// &lt;ColorTransition=(255,255,255), (0,0,0)&gt; The text here should be Gradient Colored from WHITE to BLACK. &lt;/ColorTransition&gt;
/// &lt;Magnify=3,2,4&gt; The Text here should be magnified with a SPEED of 3, SCALE of 2, LENGTH of 4. &lt;/Magnify&gt;
/// 
/// TMP has default tags:
/// &lt;b&gt; for bold
/// &lt;i&gt; for italics
/// &lt;u&gt; for underline
/// etc...
/// Full tmp tags at https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextSupportedTags.html
/// </code>
/// 
/// </summary>
public class Dialogue : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI dialogueText;

    // If these variables aren't set then nothing will happen to the text style.
    [SerializeField] private TMP_FontAsset Font;
    [SerializeField] private int FontSize;
    [SerializeField] private Color DefaultFontColor = Color.white;
    [SerializeField] private bool DoAutoPlay = false;

    Queue<(string text, KeyCode key)> dialogueQueue = new Queue<(string, KeyCode)>();
    List<TextEffect> activeEffects = new List<TextEffect>();

    // Pausing
    List<TextEffectPause> pauseMarkers = new List<TextEffectPause>();
    KeyCode DefaultAdvanceKey = KeyCode.Space; // set none to disable.

    // dependencies
    Coroutine dialogueRoutine;
    KeyCode currentAdvanceKey;

    // Fast-forward key & state
    [SerializeField] private KeyCode FastForwardKey = KeyCode.Return;
    private bool fastForwardHold = false;   // true while FF key held
    private bool fastForwardPulse = false;  // one-shot FF (press)

    // ignore tmp tags
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



    void Start()
    {
        if (dialogueText == null)
            dialogueText = GetComponent<TextMeshProUGUI>();
    }

    public void EnableAutoPlay()
    {
        DoAutoPlay = true;
    }
    public void DisableAutoPlay()
    {
        DoAutoPlay = false;
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
    public void SetDialogue(string text, KeyCode key)
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

        ParseTags(txt, out string clean, out activeEffects, out pauseMarkers);

        // DEBUG: dump parsed text + effects
        string debuginfo = $"CleanText: \"{clean}\"\n";
        foreach (var e in activeEffects)
        {
            debuginfo += $"Effect: {e.GetType().Name} start={e.startIndex} end={e.endIndex}\n";
        }
        foreach (var p in pauseMarkers)
        {
            debuginfo += $"Pause at {p.startIndex} dur={p.Duration}\n";
        }
        Debug.Log(debuginfo);
        dialogueRoutine = StartCoroutine(RunDialogue(clean));
    }

    IEnumerator RunDialogue(string text)
    {
        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        TMP_MeshInfo[] originalInfo = dialogueText.textInfo.CopyMeshInfoVertexData();

        float t = 0f;
        int pauseIndex = 0;

        // gather all typewriter effects (there may be multiple)
        var typewriters = new List<TextEffectTypewriter>();
        foreach (var e in activeEffects)
            if (e is TextEffectTypewriter tt) typewriters.Add(tt);
        typewriters.Sort((a, b) => a.startIndex.CompareTo(b.startIndex));

        // reset typewriters
        foreach (var tt in typewriters) { tt.timer = 0f; tt.revealed = 0; }

        // DEBUG state trackers (lightweight)
        int lastEarliestStart = -1;
        
        while (true)
        {
            float delta = Time.deltaTime;

            // 1) Fast-forward semantics:
            if (fastForwardHold)
            {
                // reveal everything immediately
                foreach (var tt in typewriters)
                    tt.CompleteInstantly();
            }
            else if (fastForwardPulse)
            {
                // one-shot: complete the earliest unrevealed typewriter span only
                TextEffectTypewriter earliest = null;
                foreach (var tt in typewriters)
                {
                    if (tt.revealed < (tt.endIndex - tt.startIndex + 1))
                    {
                        // if (earliest == null || tt.startIndex < earliest.startIndex)
                        earliest = tt;
                    }
                }
                if (earliest != null)
                    earliest.CompleteInstantly();

                fastForwardPulse = false; // consume the pulse
            }
            else
            {
                // advance ONLY the earliest incomplete typewriter (prevent others from pre-progressing)
                TextEffectTypewriter earliest = null;
                foreach (var tt in typewriters)
                {
                    if (tt.revealed < (tt.endIndex - tt.startIndex + 1))
                    {
                        earliest = tt;
                        break;
                    }
                }
                if (earliest != null) earliest.UpdateProgress(delta);
                // DEBUG when the earliest typewriter span changes
                int curStart = earliest != null ? earliest.startIndex : -1;
                if (curStart != lastEarliestStart)
                {
                    lastEarliestStart = curStart;
                    Debug.Log($"[Dialogue] earliestTypewriterStart changed -> {curStart} (visible will stop at first hidden glyph)");
                }
            }


            // force TMP update so characterCount is accurate and characterInfo populated
            dialogueText.ForceMeshUpdate();
            var info = dialogueText.textInfo;
            int totalChars = info.characterCount;


            // If mesh info length changed (materials came/left), refresh originalInfo
            if (originalInfo == null || originalInfo.Length != info.meshInfo.Length)
                originalInfo = dialogueText.textInfo.CopyMeshInfoVertexData();

            int visibleCount = totalChars;
            for (int ci = 0; ci < totalChars; ci++)
            {
                bool hidden = false;
                foreach (var tt in typewriters)
                {
                    // if ci is within this typewriter span and not yet revealed, then it's hidden
                    if (ci >= tt.startIndex && ci <= tt.endIndex)
                    {
                        int revealedAbsoluteEnd = tt.startIndex + tt.revealed - 1; // last revealed index in that span
                        if (tt.revealed <= 0 || ci > revealedAbsoluteEnd)
                        {
                            hidden = true;
                            break;
                        }
                    }
                }
                if (hidden)
                {
                    visibleCount = ci;
                    break;
                }
            }

            // pause markers
            if (pauseIndex < pauseMarkers.Count && visibleCount >= pauseMarkers[pauseIndex].startIndex)
            {

                Debug.Log($"[Dialogue] Pause triggered at visibleCount={visibleCount}, pause.start={pauseMarkers[pauseIndex].startIndex}, duration={pauseMarkers[pauseIndex].Duration}");

                float duration = pauseMarkers[pauseIndex].Duration;
                // holding fast-forward skips pauses
                if (fastForwardHold) duration = 0f;

                pauseIndex++;
                float pauseEnd = Time.time + duration;
                while (Time.time < pauseEnd)
                {
                    // allow hold to cancel pauses
                    if (fastForwardHold) break;
                    yield return null;
                }
            }

            dialogueText.maxVisibleCharacters = visibleCount;
            dialogueText.ForceMeshUpdate();
            info = dialogueText.textInfo; // refresh info after changing visibility

            if (info.characterCount == 0)
            {
                // finish condition uses TMP count
                if (visibleCount >= info.characterCount) break;
                t += delta;
                yield return null;
                continue;
            }

            // Set every visible glyph's vertex colors to DefaultFontColor unless a color effect overrides it
            Color ctmp = DefaultFontColor;
            Color32 defaultCol32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.r * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.g * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.b * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(ctmp.a * 255f), 0, 255)
            );


            for (int mi = 0; mi < info.meshInfo.Length; mi++)
            {
                var origCols = originalInfo.Length > mi ? originalInfo[mi].colors32 : null;
                var cols = info.meshInfo[mi].colors32;
                if (cols == null || cols.Length == 0) continue;
                if (origCols != null && origCols.Length == cols.Length)
                {
                    // copy original
                    for (int k = 0; k < cols.Length; k++) cols[k] = origCols[k];
                }
                else
                {
                    // fallback: fill with default
                    for (int k = 0; k < cols.Length; k++) cols[k] = defaultCol32;
                }
            }
            for (int ci = 0; ci < info.characterCount; ci++)
            {
                if (!info.characterInfo[ci].isVisible) continue;
                int mat = info.characterInfo[ci].materialReferenceIndex;
                int vIdx = info.characterInfo[ci].vertexIndex;
                var cols = info.meshInfo[mat].colors32;
                if (cols == null) continue;
                for (int q = 0; q < 4; q++) cols[vIdx + q] = defaultCol32;
            }

            // update other effects after revealing text hidden by tw.
            for (int i = 0; i < activeEffects.Count; i++)
            {
                var e = activeEffects[i];
                // clamp or will reading out of bounds
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
            // done
            if (visibleCount >= totalChars)
                break;

            t += delta;
            yield return null;
        }
        // finished (reveal all)
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        dialogueText.ForceMeshUpdate();

        fastForwardPulse = false;
        fastForwardHold = false;
        dialogueRoutine = null;

        if (DoAutoPlay) PlayNext();
    }
    /// <summary>
    /// Plays the next text in queue.
    /// </summary>
    public void PlayNext()
    {
        if (dialogueQueue.Count > 0)
        {
            if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
            BeginSpeaking();
        }
    }
    void Update()
    {
        fastForwardHold = Input.GetKey(FastForwardKey);
        if (Input.GetKeyDown(FastForwardKey))
        {
            // Only request a one-shot fast-forward if a dialogue core is playing
            if (dialogueRoutine != null)
                fastForwardPulse = true; // will be consumed in RunDialogue
        }
        if (Input.GetKeyDown(currentAdvanceKey))
        {
            // only go to next dialogue if current is done
            if (dialogueRoutine == null)
                PlayNext();
            
        }
    }
    void OnDisable()
    {
        if (dialogueRoutine != null)
        {
        StopCoroutine(dialogueRoutine);
        dialogueRoutine = null;
        }
    }
    /// <summary>
    /// Disables this below functionality.
    /// The default key is used whenever no key is passed to QueueDialog() or SetDialog().
    /// </summary>
    public void DisableDefaultKey()
    {
        DefaultAdvanceKey = KeyCode.None;
    }
    /// <summary>
    /// The default key is used whenever no key is passed to QueueDialog() or SetDialog().
    /// Setting default key to None will disable skipping for future dialog.
    /// </summary>
    public void SetDefaultKey(KeyCode key)
    {
        DefaultAdvanceKey = key;
    }

    void ParseTags(string input, out string cleanText, out List<TextEffect> effects, out List<TextEffectPause> pauses)
    {
        effects = new List<TextEffect>();
        pauses = new List<TextEffectPause>();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        Stack<(string tagName, int startIndex, string paramStr)> openTags = new Stack<(string, int, string)>();

        bool insideTag = false;
        string tagBuffer = "";

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
                    pauses.Add(new TextEffectPause(d) { startIndex = sb.Length });
                }
                else if (!closing)
                {
                    openTags.Push((tagName, sb.Length, paramStr));
                }
                else
                {
                    if (openTags.Count > 0)
                    {
                        var (openName, startIdx, pStr) = openTags.Pop();

                        if (openName != tagName)
                        {
                            Debug.LogWarning($"Mismatched tag: opened <{openName}> but closed </{tagName}>");
                            // Option 1: ignore the closing tag, op 2 is to process them somehow. but nah.
                            continue;                            
                        }
                        else
                        {
                            var eff = MakeEffect(openName, pStr);
                            if (eff != null)
                            {
                                eff.startIndex = startIdx;
                                eff.endIndex = sb.Length - 1;
                                effects.Add(eff);
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
            }
        }

        cleanText = sb.ToString();
    }


    TextEffect MakeEffect(string name, string paramStr)
    {
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
                    var parts = paramStr.Split(',');
                    if (parts.Length >= 2)
                    {
                        Color c1 = ParseColor(parts[0]);
                        Color c2 = ParseColor(parts[1]);
                        float dur = parts.Length > 2 ? ParseFloat(parts[2], 1f) : 1f;
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
        float v;
        if (float.TryParse(str, out v)) return v;
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
    public TextEffectTypewriter(float s) { speed = Mathf.Max(0.0001f, s); timer = 0f; revealed = 0; }
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float t) { }
    // helper to update internal progress
    public void UpdateProgress(float deltaSeconds)
    {
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
            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;
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
            if (i == startIndex) Debug.Log($"Wavy applies to char {i}, isVisible={info.characterInfo[i].isVisible}");
            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;
            Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(i, time * 3f) - 0.5f) * strengthX,
            (Mathf.PerlinNoise(i + 100, time * 3f) - 0.5f) * strengthY,
            0
            );
            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = orig[info.characterInfo[i].materialReferenceIndex].vertices[vIndex + j] + offset;
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
            if (!info.characterInfo[i].isVisible) continue;
            var colors = info.meshInfo[info.characterInfo[i].materialReferenceIndex].colors32;
            int vIndex = info.characterInfo[i].vertexIndex;
            for (int j = 0; j < 4; j++)
                colors[vIndex + j] = color;
        }
    }
}

/// <summary>
/// literrally a color gradient but i named it transition lmao.
/// </summary>
public class TextEffectColorTransition : TextEffect
{
    Color a, b;
    float speed;
    public TextEffectColorTransition(Color c1, Color c2, float spd) { a = c1; b = c2; speed = spd; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        Color c = Color.Lerp(a, b, (Mathf.Sin(time * speed) + 1f) * 0.5f);
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            var colors = info.meshInfo[info.characterInfo[i].materialReferenceIndex].colors32;
            int vIndex = info.characterInfo[i].vertexIndex;
            for (int j = 0; j < 4; j++)
                colors[vIndex + j] = c;
        }
    }
}


public class TextEffectMagnify : TextEffect
{
    float magSpeed, maxScale, length;
    public TextEffectMagnify(float speed, float scale, float len) { magSpeed = speed; maxScale = scale; length = len; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            float phase = (time * magSpeed + i) % length / length;
            float scale = 1f + Mathf.Sin(phase * Mathf.PI * 2f) * (maxScale - 1f);

            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;

            Vector3 center = Vector3.zero;
            for (int j = 0; j < 4; j++)
                center += orig[info.characterInfo[i].materialReferenceIndex].vertices[vIndex + j];
            center /= 4f;

            for (int j = 0; j < 4; j++)
                verts[vIndex + j] = center + (orig[info.characterInfo[i].materialReferenceIndex].vertices[vIndex + j] - center) * scale;
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




// for future anims idea: dropdown, construct, slam, expand, collapse.