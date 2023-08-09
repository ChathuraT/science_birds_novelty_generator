using UnityEngine;
using System.Collections;

public class ABTNTNoPlatform : ABGameObject {

	public float _explosionArea = 1f;
	public float _explosionPower = 1f;
	public float _explosionDamage = 1f;
	private bool _exploded = false;

	public override void Die(bool withEffect = true)
	{		
		//ScoreHud.Instance.SpawnScorePoint(200, transform.position);
		if (!_exploded) {
			_exploded = true;
			Explode (transform.position, _explosionArea, _explosionPower, _explosionDamage, gameObject);
		}

		base.Die (withEffect);
	}

	public static void Explode(Vector2 position, float explosionArea, float explosionPower, float explosionDamage, GameObject explosive)
	{
		int layerMask = 1 << LayerMask.NameToLayer("Platforms");
		bool hitPlatform;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(position, explosionArea);

		foreach (Collider2D coll in colliders)
		{
			hitPlatform = Physics2D.Linecast(position, coll.transform.position, layerMask);

			if (coll.attachedRigidbody && coll.gameObject != explosive && coll.GetComponent<ABBird>() == null && !hitPlatform)
			{

				float distance = Vector2.Distance((Vector2)coll.transform.position, position);
				Vector2 direction = ((Vector2)coll.transform.position - position).normalized;

				ABGameObject abGameObj = coll.gameObject.GetComponent<ABGameObject>();
				if (abGameObj)
					coll.gameObject.GetComponent<ABGameObject>().DealDamage(explosionDamage / distance);

				coll.attachedRigidbody.AddForce(direction * (explosionPower / (distance * 2f)), ForceMode2D.Impulse);
			}
		}

	}
}
