using UnityEngine;

public class TestUtil : MonoBehaviour
{
	[SerializeField]
	private Rigidbody2D rb;
	[SerializeField]
	private Collider2D col;
	[SerializeField]
	private SpriteRenderer rend;
	public void Start(){
		rb = Util.NullCheck<Rigidbody2D>(rb, gameObject);
		col = Util.NullCheck<Collider2D>(col, gameObject);
		rend = Util.NullCheck<SpriteRenderer>(rend, gameObject);
	}
}
