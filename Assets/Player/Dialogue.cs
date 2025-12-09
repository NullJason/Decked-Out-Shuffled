#pragma warning disable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using System.Linq;


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
    [SerializeField] private bool IsMain = false;
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

    public static Dialogue DefaultDialogueMono;

    [Header("Preset Styles")]
    // [SerializeField] private TMP_SpriteAsset spriteAsset;
    // below two dictionaries are replacements for certain texts. 
    // for number styles it will detect if there are numbers in the word to the left or right and also apply the style to them, 
    // if not it will just apply the style to the word. if there is a <icon=index> tag somewhere in these presets then it should replace that tag with the image from TextIcons and this image should be rescaled keeping aspect ratio until it fits into the current line, the text needs to be displaced so it isnt covered by the icon.
    private static readonly Dictionary<string,string> PresetNameStyles = new Dictionary<string, string>
    {
        ["Entis"]="<color=#041842><i><b>Entis</b></i></color>",
    };
    private static readonly Dictionary<string,string> PresetNumberStyles = new Dictionary<string, string>
    {
        ["Soul Coins"]="<colorgradient=(0, 163, 164),(0, 188, 161)><i><b>Soul Coins</b></i></colorgradient>",
        ["Soul Embers"]="<colorgradient=(25, 212, 146),(2, 230, 188)><i><b>Soul Embers</b></i></colorgradient>"
    };
    [SerializeField] private List<Sprite> TextIcons = new List<Sprite>();

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
        if(IsMain) DefaultDialogueMono = this;
        if (dialogueText == null)
        {
            dialogueText = GetComponent<TextMeshProUGUI>();
            // if (TextIcons != null && TextIcons.Count > 0)
            // {
            //     InitializeSpriteAsset();
            // }
            
            if (InitialDialogueText!=null && InitialDialogueText != " " && InitialDialogueText != "")
            {
                QueueDialogue(InitialDialogueText);
                PlayNext();
            }
        }
    }
    // private void InitializeSpriteAsset()
    // {
    //     if (spriteAsset == null)
    //     {
    //         spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            
    //         // Import sprites from the list
    //         var spriteInfoList = new List<TMP_Sprite>();
            
    //         for (int i = 0; i < TextIcons.Count; i++)
    //         {
    //             var sprite = TextIcons[i];
    //             if (sprite != null)
    //             {
    //                 var spriteInfo = new TMP_Sprite
    //                 {
    //                     id = i,
    //                     name = sprite.name,
    //                     hashCode = TMP_TextUtilities.GetSimpleHashCode(sprite.name),
    //                     sprite = sprite,
    //                     width = sprite.rect.width,
    //                     height = sprite.rect.height,
    //                     pivot = new Vector2(0.5f, 0.5f),
    //                     x = sprite.rect.x,
    //                     y = sprite.rect.y,
    //                     xAdvance = sprite.rect.width,
    //                     scale = 1.0f
    //                 };
    //                 spriteInfoList.Add(spriteInfo);
    //             }
    //         }
            
    //         spriteAsset.spriteInfoList = spriteInfoList;
    //         spriteAsset.UpdateLookupTables();
            
    //         // Assign to dialogue text
    //         if (dialogueText != null)
    //         {
    //             dialogueText.spriteAsset = spriteAsset;
    //         }
    //     }
    // }
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
                        if (i < tt.startIndex + tt.revealed)
                        {
                            isRevealed = true;
                        }
                        else
                        {
                            isRevealed = false;
                        }
                        break;
                    }
                    // if (i >= tt.startIndex && i <= tt.endIndex)
                    // {
                    //     int charsToReveal = tt.revealed;
                    //     int lastRevealedIndex = tt.startIndex + charsToReveal - 1;

                    //     if (charsToReveal <= 0 || i > lastRevealedIndex)
                    //     {
                    //         isRevealed = false;
                    //     }
                    //     break;
                    // }
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
                if (e is DelayedTextEffect delayedEffect)
                {
                    if (delayedEffect.ShouldStartEffect(t, visibleMask))
                    {
                        delayedEffect.Apply(dialogueText, info, originalInfo, t);
                    }
                }
                else
                {
                    // Apply non-delayed effects immediately
                    e.Apply(dialogueText, info, originalInfo, t);
                }
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
        // Debug.Log("Playing Next dialogue");
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
    public void Play(string txt = "", float autoplay = -1, bool disableOnComplete = false, bool enableOnStart = true)
    {
        // Debug.Log("Playing text");
        ClearDialogue();
        SetAutoPlay(autoplay);
        string[] parts = txt.Split(new string[] { "<break>" }, 0);

        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
                QueueDialogue(trimmed);
        }
        gameObject.SetActive(enableOnStart);
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
    private string ApplyPresetStyles(string text)
    {
        string result = text;

        Player plr = FindFirstObjectByType<Player>();
        if(plr != null){
            string playerName = plr.PlayerName;
            result = Regex.Replace(result, "PlayerName", playerName, RegexOptions.IgnoreCase);
        }
        
        foreach (var preset in PresetNameStyles)
        {
            string pattern = $@"\b{Regex.Escape(preset.Key)}\b";
            result = Regex.Replace(result, pattern, match => preset.Value, RegexOptions.IgnoreCase);
        }
        
        foreach (var preset in PresetNumberStyles)
        {
            string template = preset.Value;
            string key = preset.Key;
            
            int keyIndex = template.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0) continue;
            
            string openingTags = template.Substring(0, keyIndex);
            string closingTags = template.Substring(keyIndex + key.Length);
            
            string[] keyWords = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string keyPattern = string.Join(@"\s+", keyWords.Select(Regex.Escape));
            
            string pattern = $@"\b(?:(?<before>\d+)\s+)?{keyPattern}(?:\s+(?<after>\d+))?\b";
            
            result = Regex.Replace(result, pattern, 
                match => openingTags + match.Value + closingTags,
                RegexOptions.IgnoreCase);
        }
        Debug.Log(result);
        return result;
    }
    // private bool IsCharacterRevealed(int charIndex, List<TextEffectTypewriter> typewriters)
    // {
    //     if (typewriters == null || typewriters.Count == 0) return true;
        
    //     foreach (var tt in typewriters)
    //     {
    //         if (charIndex >= tt.startIndex && charIndex <= tt.endIndex)
    //         {
    //             return tt.revealed > 0 && charIndex <= tt.startIndex + tt.revealed - 1;
    //         }
    //     }
        
    //     return true;
    // }
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

        string processedInput = ApplyPresetStyles(input);
    
        processedInput = ProcessIconTags(processedInput);
        

        for (int i = 0; i < processedInput.Length; i++)
        {
            char c = processedInput[i];

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
                
                if (tagName == "sprite")
                {
                    sb.Append("<" + tagContent + ">");
                    continue;
                }

                if (IsTMPTag(tagContent))
                {
                    // Let TMP handle it & just append it back to text.
                    sb.Append("<" + tagBuffer + ">");
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
                                string cleanSoFar = sb.ToString();
                                // Trim trailing and starting whitespace from effect range 
                                while (startIdx < cleanSoFar.Length && startIdx <= endIdx && 
                                    char.IsWhiteSpace(cleanSoFar[startIdx]))
                                {
                                    startIdx++;
                                }
                                 
                                while (endIdx >= 0 && endIdx >= startIdx && 
                                    char.IsWhiteSpace(cleanSoFar[endIdx]))
                                {
                                    endIdx--;
                                }
                                if (endIdx < startIdx)
                                {
                                    endIdx = startIdx;
                                }
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
                                        string segmentText = cleanSoFar.Substring(startIdx, endIdx - startIdx + 1);
                                        typewriter.SetText(segmentText);
                                        
                                        foreach (var stutterEffect in effects)
                                        {
                                            if (stutterEffect is TextEffectStutter stutter && 
                                                stutter.startIndex >= startIdx && stutter.startIndex <= endIdx)
                                            {
                                                stutter.startIndex -= startIdx; // Make relative to typewriter start
                                                typewriter.stutterEffects.Add(stutter);
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
    private string ProcessIconTags(string text)
    {
        string pattern = @"<icon=(\d+)>";
        return Regex.Replace(text, pattern, match =>
        {
            if (int.TryParse(match.Groups[1].Value, out int index) && index >= 0 && index < TextIcons.Count)
            {
                return $"<sprite index=\"{index}\" height=\"1em\">";
            }
            return match.Value;
        });
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
                case "gravity": return new TextEffectGravity();
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
            case "stutter":
                {
                    var parts = paramStr.Split(',');
                    if (parts.Length >= 2)
                    {
                        int index = (int)ParseFloat(parts[0], 0f);
                        string replacement = parts[1];
                        return new TextEffectStutter(index, replacement);
                    }
                    break;
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
                    var parts = SplitTopLevelArgs(paramStr);
                    float intensity = parts.Length > 0 ? ParseFloat(parts[0], 5f) : 5f;
                    float delay = parts.Length > 1 ? ParseFloat(parts[1], 0f) : 0f;
                    return new TextEffectQuake(intensity, delay);               
                }
            case "drop":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 2f) : 2f;
                    float bounce = parts.Length > 1 ? ParseFloat(parts[1], 10f) : 10f;
                    float angle = parts.Length > 2 ? ParseFloat(parts[2], -90f) : -90f;
                    float delay = parts.Length > 3 ? ParseFloat(parts[3], 0f) : 0f;
                    return new TextEffectDrop(speed, bounce, angle, delay);
                }
            case "rain":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 2f) : 2f;
                    float bounce = parts.Length > 1 ? ParseFloat(parts[1], 10f) : 10f;
                    float angle = parts.Length > 2 ? ParseFloat(parts[2], -90f) : -90f;
                    float delay = parts.Length > 3 ? ParseFloat(parts[3], 0f) : 0f;
                    return new TextEffectRain(speed, bounce, angle, delay);
                }
            case "construct":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float length = parts.Length > 0 ? ParseFloat(parts[0], 50f) : 50f;
                    float speed = parts.Length > 1 ? ParseFloat(parts[1], 1f) : 1f;
                    float delay = parts.Length > 2 ? ParseFloat(parts[2], 0f) : 0f;
                    return new TextEffectConstruct(length, speed, delay);
                }
            case "slam":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float speed = parts.Length > 0 ? ParseFloat(parts[0], 3f) : 3f;
                    int bounces = parts.Length > 1 ? (int)ParseFloat(parts[1], 2f) : 2;
                    float delay = parts.Length > 2 ? ParseFloat(parts[2], 0f) : 0f;
                    return new TextEffectSlam(speed, bounces, delay);
                }
            case "explode":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float force = parts.Length > 0 ? ParseFloat(parts[0], 50f) : 50f;
                    float gravity = parts.Length > 1 ? ParseFloat(parts[1], 9.8f) : 9.8f;
                    float delay = parts.Length > 2 ? ParseFloat(parts[2], 0f) : 0f;
                    return new TextEffectExplode(force, gravity, delay);
                }
            case "collapse":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float rotation = parts.Length > 0 ? ParseFloat(parts[0], 30f) : 30f;
                    float drop = parts.Length > 1 ? ParseFloat(parts[1], 20f) : 20f;
                    float delay = parts.Length > 2 ? ParseFloat(parts[2], 0f) : 0f;
                    return new TextEffectCollapse(rotation, drop, delay);
                }
            case "gravity":
                {
                    var parts = SplitTopLevelArgs(paramStr);
                    float gravity = parts.Length > 0 ? ParseFloat(parts[0], 9.8f) : 9.8f;
                    float velocity = parts.Length > 1 ? ParseFloat(parts[1], 0f) : 0f;
                    float delay = parts.Length > 2 ? ParseFloat(parts[2], 0f) : 0f;
                    return new TextEffectGravity(gravity, velocity, delay);
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
public abstract class DelayedTextEffect : TextEffect
{
    public float delay = 0f;
    protected float effectStartTime = -1f;
    protected bool effectStarted = false;
    
    public virtual void StartEffect(float currentTime)
    {
        effectStartTime = currentTime;
        effectStarted = true;
    }
    
    public bool ShouldStartEffect(float currentTime, bool[] visibleMask)
    {
        for (int i = startIndex; i <= endIndex && i < visibleMask.Length; i++)
        {
            if (!visibleMask[i]) return false;
        }
        
        if (!effectStarted)
        {
            StartEffect(currentTime);
        }
        return effectStarted;
    }
    
    public float GetEffectTime(float currentTime)
    {
        if (!effectStarted) return 0f;
        return currentTime - effectStartTime - delay;
    }
}

public class TextEffectTypewriter : TextEffect
{
    public float speed;
    public float timer = 0f;
    public int revealed = 0;
    public List<int> pauseIndices = new List<int>();
    public List<float> pauseDurations = new List<float>();
    public List<TextEffectStutter> stutterEffects = new List<TextEffectStutter>();
    private float pauseTimeRemaining = 0f;
    private int currentPauseIndex = -1;
    private string originalText;  
    private string currentText;   
    public TextEffectTypewriter(float s) { speed = Mathf.Max(0.0001f, s); timer = 0f; revealed = 0; }
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float t) { }
    public void SetText(string text)
    {
        originalText = text;
        currentText = text;
    }
    private void CheckStutterReplacements()
    {
        foreach (var stutter in stutterEffects)
        {
            if (!stutter.triggered && revealed >= stutter.triggerIndex)
            {
                stutter.triggered = true;
                
                int currentPosition = Mathf.Min(revealed, currentText.Length);
                
                int wordStart = 0;
                int wordEnd = currentText.Length;
                
                for (int i = currentPosition - 1; i >= 0; i--)
                {
                    if (char.IsWhiteSpace(currentText[i]))
                    {
                        wordStart = i + 1;
                        break;
                    }
                }
                
                for (int i = currentPosition; i < currentText.Length; i++)
                {
                    if (char.IsWhiteSpace(currentText[i]))
                    {
                        wordEnd = i;
                        break;
                    }
                }
                
                if (wordStart < wordEnd && wordEnd <= currentText.Length)
                {
                    currentText = currentText.Substring(0, wordStart) + 
                                  stutter.replacementText + 
                                  currentText.Substring(wordEnd);
                    
                    int lengthDiff = stutter.replacementText.Length - (wordEnd - wordStart);
                    revealed = Mathf.Max(0, revealed + lengthDiff);
                }
            }
        }
    }
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
        CheckStutterReplacements();
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
        timeOffset = UnityEngine.Random.Range(0f, duration); // UnityEngine.Random offset for variety
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
        if (info.characterCount == 0 || startIndex >= info.characterCount) return;
        
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        
        List<float> distances = new List<float>();
        List<int> visibleIndices = new List<int>();
        
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
            visibleIndices.Add(i);
        }
        
        if (distances.Count == 0) return;
        
        float minDist = float.MaxValue;
        float maxDist = float.MinValue;
        foreach (float dist in distances)
        {
            minDist = Mathf.Min(minDist, dist);
            maxDist = Mathf.Max(maxDist, dist);
        }
        
        if (Mathf.Approximately(minDist, maxDist)) return;
        
        for (int idx = 0; idx < visibleIndices.Count; idx++)
        {
            int i = visibleIndices[idx];
            float t = Mathf.InverseLerp(minDist, maxDist, distances[idx]);
            Color c = Color.Lerp(a, b, t);
            
            var charInfo = info.characterInfo[i];
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var cols = info.meshInfo[mat].colors32;
            if (cols == null || cols.Length <= vIndex) continue;

            for (int j = 0; j < 4; j++)
                cols[vIndex + j] = c;
        }
    }
}
public class TextEffectQuake : DelayedTextEffect
{
    float intensity;
    Vector3[] originalPositions;
    bool initialized = false;

    public TextEffectQuake(float intensity = 5f, float delay = 0f)
    {
        this.intensity = intensity;
        this.delay = delay;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            originalPositions = new Vector3[info.characterCount * 4];
            for (int i = 0; i < info.characterCount; i++)
            {
                if (!info.characterInfo[i].isVisible) continue;
                for (int j = 0; j < 4; j++)
                {
                    originalPositions[i * 4 + j] = orig[info.characterInfo[i].materialReferenceIndex].vertices[info.characterInfo[i].vertexIndex + j];
                }
            }
        }

        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            // Stronger, more abrupt shaking
            float shakeAmount = Mathf.PerlinNoise(i * 0.5f + effectTime * 20f, 0) - 0.5f;
            Vector3 offset = new Vector3(
                shakeAmount * intensity * 2f,
                (Mathf.PerlinNoise(i * 0.5f + 100f, effectTime * 20f) - 0.5f) * intensity * 2f,
                0
            );

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = originalPositions[i * 4 + j];
                verts[vIndex + j] = origPos + offset;
            }
        }
    }
}
public class TextEffectDrop : DelayedTextEffect
{
    float speed;
    float bounceAmount;
    float angle;
    float[] dropTimes;
    bool initialized = false;

    public TextEffectDrop(float speed = 2f, float bounceAmount = 10f, float angle = -90f, float delay = 0f)
    {
        this.speed = speed;
        this.bounceAmount = bounceAmount;
        this.angle = angle * Mathf.Deg2Rad;
        this.delay = delay;
    }

    public override void StartEffect(float currentTime)
    {
        base.StartEffect(currentTime);
        dropTimes = new float[endIndex - startIndex + 1];
        for (int i = 0; i < dropTimes.Length; i++)
        {
            dropTimes[i] = UnityEngine.Random.Range(0f, 0.3f); // Staggered drop
        }
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (dropTimes == null) return;
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;

        Vector2 dropDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            int idx = i - startIndex;
            float charEffectTime = Mathf.Max(0f, effectTime - dropTimes[idx]);
            float dropProgress = Mathf.Clamp01(charEffectTime * speed);

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
public class TextEffectRain : DelayedTextEffect
{
    float speed;
    float bounceAmount;
    float angle;
    float[] dropTimes;
    bool initialized = false;

    public TextEffectRain(float speed = 2f, float bounceAmount = 10f, float angle = -90f, float delay = 0f)
    {
        this.speed = speed;
        this.bounceAmount = bounceAmount;
        this.angle = angle * Mathf.Deg2Rad;
        this.delay = delay;
    }

    public override void StartEffect(float currentTime)
    {
        base.StartEffect(currentTime);
        dropTimes = new float[endIndex - startIndex + 1];
        for (int i = 0; i < dropTimes.Length; i++)
        {
            dropTimes[i] = UnityEngine.Random.Range(0f, 2f); // More staggered for rain effect
        }
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (dropTimes == null) return;
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;

        Vector2 dropDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            int idx = i - startIndex;
            float charEffectTime = Mathf.Max(0f, effectTime - dropTimes[idx]);
            float dropProgress = Mathf.Clamp01(charEffectTime * speed);

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
public class TextEffectConstruct : DelayedTextEffect
{
    float pieceTravelDistance;
    float buildSpeed;
    float[] pieceTimes;
    bool initialized = false;

    public TextEffectConstruct(float pieceTravelDistance = 50f, float buildSpeed = 1f, float delay = 0f)
    {
        this.pieceTravelDistance = pieceTravelDistance;
        this.buildSpeed = buildSpeed;
        this.delay = delay;
    }

    public override void StartEffect(float currentTime)
    {
        base.StartEffect(currentTime);
        // Initialize piece times with UnityEngine.Random delays
        pieceTimes = new float[(endIndex - startIndex + 1) * 4];
        for (int i = 0; i < pieceTimes.Length; i++)
        {
            pieceTimes[i] = UnityEngine.Random.Range(0f, 1f); // UnityEngine.Random delay for each piece
        }
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (pieceTimes == null) return;
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            
            Vector3 center = (orig[mat].vertices[vIndex] + orig[mat].vertices[vIndex + 2]) * 0.5f;
            
            for (int j = 0; j < 4; j++)
            {
                int pieceIndex = (i - startIndex) * 4 + j;
                float pieceDelay = pieceTimes[pieceIndex];
                float pieceEffectTime = Mathf.Max(0f, effectTime - pieceDelay);
                
                float constructProgress = Mathf.Clamp01(pieceEffectTime * buildSpeed);
                
                // Each piece comes from a different direction
                Vector3 scatterDirection = new Vector3(
                    (j % 2 == 0 ? 1 : -1),
                    (j < 2 ? 1 : -1),
                    0
                ).normalized;
                
                Vector3 startPos = center + scatterDirection * pieceTravelDistance;
                Vector3 targetPos = orig[mat].vertices[vIndex + j];
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, constructProgress);
                
                verts[vIndex + j] = currentPos;
            }
        }
    }
}
public class TextEffectSlam : DelayedTextEffect
{
    float speed;
    int bounceCount;
    bool initialized = false;

    public TextEffectSlam(float speed = 3f, int bounceCount = 2, float delay = 0f)
    {
        this.speed = speed;
        this.bounceCount = bounceCount;
        this.delay = delay;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;
        
        float slamProgress = Mathf.Clamp01(effectTime * speed);
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

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            Vector3 center = (orig[mat].vertices[vIndex] + orig[mat].vertices[vIndex + 2]) * 0.5f;

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = center + (origPos - center) * scale;
            }
        }
    }
}
public class TextEffectExplode : DelayedTextEffect
{
    float explosionForce;
    float gravity;
    Vector2[] velocities;
    bool initialized = false;

    public TextEffectExplode(float explosionForce = 50f, float gravity = 9.8f, float delay = 0f)
    {
        this.explosionForce = explosionForce;
        this.gravity = gravity;
        this.delay = delay;
    }

    public override void StartEffect(float currentTime)
    {
        base.StartEffect(currentTime);
        
        if (!initialized)
        {
            initialized = true;
            velocities = new Vector2[endIndex - startIndex + 1];
            
            // Calculate center of the word
            // We'll use the first character as reference
            for (int i = 0; i < velocities.Length; i++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                velocities[i] = direction * explosionForce * UnityEngine.Random.Range(0.8f, 1.2f);
            }
        }
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (velocities == null) return;
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;

        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;

            int idx = i - startIndex;
            
            // Apply gravity
            velocities[idx].y -= gravity * Time.deltaTime;
            
            Vector2 displacement = velocities[idx] * effectTime;
            
            // Don't let characters fall below original line
            Vector3 originalBottom = new Vector3(
                orig[mat].vertices[vIndex].x,
                Mathf.Min(orig[mat].vertices[vIndex].y, orig[mat].vertices[vIndex + 1].y),
                0
            );
            
            Vector3 newPos = orig[mat].vertices[vIndex] + (Vector3)displacement;
            
            // Keep above original line
            if (newPos.y < originalBottom.y)
            {
                newPos.y = originalBottom.y;
                velocities[idx].y = Mathf.Abs(velocities[idx].y) * 0.5f; // Bounce
            }

            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                Vector3 offset = origPos - orig[mat].vertices[vIndex];
                verts[vIndex + j] = newPos + offset;
            }
        }
    }
}
public class TextEffectCollapse : DelayedTextEffect
{
    float rotationIntensity;
    float dropDistance;
    float[] rotations;
    float[] dropTimes;
    bool initialized = false;
    
    public TextEffectCollapse(float rotationIntensity = 30f, float dropDistance = 20f, float delay = 0f)
    {
        this.rotationIntensity = rotationIntensity;
        this.dropDistance = dropDistance;
        this.delay = delay;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized)
        {
            initialized = true;
            rotations = new float[endIndex - startIndex + 1];
            dropTimes = new float[endIndex - startIndex + 1];
            
            // UnityEngine.Random rotation for each character
            for (int i = 0; i < rotations.Length; i++)
            {
                rotations[i] = UnityEngine.Random.Range(-rotationIntensity, rotationIntensity);
                dropTimes[i] = UnityEngine.Random.Range(0f, 0.5f); // Staggered drop start
            }
        }
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;
        
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            
            int idx = i - startIndex;
            float charEffectTime = Mathf.Max(0f, effectTime - dropTimes[idx]);
            float collapseProgress = Mathf.Clamp01(charEffectTime * 2f);
            
            // Apply rotation and drop
            float rotation = rotations[idx] * collapseProgress;
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
        this.speed = speed; 
        maxScale = scale; 
        falloff = len;
    }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        int span = endIndex - startIndex + 1;
        
        // Total distance the lens travels in one complete cycle
        float cycleLength = span + 2 * falloff;
        
        // Lens position moving continuously
        float rawPos = time * speed;
        
        // Calculate lens position with wrap-around
        float lensPos = startIndex - falloff + (rawPos % cycleLength);
        
        for (int i = startIndex; i <= endIndex; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            Vector3[] origVerts = orig[mat].vertices;

            float dist = Mathf.Abs(i - lensPos);
            
            // Also check distance to lens in the next cycle (for seamless transition)
            if (dist > falloff)
            {
                // Check if we're close to the wrap-around point
                float distToWrap = Mathf.Abs(i - (lensPos - cycleLength));
                dist = Mathf.Min(dist, distToWrap);
            }

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

public class TextEffectStutter : TextEffect
{
    public int triggerIndex;
    public string replacementText;
    public bool triggered = false;
    
    public TextEffectStutter(int index, string replacement)
    {
        triggerIndex = index;
        replacementText = replacement;
    }
    
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] originalVerts, float time)
    {
        // This effect is handled by the typewriter, not applied here
    }
}
public class TextEffectGravity : DelayedTextEffect
{
    float gravityStrength;
    float initialVelocity;
    Vector2[] velocities;
    Vector2[] positions;
    bool[] hasCollided;
    bool initialized = false;
    
    public TextEffectGravity(float gravityStrength = 9.8f, float initialVelocity = 0f, float delay = 0f)
    {
        this.gravityStrength = gravityStrength;
        this.initialVelocity = initialVelocity;
        this.delay = delay;
    }
    
    public override void StartEffect(float currentTime)
    {
        base.StartEffect(currentTime);
        if (!initialized)
        {
            initialized = true;
            velocities = new Vector2[endIndex - startIndex + 1];
            positions = new Vector2[endIndex - startIndex + 1];
            hasCollided = new bool[endIndex - startIndex + 1];
            
            for (int i = 0; i < velocities.Length; i++)
            {
                velocities[i] = new Vector2(
                    UnityEngine.Random.Range(-1f, 1f) * initialVelocity,
                    UnityEngine.Random.Range(-1f, 1f) * initialVelocity
                );
                positions[i] = Vector2.zero;
            }
        }
    }
    
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, TMP_MeshInfo[] orig, float time)
    {
        if (!initialized) return;
        
        float effectTime = GetEffectTime(time);
        if (effectTime < 0) return;
        
        // Get screen bounds (simplified)
        Rect screenBounds = new Rect(0, 0, Screen.width, Screen.height);
        
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            var charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;
            
            int idx = i - startIndex;
            
            // Apply gravity
            if (!hasCollided[idx])
            {
                velocities[idx].y -= gravityStrength * Time.deltaTime;
                
                // Update position
                positions[idx] += velocities[idx] * Time.deltaTime;
                
                // Check screen bounds collision
                Vector2 charSize = new Vector2(
                    charInfo.topRight.x - charInfo.bottomLeft.x,
                    charInfo.topRight.y - charInfo.bottomLeft.y
                );
                
                // Simple AABB collision with screen bounds
                // This is simplified - you'd need proper screen-space conversion
                
                // Check bottom collision
                if (positions[idx].y < screenBounds.yMin + charSize.y * 0.5f)
                {
                    positions[idx].y = screenBounds.yMin + charSize.y * 0.5f;
                    velocities[idx].y = Mathf.Abs(velocities[idx].y) * 0.3f; // Bounce with damping
                    
                    if (Mathf.Abs(velocities[idx].y) < 0.1f)
                    {
                        hasCollided[idx] = true;
                        velocities[idx] = Vector2.zero;
                    }
                }
                
                // Check top collision
                if (positions[idx].y > screenBounds.yMax - charSize.y * 0.5f)
                {
                    positions[idx].y = screenBounds.yMax - charSize.y * 0.5f;
                    velocities[idx].y = -Mathf.Abs(velocities[idx].y) * 0.3f;
                }
                
                // Check sides
                if (positions[idx].x < screenBounds.xMin + charSize.x * 0.5f)
                {
                    positions[idx].x = screenBounds.xMin + charSize.x * 0.5f;
                    velocities[idx].x = Mathf.Abs(velocities[idx].x) * 0.3f;
                }
                
                if (positions[idx].x > screenBounds.xMax - charSize.x * 0.5f)
                {
                    positions[idx].x = screenBounds.xMax - charSize.x * 0.5f;
                    velocities[idx].x = -Mathf.Abs(velocities[idx].x) * 0.3f;
                }
            }
            
            int mat = charInfo.materialReferenceIndex;
            int vIndex = charInfo.vertexIndex;
            var verts = info.meshInfo[mat].vertices;
            
            // Apply position offset
            for (int j = 0; j < 4; j++)
            {
                Vector3 origPos = orig[mat].vertices[vIndex + j];
                verts[vIndex + j] = origPos + (Vector3)positions[idx];
            }
        }
    }
}