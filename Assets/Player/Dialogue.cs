using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
///  Generates text for dialogues. This uses TMP btw.
/// contains some code from previous projects, redone some to fit and also reorganize because it was a huge mess.
/// </summary>
public class Dialogue : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI dialogueText;

    Queue<(string text, KeyCode key)> dialogueQueue = new Queue<(string, KeyCode)>();
    List<TextEffect> activeEffects = new List<TextEffect>();

    Coroutine dialogueRoutine;
    KeyCode currentAdvanceKey;
    float pauseUntil = 0f;

    void Start()
    {
        if (dialogueText == null)
            dialogueText = GetComponent<TextMeshProUGUI>();
    }

    
    public void QueueDialogue(string newText, KeyCode key)
    {
        dialogueQueue.Enqueue((newText, key));

        if (dialogueRoutine == null)
            BeginSpeaking();
    }
    void BeginSpeaking()
    {
        if (dialogueQueue.Count == 0) return;
        var (txt, key) = dialogueQueue.Dequeue();
        currentAdvanceKey = key;

        ParseTags(txt, out string clean, out activeEffects);
        dialogueRoutine = StartCoroutine(RunDialogue(clean));
    }

    IEnumerator RunDialogue(string text)
    {
        dialogueText.text = text;
        dialogueText.ForceMeshUpdate();

        int visibleCount = 0;
        float t = 0;

        while (true)
        {
            if (Time.time < pauseUntil)
            {
                yield return null;
                continue;
            }

            dialogueText.maxVisibleCharacters = visibleCount;
            foreach (var eff in activeEffects)
            {
                if (eff is TextEffectPause pause &&
                    visibleCount >= eff.startIndex && visibleCount <= eff.endIndex)
                {
                    pauseUntil = Time.time + pause.Duration;
                }
            }
            // do the effects
            dialogueText.ForceMeshUpdate();
            var info = dialogueText.textInfo;
            foreach (var eff in activeEffects)
                eff.Apply(dialogueText, info, t);

            for (int i = 0; i < info.meshInfo.Length; i++)
                info.meshInfo[i].mesh.colors32 = info.meshInfo[i].colors32;
            dialogueText.UpdateVertexData();

            // advance the typewrite
            var tw = activeEffects.Find(e => e is TextEffectTypewriter) as TextEffectTypewriter;
            if (tw != null && visibleCount < text.Length)
            {
                visibleCount = Mathf.Min(text.Length, (int)(t * tw.speed));
            }

            // End condition
            if (visibleCount >= text.Length)
                break;

            t += Time.deltaTime;
            yield return null;
        }

        dialogueRoutine = null;
    }
    public void PlayNext()
    {
        if (dialogueRoutine == null && dialogueQueue.Count > 0)
            BeginSpeaking();
    }
    void Update()
    {
        if (dialogueRoutine == null) return;
        if (Input.GetKeyDown(currentAdvanceKey))
            PlayNext();
    }


    void ParseTags(string input, out string cleanText, out List<TextEffect> effects)
    {
        effects = new List<TextEffect>();
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

                if (!closing)
                {
                    // put eff
                    openTags.Push((tagName, sb.Length, paramStr));
                }
                else
                {
                    // pop eff
                    if (openTags.Count > 0)
                    {
                        var (openName, startIdx, pStr) = openTags.Pop();
                        if (openName == tagName)
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
                Color col;
                if (ColorUtility.TryParseHtmlString(paramStr, out col))
                    return new TextEffectColor(col);
                break;

            case "colortransition":
                {
                    string[] ca = paramStr.Split(',');
                    Color c1 = Color.white, c2 = Color.red;
                    float spd = 2f;
                    if (ca.Length > 0) ColorUtility.TryParseHtmlString(ca[0], out c1);
                    if (ca.Length > 1) ColorUtility.TryParseHtmlString(ca[1], out c2);
                    if (ca.Length > 2) spd = ParseFloat(ca[2], 2f);
                    return new TextEffectColorTransition(c1, c2, spd);
                }

            case "magnify":
                {
                    string[] ma = paramStr.Split(',');
                    float ms = ma.Length > 0 ? ParseFloat(ma[0], 3f) : 3f;
                    float maxSize = ma.Length > 1 ? ParseFloat(ma[1], 2f) : 2f;
                    float len = ma.Length > 2 ? ParseFloat(ma[2], 5f) : 5f;
                    return new TextEffectMagnify(ms, maxSize, len);
                }
            case "pause":
                float d = ParseFloat(paramStr, 1f);
                return new TextEffectPause(d);
        }
        return null;
    }


    float ParseFloat(string str, float def)
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
    public abstract void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time);
}


public class TextEffectTypewriter : TextEffect
{
    public float speed;
    public TextEffectTypewriter(float s) { speed = s; }
    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time) { }
}

public class TextEffectWavy : TextEffect
{
    float waveSpeed, amplitude;
    public TextEffectWavy(float speed, float amp) { waveSpeed = speed; amplitude = amp; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;
            Vector3 offset = new Vector3(0, Mathf.Sin(time * waveSpeed + i) * amplitude, 0);
            for (int j = 0; j < 4; j++) verts[vIndex + j] += offset;
        }
    }
}


public class TextEffectShake : TextEffect
{
    float strengthX, strengthY;
    public TextEffectShake(float sx, float sy) { strengthX = sx; strengthY = sy; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;
            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(i, time * 3f) * strengthX,
                Mathf.PerlinNoise(i + 100, time * 3f) * strengthY,
                0
            );
            for (int j = 0; j < 4; j++) verts[vIndex + j] += offset;
        }
    }
}


public class TextEffectColor : TextEffect
{
    Color color;
    public TextEffectColor(Color c) { color = c; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            var colors = info.meshInfo[info.characterInfo[i].materialReferenceIndex].colors32;
            int vIndex = info.characterInfo[i].vertexIndex;
            for (int j = 0; j < 4; j++) colors[vIndex + j] = color;
        }
    }
}


public class TextEffectColorTransition : TextEffect
{
    Color a, b;
    float speed;
    public TextEffectColorTransition(Color c1, Color c2, float spd) { a = c1; b = c2; speed = spd; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time)
    {
        Color c = Color.Lerp(a, b, (Mathf.Sin(time * speed) + 1f) * 0.5f);
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            var colors = info.meshInfo[info.characterInfo[i].materialReferenceIndex].colors32;
            int vIndex = info.characterInfo[i].vertexIndex;
            for (int j = 0; j < 4; j++) colors[vIndex + j] = c;
        }
    }
}


public class TextEffectMagnify : TextEffect
{
    float magSpeed, maxScale, length;
    public TextEffectMagnify(float speed, float scale, float len) { magSpeed = speed; maxScale = scale; length = len; }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float time)
    {
        for (int i = startIndex; i <= endIndex && i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;
            float phase = (time * magSpeed + i) % length / length;
            float scale = 1f + Mathf.Sin(phase * Mathf.PI * 2f) * (maxScale - 1f);

            var verts = info.meshInfo[info.characterInfo[i].materialReferenceIndex].vertices;
            int vIndex = info.characterInfo[i].vertexIndex;

            Vector3 center = Vector3.zero;
            for (int j = 0; j < 4; j++) center += verts[vIndex + j];
            center /= 4f;

            for (int j = 0; j < 4; j++) verts[vIndex + j] = center + (verts[vIndex + j] - center) * scale;
        }
    }
}


public class TextEffectPause : TextEffect
{
    float duration;
    public TextEffectPause(float time) { duration = Mathf.Max(0.01f, time); }

    public override void Apply(TextMeshProUGUI tmp, TMP_TextInfo info, float t) { }
    public float Duration => duration;
}
