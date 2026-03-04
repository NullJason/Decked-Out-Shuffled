using UnityEngine;
using UnityEngine.UI;

public class DoBuyCardUpgrade : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
           CardUpgradeHandler.current.DoUpgrade(); 
        });
    }
}
