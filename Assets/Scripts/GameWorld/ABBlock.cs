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

public class ABBlock : ABGameObject
{

    public MATERIALS _material;
    public string originMaterial;

	public int _damage;

	public int  _points;

    public Sprite[] _woodSprites;
    public Sprite[] _stoneSprites;
    public Sprite[] _iceSprites;

    protected override void Awake()
    {

        base.Awake();
        SetMaterial(_material);
    }

    public override void Die(bool withEffect = true)
    {
        if (!ABGameWorld.Instance._isSimulation)
            ScoreHud.Instance.SpawnScorePoint(_points, transform.position);

        base.Die();
    }

    public void SetMaterial(MATERIALS material)
    {

        _material = material;

        switch (material)
        {

            case MATERIALS.wood:
                _sprites = _woodSprites;
                _destroyEffect._particleSprites = ABWorldAssets.WOOD_DESTRUCTION_EFFECT;
                _collider.sharedMaterial = ABWorldAssets.WOOD_MATERIAL;

                //base.getRigidBody().mass = base.getRigidBody().mass * 0.375f;
                base.getRigidBody().mass = base.getRigidBody().mass * 0.05f;
                _life *= 0.75f;
                _spriteRenderer.sprite = _sprites[0];
                break;

            case MATERIALS.stone:
                _sprites = _stoneSprites;
                _destroyEffect._particleSprites = ABWorldAssets.STONE_DESTRUCTION_EFFECT;
                _collider.sharedMaterial = ABWorldAssets.STONE_MATERIAL;

                base.getRigidBody().mass = base.getRigidBody().mass * 1f;
                _life *= 1.25f;
                _spriteRenderer.sprite = _sprites[0];
                break;

            case MATERIALS.ice:
                _sprites = _iceSprites;
                _destroyEffect._particleSprites = ABWorldAssets.ICE_DESTRUCTION_EFFECT;
                _collider.sharedMaterial = ABWorldAssets.ICE_MATERIAL;

                base.getRigidBody().mass = base.getRigidBody().mass * 0.188f;
                _life *= 0.4f;
                _spriteRenderer.sprite = _sprites[0];
                break;

            default:
                break;

        }
    }

    public void SetMaterial(MATERIALS material, string objMaterial)
    {

        _material = material;

        switch (material)
        {

            case MATERIALS.novelty:
                /*
				*leave some space for future develop
				*/
                _collider.sharedMaterial = (PhysicsMaterial2D)ABGameWorld.NOVELTIES.LoadAsset(objMaterial);

                break;

            default:
                Debug.Log("wrong material choice for novel objects");
                break;

        }


    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Bird")
        {

            ABBird bird = collision.gameObject.GetComponent<ABBird>();
            float collisionMagnitude = collision.relativeVelocity.magnitude;
            float birdDamage = 1f;

            switch (_material)
            {

                case MATERIALS.wood:
                    birdDamage = bird._woodDamage;
                    break;

                case MATERIALS.stone:
                    birdDamage = bird._stoneDamage;
                    break;

                case MATERIALS.ice:
                    birdDamage = bird._iceDamage;
                    break;

                case MATERIALS.novelty:
                    if (originMaterial == "wood")
                    {
                        birdDamage = bird._woodDamage;
                        //Debug.Log("origin material wood");
                    }
                    else if (originMaterial == "stone")
                    {
                        birdDamage = bird._stoneDamage;
                        //Debug.Log("origin material stone");

                    }
                    else if (originMaterial == "ice")
                    {
                        birdDamage = bird._iceDamage;
                        //Debug.Log("origin material ice");
                    }
                    else
                    {
                        birdDamage = bird._woodDamage;
                        //Debug.Log("origin material not found! setting wood damage");

                    }
                    break;
            }

            // spawn the points for colliding with the bird  - give points if it only exeeds 10 and block is not destroyed
            int points = (int)System.Math.Round(collisionMagnitude * birdDamage * 10) * 10;
            if ((points > 10) & ((base.getCurrentLife() - collisionMagnitude * birdDamage) > 0f))
                ScoreHud.Instance.SpawnScorePoint(points, transform.position);

            DealDamage(collisionMagnitude * birdDamage);
        }
        else
        {

            base.OnCollisionEnter2D(collision);
        }
    }
}
