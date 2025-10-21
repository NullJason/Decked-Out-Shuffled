using UnityEngine;

public class ListPriority : MonoBehaviour
{
    [SerializeField] private int sortOrder = 0;
    public int SortOrder { get { return sortOrder; } set { sortOrder = value; } }
}