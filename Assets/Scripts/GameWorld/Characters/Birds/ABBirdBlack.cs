using UnityEngine;
using System.Collections;

public class ABBirdBlack : ABBird {

	public float _explosionArea = 1f;
	public float _explosionPower = 1f;
	public float _explosionDamage = 1f;

	void SpecialAttack() {

		Explode ();
	}

	// Called via frame event
	void Explode() {

		ABTNT.Explode (transform.position, _explosionArea, _explosionPower, _explosionDamage, gameObject);
		Die (true);
	}
    IEnumerator WaitAndExplode()
    {
        // suspend execution for 50*Time.fixedDeltaTime
        yield return new WaitForSeconds(Time.fixedDeltaTime*50/ABGameWorld.SimulationSpeed);
        Explode();
    }
    public override void OnCollisionEnter2D(Collision2D collision) {
		ABGameWorld.displayTrajectory = false;
		IsCollided = true;
		if(_trailParticles!=null){
			_trailParticles._shootParticles = false;
		}
        int startSimID = ABGameWorld.simulationID;
        if (OutOfSlingShot) {
            StartCoroutine("WaitAndExplode");
        }
        //animator is not used as the play time is not sequenized with the physical simulation
        //        _animator.Play ("explode");
    }
}
