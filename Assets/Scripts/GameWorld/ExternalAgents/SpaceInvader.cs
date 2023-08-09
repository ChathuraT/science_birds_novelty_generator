using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceInvader : ABGameObject
{
    private float lastStep = 0.0f;
    public float moveInterval;
    public float speed;
    public float vertSpeed;
    public Vector3 direction;
    public GameObject laserPrefab;
    public float xMin;
    public float xMax;

    Animator m_Animator;
    public bool moveState = false;

    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        m_Animator = gameObject.GetComponent<Animator>();
        direction = Vector3.right;
        xMin = transform.position.x;
        xMax = transform.position.x + 3;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float currentTime = Time.time; 
        if (currentTime - lastStep > moveInterval)
        {
            if ((transform.position.x >= xMax) && (direction == Vector3.right))
            {
                transform.Translate(Vector3.down * vertSpeed);
                direction = Vector3.left;
            }
            else if ((transform.position.x <= xMin) && (direction == Vector3.left))
            {
                transform.Translate(Vector3.down * vertSpeed);
                direction = Vector3.right;
            }

            else
            {
                transform.Translate(direction * speed);
            }
            toggleMoveState();
            lastStep = currentTime;
            Instantiate(laserPrefab, transform.position + Vector3.down * 0.75f, transform.rotation);
        }
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        DealDamage(_life);
    }

    private void toggleMoveState()
    {
        moveState ^= true;
        m_Animator.SetBool("moveState", moveState);
    }

}
