using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
public class AudioScrollControl : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;      
    [SerializeField] private Slider audSlider;   
    [SerializeField] private string exposedParam = "MainVolume";

    private const float MuteDb = -80f;

    private void Start()
    {
        if(audSlider == null) audSlider = GetComponent<Slider>();
        audSlider.value = 0.5f;
        ApplyaudSlider(audSlider.value);
        audSlider.onValueChanged.AddListener(ApplyaudSlider);
    }

    private void ApplyaudSlider(float value)
    {
        float amplitude = value * 2f;

        if (amplitude <= 0f)
        {
            mixer.SetFloat(exposedParam, MuteDb);
        }
        else
        {
            float dB = 20f * Mathf.Log10(amplitude);
            dB = Mathf.Max(dB, MuteDb);
            mixer.SetFloat(exposedParam, dB);
        }
    }
}
