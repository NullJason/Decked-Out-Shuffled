using System.Collections.Generic;

[System.Serializable]
public class DialogueResponse
{
    public List<string> Dialogue = new List<string>();
    public List<int> OptionID = new List<int>();
    public List<int> ResponseIDs = new List<int>();
}
