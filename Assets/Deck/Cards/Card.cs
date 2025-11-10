using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public abstract class Card : ClickSelectable
{
	[SerializeField] private protected Image sprite;
	[SerializeField] private protected Sprite faceUpSprite;
	[SerializeField] private protected Sprite faceDownSprite;
	[SerializeField] private protected Sprite hiddenCard;
	[SerializeField] private protected List<string> attributes;

	private protected void Awake(){
		sprite = Util.NullCheck(sprite, gameObject);
	}

	private Face facing;

	public abstract void Effect();

	public void FaceFlip(Face face){
		facing = face;
		UpdateFace();
	}

	public void FaceFlip(){
		if(facing == Face.up) FaceFlip(Face.down);
		else if(facing == Face.down) FaceFlip(Face.up);
	}

	public enum Face{
		up, 
		down,
		hidden
	}

	private void UpdateFace(){
		if(facing == Face.up) sprite.sprite = faceUpSprite;
		else if(facing == Face.down) sprite.sprite = faceDownSprite;
		else sprite.sprite = hiddenCard;
	}
}
