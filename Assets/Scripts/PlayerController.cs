using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update

    [Header("Horizontal Movement Settings:")]
    [SerializeField] private float walkSpeed = 7f;
    
    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask ground;
    public static PlayerController Instance;
        
    [Header("Vertical Movement Settings")]
    [SerializeField] private float jumpForce = 20f;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames = 8;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime = 0.1f;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJump = 1;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 5f;
    [SerializeField] private float dashTime = 0.25f;
    [SerializeField] private float dashCooldown = 0.35f;
    [SerializeField] GameObject dashEffect;

    [Header("Attacking")]
    [SerializeField] Transform SideAttack, UpAttack, DownAttack;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage = 3;
    [SerializeField] GameObject slashEffect;
    private float timeBetweenAttack, timeSinceAttack;

    [Header("Recoil")]
    [SerializeField] int recoilXSteps = 5;
    //[SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 10;
    //[SerializeField] float recoilYSpeed = 50;
    int stepsXRecoiled;

    private Rigidbody2D rb;
    private float xAxis, yAxis;
    Animator anim;
    PlayerStateList pState;
    private float gravity;
    private bool canDash = true;
    private bool dashed;
    bool attack = false;



    private void Awake()
    {
        if(Instance != null && Instance != this) 
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        pState = GetComponent<PlayerStateList>();

        rb = GetComponent<Rigidbody2D>();

        anim = GetComponent<Animator>();

        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(SideAttack.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttack.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttack.position, DownAttackArea);
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        if (pState.dashing) return;
        Flip();
        Movement();
        Jump();
        StartDash();
        Attack();
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");

        attack = Input.GetMouseButtonDown(0);
    }

    void Flip()
    {
        if(xAxis < 0)
        {
            transform.localScale = new Vector2(-3, transform.localScale.y); //Tranform -> Scale
            pState.lookingRight = false;
        }
        else if(xAxis > 0)
        {
            transform.localScale = new Vector2(3, transform.localScale.y);
            pState.lookingRight = true;

        }
    }

    private void Movement()
    {
        rb.velocity = new Vector2 (walkSpeed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
    }


    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (Grounded())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if(attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            if (yAxis == 0 || yAxis < 0 &&Grounded())
            {
                Hit(SideAttack, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
                Instantiate(slashEffect, SideAttack);
            }
            //else if(yAxis > 0)
            //{
            //    Hit(UpAttack, UpAttackArea);
            //    SlashEffectAtAngle(slashEffect, 90, UpAttack, ref pState.recoilingY, recoilYSpeed);
            //}else if(yAxis < 0 && !Grounded())
            //{
            //    Hit(DownAttack, DownAttackArea);
            //    SlashEffectAtAngle(slashEffect, -90, DownAttack, ref pState.recoilingY, recoilYSpeed);
            //}
        }
    }

    void Hit(Transform _attackTranform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTranform.position, _attackArea, 0, attackableLayer);

        if(objectsToHit.Length > 0 )
        {
            _recoilDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            if (objectsToHit[i].GetComponent<Enemy>() != null) 
            {
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage, (transform.position -objectsToHit[i].transform.position).normalized, _recoilStrength);        
            }
        }
    }

    //void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTranform )
    //{
    //    _slashEffect = Instantiate(_slashEffect, _attackTranform);
    //    _slashEffect.transform.eulerAngles = new Vector3(0,0,_effectAngle);
    //    _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    //}

    void Recoil()
    {
        if(pState.recoilingX)
        {
            if(pState.lookingRight)
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        //if (pState.recoilingY)
        //{
        //    rb.gravityScale = 0;
        //    if (yAxis < 0)
        //    {
        //        rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
        //    }
        //    else
        //    {
        //        rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
        //    }
        //    airJumpCounter = 0;
        //}
        else
        {
            rb.gravityScale = gravity;
        }

        if(pState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }

    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }

    public bool Grounded()
    {
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, ground)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX,0,0), Vector2.down, groundCheckY, ground)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX,0,0), Vector2.down, groundCheckY, ground)
            )
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);

            pState.jumping = false;
        }


        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);

                pState.jumping = true;
            }
            else if(!Grounded() && airJumpCounter < maxAirJump && Input.GetButtonDown("Jump"))
            {
                pState.jumping = true;
                airJumpCounter++;
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariables()
    {
        if (Grounded())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter --;
        }
    }
}
