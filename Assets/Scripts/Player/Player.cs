using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{

    public int _direction = 1;
    public float jumpHeight = 4;
    public float timeToJumpApex = 0.4f;
    public float moveSpeed = 6;
    public float dashSpeed = 18;
    public float defaultSpeed = 6;
    public float dashCoolTime = 0.09f;
    public float _lastInputX;
    public float wallCheckDistance = 0.64f;
    public float maxHP;
    public bool isHit, isCombo;
    public AttackMode _currentAttack;
    public GameObject E;
    public GameObject textBox;
    public GameObject gameOver;
    public GameObject[] attackPrefabs;
    public Transform[] wallRayCheckTfs;
    public AudioClip[] clip;
    public Animator animator;
    public SpriteFlipper flipper;
    public PlayerAttachedCamera playerAttached;
    public SpriteRenderer sr;

    private static readonly int AnimIsBackStep = Animator.StringToHash("isBackStep");
    private static readonly int AnimIsDash = Animator.StringToHash("isDash");
    private static readonly int AnimIsJump = Animator.StringToHash("isJump");
    private static readonly int AnimDJump = Animator.StringToHash("dJump");
    private static readonly int AnimIsRun = Animator.StringToHash("isRun");
    private static readonly int AnimIsAttack = Animator.StringToHash("isAttack");
    private static readonly int AnimIsAttack2 = Animator.StringToHash("isAttack2");
    private static readonly int AnimIsAttack3 = Animator.StringToHash("isAttack3");
    private static readonly int AnimIsWall = Animator.StringToHash("isWall");
    private static readonly int AnimIsDiy = Animator.StringToHash("isDie");
    private static readonly int AnimShoot = Animator.StringToHash("isShoot");
    private static readonly int AnimShoot2 = Animator.StringToHash("isShoot2");

    private AudioSource audio;

    private int _attackMode;
    private float accelerationTimeAirborne = 0.2f;
    private float accelerationTimeGrounded = 0.1f;
    private float _gravity;
    private float _jumpVelocity;
    private float _velocityXSmoothing;
    private float _sinceLastDashTime = 10f;
    private float _comboTime;
    public float _HP;
    private bool _isDash, _isAttack, _isWall, _isTalk, _isTalking, _isSave, _isDoor, _isDoAttack, _isAttackYet;
    private Vector2 _input;
    private Vector3 _velocity;
    private Vector3 _doorPos;
    private JumpMode _currentJump;
    private Hit _hit;
    private Controller2D _controller;
    private GameObject hpBar;
    private Image _hpBar;
    private Sine sine;
    private Doer door;
    private Egg egg;


    private enum JumpMode
    {
        None,
        Normal,
        Double
    }

    public enum AttackMode
    {
        None,
        First,
        Second,
        Third,
        FirstShoot,
        SecondShoot
    }

    private void Start()
    {
        _isAttackYet = true;
        Application.targetFrameRate = 60;
        _controller = GetComponent<Controller2D>();

        defaultSpeed = moveSpeed;
        _lastInputX = 1;

        _gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2); // f??p??
        _jumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
        print("Gravity: " + _gravity + " Jump Velocity: " + _jumpVelocity);


        hpBar = GameObject.Find("PlayerHp");
        _hpBar = hpBar.GetComponent<Image>();
        _HP = maxHP;

        _hit = GetComponent<Hit>();
        isHit = true;

        E.SetActive(false);

        switch (GameManager.Instance.savePoint)
        {

            case 0:
                transform.position = new Vector3(0.5f, -6.07f, 0);
                Time.timeScale = 1;
                break;
            case 1:
                transform.position = new Vector3(89.41f, 2.95f, 0);
                Time.timeScale = 1;
                break;
        }
        playerAttached.isIn = true;
        gameOver.SetActive(false);
        egg = FindObjectOfType<Egg>();
        audio = GetComponent<AudioSource>();
    }

    //IEnumerator ComboAttack(AttackMode attackMode)
    //{
    //    yield return YieldlnstructionCache.WaitForSeconds(0.5f);
    //    if (_currentAttack==attackMode)
    //    {
    //        Debug.Log("attack");
    //        _isCombo = false;
    //        _currentAttack = AttackMode.None;
    //    }
    //}

    private void Update()
    {
        _sinceLastDashTime += Time.deltaTime;


        if (_controller.collisions.above || _controller.collisions.below)
        {
            _velocity.y = 0;
        }

        if (_controller.collisions.below)
        {
            _currentJump = JumpMode.None;
            animator.SetBool(AnimIsJump, false);
            animator.SetBool(AnimDJump, false);
        }

        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (!_isDash)
        {
            WallCheck();
            if (!_isAttack)
            {
                if (!_isWall)
                {
                    if (dashCoolTime < _sinceLastDashTime)
                    {
                        if (_input.x == 0)
                        {
                            BackStep();
                        }
                        else
                        {
                            Dash();
                        }
                    }
                }
            }
            
        }

        float targetVelocityX = _input.x * moveSpeed;

        if (_isDash)
        {
            targetVelocityX = _direction * moveSpeed;
        }
        else
        {
            if (!_isAttack && _input.x != 0) _lastInputX = _input.x;
        }

        if (!_isAttack) Jump();

        var gravityMultiplier = 1f;

        if (!_isWall) Attack();
        else gravityMultiplier = 0.4f;

        if (_isAttack) targetVelocityX = 0;

        _velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing,
            _controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne);
        _velocity.y += _gravity * Time.deltaTime * gravityMultiplier;

        flipper.Flip(_lastInputX);

        if (!_isDash)
        {
            if (targetVelocityX < 0 || targetVelocityX > 0)
            {
                animator.SetBool(AnimIsRun, true);

            }
            else if (targetVelocityX == 0)
            {
                animator.SetBool(AnimIsRun, false);
            }
        }

        _controller.Move(_velocity * Time.deltaTime);

        //if (_isTalk)
        //{
        //    if (Input.GetKeyDown(KeyCode.E))
        //    {
        //        if (!_isTalking)
        //        {
        //            _isTalking = true;
        //            E.SetActive(false);
        //            sine.TalkStart();
        //        }
        //        else
        //        {
        //            sine.NextText();
        //        }
        //    }
        //}
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_isTalk)
            {

                if (!_isTalking)
                {
                    Debug.Log(_isTalking);
                    _isTalking = true;
                    E.SetActive(false);
                    sine.TalkStart();
                }
                else
                {
                    sine.NextText();
                }

            }
            if (_isSave)
            {
                GameManager.Instance.savePoint = 1;
                GameManager.Instance.GameSave();
                StartCoroutine(_hit.Save());
            }
            if (_isDoor)
            {
                transform.position = _doorPos;
                _isDoor = false;
                if (playerAttached.isIn)
                {
                    playerAttached.isIn = false;
                }
                else
                {
                    playerAttached.isIn = true;
                }
                E.SetActive(false);
                door = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SceneManager.LoadScene("Title");

        }
        if (isCombo)
        {
            if (!_isAttack)
            {
                if (_isDoAttack)
                {
                    DoAttack();
                }
            }
        }

        
    }

    private void WallCheck()
    {
        // 체크 용도
        Debug.DrawRay(wallRayCheckTfs[0].position, Vector3.right * (_lastInputX * wallCheckDistance), Color.red, 0,
            false);
        Debug.DrawRay(wallRayCheckTfs[1].position, Vector3.right * (_lastInputX * wallCheckDistance), Color.red, 0,
            false);

        // 레이어 마스크
        var mask = 1 << LayerMask.NameToLayer("WorldGround");

        // 위 아레로 플레이어 보는 방향으로 레이 쏘기
        var wallCheckRays = new RaycastHit2D[2];
        wallCheckRays[0] =
            Physics2D.Raycast(wallRayCheckTfs[0].position, Vector3.right * (_lastInputX * wallCheckDistance),
                wallCheckDistance, mask);
        wallCheckRays[1] =
            Physics2D.Raycast(wallRayCheckTfs[1].position, Vector3.right * (_lastInputX * wallCheckDistance),
                wallCheckDistance, mask);

        // 값 확인
        var checkWallResult = new bool[2];
        if (wallCheckRays[0].transform) checkWallResult[0] = true;
        if (wallCheckRays[1].transform) checkWallResult[1] = true;

        // 벽인가?
        if (checkWallResult[0] && checkWallResult[1])
        {
            if (!_isWall)
            {
                _isWall = true;
                _isDash = false;
                _currentJump = JumpMode.None;
                animator.SetBool(AnimIsJump, false);
                animator.SetBool(AnimDJump, false);

                _velocity.y = 0;
            }
        }
        else
        {
            _isWall = false;
        }

        animator.SetBool(AnimIsWall, _isWall);

    }

    private void DoAttack()
    {
        switch ((int)_currentAttack)
        {
            case 0:
                break;
            case 1:
                _isAttack = true;
                animator.SetBool(AnimIsAttack, true);
                break;
            case 2:
                _isAttack = true;
                animator.SetBool(AnimIsAttack2, true);
                break;
            case 3:
                _isAttack = true;
                animator.SetBool(AnimIsAttack3, true);
                break;
            case 4:
                _isAttack = true;
                animator.SetBool(AnimShoot, true);
                break;
            case 5:
                _isAttack = true;
                animator.SetBool(AnimShoot2, true);
                break;
        }
    }
    private void Attack()
    {
        if (_isAttackYet)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_currentAttack == AttackMode.Second || _currentAttack == AttackMode.SecondShoot)
                {
                    _currentAttack = AttackMode.Third;
                    _isAttackYet = false;
                }

                else if (_currentAttack == AttackMode.First || _currentAttack == AttackMode.FirstShoot)
                {
                    _currentAttack = AttackMode.Second;
                    _isAttackYet = false;
                }
                else if (_currentAttack == AttackMode.None)
                {
                    _isAttack = true;
                    animator.SetBool(AnimIsAttack, true);
                    _currentAttack = AttackMode.First;
                }
                _isDoAttack = true;
            }

            else if (Input.GetMouseButtonDown(1))
            {
                if (_currentAttack == AttackMode.None)
                {
                    _isAttack = true;
                    animator.SetBool(AnimShoot, true);
                    _currentAttack = AttackMode.FirstShoot;
                }

                else if (_currentAttack == AttackMode.FirstShoot || _currentAttack == AttackMode.First)
                {
                    _currentAttack = AttackMode.SecondShoot;
                    _isAttackYet = false;
                }
                _isDoAttack = true;
            }
        }
        
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            switch (_currentJump)
            {
                case JumpMode.None:
                    {
                        audio.clip = clip[0];
                        audio.Play();
                        switch (_isWall)
                        {
                            case true when _controller.collisions.below:
                                _velocity.y = _jumpVelocity;
                                _velocity.x = -20 * _lastInputX;
                                _currentJump = JumpMode.Normal;
                                animator.SetBool(AnimIsJump, true);
                                _lastInputX *= -1;
                                break;
                            case false when _controller.collisions.below:
                                _velocity.y = _jumpVelocity;
                                _currentJump = JumpMode.Normal;
                                animator.SetBool(AnimIsJump, true);
                                break;
                            case false when !_controller.collisions.below:
                                _velocity.y = _jumpVelocity;
                                _currentJump = JumpMode.Double;
                                animator.SetBool(AnimDJump, true);
                                break;
                            case true when !_controller.collisions.below:
                                _velocity.y = _jumpVelocity;
                                _velocity.x = -20 * _lastInputX;
                                _currentJump = JumpMode.Normal;
                                animator.SetBool(AnimIsJump, true);
                                _lastInputX *= -1;
                                break;
                        }
                        break;
                    }
                case JumpMode.Normal:
                    {
                        _velocity.y = _jumpVelocity;
                        _currentJump = JumpMode.Double;
                        animator.SetBool(AnimDJump, true);
                        break;
                    }
            }
        }
    }

    private void BackStep()
    {
        if (_currentJump == JumpMode.None && !_isDash)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && _controller.collisions.below)
            {
                audio.PlayOneShot(clip[3]);
                animator.SetBool(AnimIsBackStep, true);
                _direction = -(int)_lastInputX;
                moveSpeed = dashSpeed;
                _isDash = true;
                isHit = false;
            }
        }
    }

    private void Dash()
    {
        if (_currentJump == JumpMode.None && !_isDash)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && _controller.collisions.below)
            {
                audio.PlayOneShot(clip[3]);
                animator.SetBool(AnimIsDash, true);
                _direction = (int)_lastInputX;
                moveSpeed = dashSpeed;
                _isDash = true;
                isHit = false;
            }
        }
    }

    public void OnAnimationDashEnd()
    {
        _sinceLastDashTime = 0;
        animator.SetBool(AnimIsDash, false);
        _isDash = false;
        isHit = true;
        moveSpeed = defaultSpeed;
        //dTime = 0;
    }

    public void OnAnimationBackStepEnd()
    {
        _sinceLastDashTime = 0;
        animator.SetBool(AnimIsBackStep, false);
        _isDash = false;
        moveSpeed = defaultSpeed;
        //dTime = 0;
    }

    public void OnAnimationAttackFx(AttackMode attackMode)
    {
        var thisTransform = transform;

        if (attackMode == AttackMode.First)
        {
            audio.PlayOneShot(clip[1]);

            if (!sr.flipX)
            {
                Instantiate(attackPrefabs[0], thisTransform.position, thisTransform.rotation);
            }
            else
            {
                Instantiate(attackPrefabs[0], thisTransform.position, Quaternion.Euler(0,-180,0));
            }
        }

        if (attackMode == AttackMode.Second)
        {
            audio.PlayOneShot(clip[1]);

            if (!sr.flipX)
            {
                Instantiate(attackPrefabs[1], thisTransform.position, thisTransform.rotation);
            }
            else
            {
                Instantiate(attackPrefabs[1], thisTransform.position, Quaternion.Euler(0, -180, 0));
            }
        }

        if (attackMode == AttackMode.Third)
        {
            audio.PlayOneShot(clip[1]);

            if (!sr.flipX)
            {
                Instantiate(attackPrefabs[2], thisTransform.position, thisTransform.rotation);
            }
            else
            {
                Instantiate(attackPrefabs[2], thisTransform.position, Quaternion.Euler(0, -180, 0));
            }
        }
        if (attackMode == AttackMode.FirstShoot || attackMode == AttackMode.SecondShoot)
        {
            audio.PlayOneShot(clip[2]);
            if (!sr.flipX)
            {
                Instantiate(attackPrefabs[3], thisTransform.position, thisTransform.rotation);
            }
            else
            {
                Instantiate(attackPrefabs[3], thisTransform.position, Quaternion.Euler(0, -180, 0));
            }
        }
    }

    public void OnAnimationAttackEnd(AttackMode attackMode)
    {
        _isAttackYet = true;
        if (attackMode == AttackMode.First)
        {
            animator.SetBool(AnimIsAttack, false);
            _isAttack = false;
        }

        if (attackMode == AttackMode.Second)
        {
            animator.SetBool(AnimIsAttack2, false);
            _isAttack = false;
        }

        if (attackMode == AttackMode.Third)
        {
            _isAttack = false;
            animator.SetBool(AnimIsAttack3, false);
            _currentAttack = AttackMode.None;
        }

        if (attackMode == AttackMode.FirstShoot)
        {
            _isAttack = false;
            animator.SetBool(AnimShoot, false);
        }

        if (attackMode == AttackMode.SecondShoot)
        {
            _isAttack = false;
            animator.SetBool(AnimShoot2, false);
        }
        if (_currentAttack == attackMode)
        {
            _isDoAttack = false;
            Debug.Log(_currentAttack);
        }
        isCombo = true;
        StartCoroutine(_hit.AttackWait(attackMode));
    }

    public void IsDie()
    {
        GameManager.instance.GameLoad();
        Time.timeScale = 0;
        gameOver.SetActive(true);
        egg.PlayerDie();
        switch (GameManager.Instance.savePoint)
        {
            case 0:
                transform.position = new Vector3(0, -1.5f, 0);
                break;
            case 1:
                transform.position = new Vector3(86, 8.25f, 0);
                break;
        }
    }

    public void UP()
    {
        _HP = maxHP;
        _hpBar.fillAmount = _HP / maxHP;
        gameOver.SetActive(false);
        animator.SetBool(AnimIsDiy, false);
        Time.timeScale = 1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnemyAttack"))
        {
            Damage damage = collision.GetComponent<Damage>();
            if (isHit)
            {
                _HP -= damage.dmg;
                _hpBar.fillAmount = _HP / maxHP;
                if (_HP <= 0)
                {
                    animator.SetBool(AnimIsDiy, true);
                }
                StartCoroutine(_hit.HitAni());
            }
        }
        if (collision.CompareTag("Sine"))
        {
            _isTalk = true;
            E.SetActive(true);
            sine = collision.GetComponent<Sine>();
        }
        if (collision.CompareTag("SavePoint"))
        {
            _isSave = true;
            E.SetActive(true);
        }
        if (collision.CompareTag("Door"))
        {
            door = collision.GetComponent<Doer>();
            _doorPos = door.monePosition;
            _isDoor = true;
            E.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Sine"))
        {
            _isTalk = false;
            _isTalking = false;
            E.SetActive(false);
            sine.TextEnd();
        }
        if (other.CompareTag("SavePoint"))
        {
            _isSave = false;
            E.SetActive(false);
            
        }
        if (other.CompareTag("Door"))
        {
            _isDoor = false;
            E.SetActive(false);
            door = null;
        }
    }

}