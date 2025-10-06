using UnityEngine;
using System.Collections.Generic;

//A UserInput is a system designed to get user input. 
//TODO: Test
//TODO: Make abstract parent class Input, and sibling class WeightedInput (for ai behaviour). 
//
//On creation, a Selectable announces itself to a UserInput, where its reference is stored in a HashSet. 
//
//When some external class wants the user to select one or more objects (for example, which card to discard from a player's hand), the external class calls WaitForNewInput(selectables). 
//This method makes the UserInput tell the appropriate Selectables to be ready for user interaction, i.e. clickable. (In the previous example, a HashSet containing the cards in the hand would be passed as the parameter, and those cards-- and only those cards --would become clickable.) 
//
//The Selectables that are clicked by the player are recorded in FIFO order, and can be gotten by calling GetNext(). If no cards have been selected, GetNext() will return null. 
//(To complete the example, the external class would call GetNext() to get the card, and then move it to the discard pile using relevant card/deck methods. It might also call WaitForNewInput again with an empty set, to prevent any new input from being collected.)
public class UserInput : MonoBehaviour
{
  public static UserInput main;

  private void Awake(){
    selected = new Queue<Selectable>();
    selectables = new HashSet<Selectable>();
    everything = new HashSet<Selectable>();

    if(main == null) main = this;
  }

  Queue<Selectable> selected;
  HashSet<Selectable> selectables;

  HashSet<Selectable> everything;
  
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

  public void Try(Selectable s){
    if(!selectables.Contains(s)) Debug.LogError("Could not select Selectable " + s.gameObject + " because it was not in the selectable set!");
    if(!everything.Contains(s)) Debug.LogError("Could not select Selectable " + s.gameObject + " because it was not in the everything set!");
    selected.Enqueue(s);
  }
}
