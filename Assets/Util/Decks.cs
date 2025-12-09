using UnityEngine;

public abstract class Decks 
{
	//https://discussions.unity.com/t/how-can-i-shuffle-a-list/75012/5
	public static Deck Shuffle(Deck d){
		for(int i = 0; i < d.Count(); i++){
			int r = i + (int)(Random.value * (d.Count() - i));
			d.SwapCards(r, i);
		}
		return d;
	}
}
