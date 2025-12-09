using UnityEngine;

public class TransportOnTrigger : MonoBehaviour
{
    [SerializeField] string LevelID = "Void";
    [SerializeField] string PathID = "EntisShop";
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.TryGetComponent<Player>(out Player plr)) SceneTransition.LoadEnvironmentLevel(LevelID,PathID);
    }
}
