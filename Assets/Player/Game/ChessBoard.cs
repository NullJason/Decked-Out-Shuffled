// ChessBoard.cs
using System.Collections.Generic;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Board Settings")]
    public float squareSize = 1f;
    public Material lightSquareMaterial;
    public Material darkSquareMaterial;
    public GameObject squareHighlightPrefab;
    
    private GameObject[,] squares = new GameObject[8, 8];
    private GameObject[] highlights = new GameObject[64];
    
    void Start()
    {
        GenerateBoard();
    }
    
    private void GenerateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // Create square
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Cube);
                square.transform.position = GetWorldPosition(x, y);
                square.transform.localScale = new Vector3(squareSize, 0.1f, squareSize);
                
                // Set material
                Renderer renderer = square.GetComponent<Renderer>();
                if ((x + y) % 2 == 0)
                    renderer.material = lightSquareMaterial;
                else
                    renderer.material = darkSquareMaterial;
                    
                squares[x, y] = square;
                
                // Create highlight object
                GameObject highlight = Instantiate(squareHighlightPrefab, GetWorldPosition(x, y), Quaternion.identity);
                highlight.SetActive(false);
                highlights[x * 8 + y] = highlight;
            }
        }
    }
    
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            x * squareSize - (3.5f * squareSize),
            0,
            y * squareSize - (3.5f * squareSize)
        );
    }
    
    public void HighlightSquares(List<Vector2Int> positions)
    {
        // Clear all highlights
        ClearHighlights();
        
        // Show new highlights
        foreach (Vector2Int pos in positions)
        {
            int index = pos.x * 8 + pos.y;
            if (index >= 0 && index < highlights.Length)
            {
                highlights[index].SetActive(true);
            }
        }
    }
    
    public void ClearHighlights()
    {
        foreach (GameObject highlight in highlights)
        {
            if (highlight != null)
                highlight.SetActive(false);
        }
    }
}