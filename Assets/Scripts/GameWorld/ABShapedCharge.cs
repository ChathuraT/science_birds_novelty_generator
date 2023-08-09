using UnityEngine;
using System.Collections;

public class ABShapedCharge : ABGameObject {

	public float _explosionArea = 1f;
	public float _explosionPower = 1f;
	public float _explosionDamage = 1f;
	private bool _exploded = false;


	public override void OnCollisionEnter2D(Collision2D collision)
	{
		Collider2D other = collision.collider;
		Vector2 inDirection = (other.transform.position - transform.position);
		inDirection.Normalize();
		DealDamage(collision.relativeVelocity.magnitude, inDirection);
	}

	public void DealDamage(float damage, Vector2 inDirection)
	{

		_currentLife -= damage;

		//if (_currentLife <= (_life / _sprites.Length * (_sprites.Length - _spriteChangedTimes + 1)))
		if (_currentLife <= _life - (_life / (_sprites.Length + 1)) * (_spriteChangedTimes + 1))
		{
			if (_spriteChangedTimes < _sprites.Length)
				_spriteRenderer.sprite = _sprites[_spriteChangedTimes];

			//if(!ABGameWorld.Instance._isSimulation)
			//_audioSource.PlayOneShot(_clips[(int)OBJECTS_SFX.DAMAGE]);

			_spriteChangedTimes++;
		}

		if (_currentLife <= 0f)
			Die(inDirection);

	}


	public void Die(Vector2 inDirection, bool withEffect = true)
	{		
		//ScoreHud.Instance.SpawnScorePoint(200, transform.position);
		if (!_exploded) {
			_exploded = true;
			Explode(transform.position, _explosionArea, _explosionPower, _explosionDamage, gameObject, inDirection);
		}

		base.Die (withEffect);
	}

	public static void Explode(Vector2 position, float explosionArea, float explosionPower, float explosionDamage, GameObject explosive, Vector2 inDirection) {

		Collider2D[] colliders = Physics2D.OverlapCircleAll (position, explosionArea);

		foreach (Collider2D coll in colliders) {
			if (coll.attachedRigidbody && coll.gameObject != explosive && coll.GetComponent<ABBird>() == null) {

				float distance = Vector2.Distance ((Vector2)coll.transform.position, position);
				Vector2 outDirection = ((Vector2)coll.transform.position - position).normalized;
				if (Mathf.Abs(Vector2.Angle(-1.0f * inDirection, outDirection)) < 22.5f)
				{
					ABGameObject abGameObj = coll.gameObject.GetComponent<ABGameObject>();
					if (abGameObj)
						coll.gameObject.GetComponent<ABGameObject>().DealDamage(explosionDamage / distance);

					coll.attachedRigidbody.AddForce(outDirection * (explosionPower / (distance * 2f)), ForceMode2D.Impulse);
				}
			}
		}

	}
}
