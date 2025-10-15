using UnityEngine;
using System.Collections.Generic;

public abstract class DeckPreset : MonoBehaviour
{
	public abstract void Apply(List<Card> cards, Deck deck);
}
