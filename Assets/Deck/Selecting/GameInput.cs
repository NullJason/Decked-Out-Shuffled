using UnityEngine;
using System.Collections.Generic;

public class GameInput : MonoBehaviour
{
  Queue<Selectable> selected;
  HashSet<Selectable> selectables;
  HashSet<Selectable> everything;

  private protected virtual void Awake(){
    selected = new Queue<Selectable>();
    selectables = new HashSet<Selectable>();
    if(everything == null) everything = new HashSet<Selectable>();
  }

  public void WaitForNewInput(HashSet<Selectable> selectables)
  {
    selected.Clear();
    this.selectables = selectables;
    HighlightSelectable();
  }

  public void Clear(){
    WaitForNewInput(new HashSet<Selectable>());
  }

  public Selectable GetNext(){
    if(selected.Count > 0) return selected.Dequeue();
    return null;
  }

  public void Add(Selectable s){
    everything.Add(s);
  }

  private void HighlightSelectable(){
    foreach(Selectable s in everything) s.Unhighlight();
    foreach(Selectable s in selectables) s.Highlight();
  }

  public virtual void Try(Selectable s){
    TrySelect(s);
  }

  private protected void TrySelect(Selectable s) {
    if(!selectables.Contains(s)) Debug.LogError("Could not select Selectable " + s.gameObject + " because it was not in the selectable set!");
    if(!everything.Contains(s)) Debug.LogError("Could not select Selectable " + s.gameObject + " because it was not in the everything set!");
    selected.Enqueue(s);
  }
}
