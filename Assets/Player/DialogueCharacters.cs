using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class DialogueCharacters:MonoBehaviour
{
    // id based on index
    public Sprite[] CharacterHeadshotImage;

    public Sprite GetImage(int id)
    {
        return CharacterHeadshotImage[id];
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        LoadImages(true);
    }
    /// <summary>
    /// Loads all images under Resources.CharacterHeadShots and applies it to CharacterHeadshotImage list, indexing alphabetically.
    /// 
    /// debugPrint prints all index & image names if true, when done loading.
    /// </summary>
    public void LoadImages(bool debugPrint = false)
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("CharacterHeadShots");

        loadedSprites = loadedSprites.OrderBy(s => s.name).ToArray();

        if (CharacterHeadshotImage == null || CharacterHeadshotImage.Length != loadedSprites.Length)
            CharacterHeadshotImage = new Sprite[loadedSprites.Length];

        for (int i = 0; i < loadedSprites.Length; i++)
        {
            CharacterHeadshotImage[i] = loadedSprites[i];
            if (debugPrint)
                Debug.Log($"[{i}] {loadedSprites[i].name}");
        }

        if (debugPrint)
            Debug.Log($"Loaded {loadedSprites.Length} character headshots.");
    }

}
