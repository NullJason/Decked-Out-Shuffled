using UnityEngine;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    [SerializeField] Button button;
    void OnEnable()
    {
        if (button == null) TryGetComponent<Button>(out button);
        if (button != null) button.onClick.AddListener(OnClick_Load);
    }
    void OnClick_Load()
    {
        SceneTransition.StartTransition("Environment");
    }
}
