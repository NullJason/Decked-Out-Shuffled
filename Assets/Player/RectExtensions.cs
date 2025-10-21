using UnityEngine;

public static class RectExtensions
{
    public static bool ProcessEvents(this Rect rect, Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (rect.Contains(e.mousePosition))
                {
                    return true;
                }
                break;
                
            case EventType.MouseDrag:
                if (rect.Contains(e.mousePosition))
                {
                    rect.position += e.delta;
                    return true;
                }
                break;
        }
        return false;
    }
}