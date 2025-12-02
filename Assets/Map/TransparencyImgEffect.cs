using System.Collections;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;

public class TransparencyImgEffect : MonoBehaviour
{
    [SerializeField] private float CycleDuration = 8;
    [SerializeField] private float StartTransparency = 1;
    [SerializeField] private float EndTransparency = 230/255;
    [SerializeField] private int Cycles = -1;
    [SerializeField] private CanvasGroup image;
    private int CurrentCycle = 0;
    private bool CycleDir = true;

    void OnEnable()
    {
        if(image == null) {if(!TryGetComponent<CanvasGroup>(out image)) transform.AddComponent<CanvasGroup>();}
        CurrentCycle = 0;
        if (StartTransparency > EndTransparency) {CycleDir = false; float tmp = EndTransparency; EndTransparency = StartTransparency; StartTransparency = tmp; }
        else if (EndTransparency > StartTransparency) CycleDir = true;
        else return;

        StartCoroutine(TransparencyEffect());
    }

    IEnumerator TransparencyEffect()
    {
        float elapsed = 0;
        while (Cycles == -1 || CurrentCycle < Cycles)
        {
            elapsed+=Time.deltaTime;
            if(elapsed >= CycleDuration) { CycleDir = !CycleDir; if(Cycles>0) Cycles++; elapsed = 0; }
            float t = Mathf.Clamp01(elapsed / CycleDuration);
            image.alpha = CycleDir ?Mathf.Lerp(StartTransparency, EndTransparency, t) :Mathf.Lerp(EndTransparency, StartTransparency, t);
            yield return null;
        }
    }
}
