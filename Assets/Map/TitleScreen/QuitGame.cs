using UnityEngine;
using UnityEngine.UI;

public class QuitGame : MonoBehaviour
{
    [SerializeField] Button button;
    void OnEnable()
    {
        if (button == null) TryGetComponent<Button>(out button);
        if (button != null) button.onClick.AddListener(ExitProgram);
    }
    public static void ExitProgram()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
                    Application.ExternalEval("window.close();");
        #elif UNITY_STANDALONE
                    Application.Quit();
        #else
                    Application.Quit();
        #endif
    }
}
