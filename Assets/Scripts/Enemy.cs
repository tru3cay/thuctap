using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float health = 10f;
    [SerializeField] float recoilLength = 1f;
    [SerializeField] float recoilFactor = 20f;
    [SerializeField] bool isRecoiling = false;

    float recoilTimer;
    Rigidbody2D rb;

    void Start()
    {
        
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(health <= 0)
        {
            Destroy(gameObject);
        }

        if(isRecoiling)
        {
            if(recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
    }

    public void EnemyHit(float _dmgDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _dmgDone;
        if (!isRecoiling)
        {
            rb.AddForce(- _hitForce * recoilFactor * _hitDirection);
        }
    }
}
