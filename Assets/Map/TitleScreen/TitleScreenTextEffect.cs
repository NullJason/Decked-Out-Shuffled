using TMPro;
using UnityEngine;

public class TitleScreenTextEffect : MonoBehaviour
{
    [SerializeField] string Text;
    [SerializeField] Dialogue dialogue;
    public TextMeshProUGUI tmpc_Text;
    void Start()
    {
        dialogue.Play(Text);
    }

   
}
