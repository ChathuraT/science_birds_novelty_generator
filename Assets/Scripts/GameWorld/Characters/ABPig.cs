// SCIENCE BIRDS: A clone version of the Angry Birds game used for 
// research purposes
// 
// Copyright (C) 2016 - Lucas N. Ferreira - lucasnfe@gmail.com
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using UnityEngine;
using System.Collections;

public class ABPig : ABCharacter {

	public override void Die(bool withEffect = true)
	{
		ScoreHud.Instance.SpawnScorePoint(5000, transform.position);
		ABGameWorld.Instance.KillPig(this);

		base.Die(withEffect);
	}
	public override void OnCollisionEnter2D(Collision2D collision)
	{
		float collisionMagnitude = collision.relativeVelocity.magnitude;

		if (collision.gameObject.tag == "Bird")
		{
			// spawn the points for colliding with the bird - give points if it only exeeds 10 and not killed
			int points = (int)System.Math.Round(collisionMagnitude * 10)*10;
			if ((points > 10) & ((base.getCurrentLife() - collisionMagnitude) > 0f))
				ScoreHud.Instance.SpawnScorePoint(points, transform.position);
		}

		// kill the pig with a single collision (only for the collisions with birds and blocks)
		// base.OnCollisionEnter2D(collision);

		if (collision.gameObject.layer == 8 | collision.gameObject.layer == 10) //layer 8 is birds and 10 is blocks
		{
			Die(true);
		}

	}

}
