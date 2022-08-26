using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Monster.Pig
{
    public class NewPigCtrl : Monster
    {
        private bool _isAttack, _attack4Waiting;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _aiMoveCoroutine;
        private IDisposable _moveStateSubscription;// _attack2WaitSubscription;
        //public float attack2Distance;
        public Transform directionalObjectGroup, smokingPoint;
        public LayerMask attackContactLayerMask;
        public GameObject smoke;

        public BoxCollider2D attackColliders;
        private ContactFilter2D _attackContactFilter;

        #region �ִϸ����� �ؽ�

        private static readonly int IsWalk = Animator.StringToHash("isWalk");
        
        #endregion

        protected override void Start()
        {
            base.Start();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            StartCoroutineWithRunningCheck(ref _aiMoveCoroutine, AIMove(1, 3, 1f, 2f));
            _moveStateSubscription = isMonsterMoving.DistinctUntilChanged()
                .Subscribe(v => { _animator.SetBool(IsWalk, v); }).AddTo(gameObject);
            _attackContactFilter = new ContactFilter2D
            {
                layerMask = attackContactLayerMask,
                useLayerMask = true
            };
        }

        private void OnDestroy()
        {
            GameUIManager.Instance.TryPopHpBar(GetInstanceID().ToString());
        }

        protected override void OnHpDrown()
        {
            _animator.Play("Die");
            _animator.Update(0);
            PlayerFoundSubscription.Dispose();
            //_moveStateSubscription.Dispose();
            //_attack2WaitSubscription?.Dispose();
        }

        public override void OnMonsterGetDamaged(int dmg)
        {
            if (hp.Value <= 0) return;
            base.OnMonsterGetDamaged(dmg);
            _spriteRenderer.material.color = Color.white;
            Observable.TimerFrame(1, FrameCountType.EndOfFrame)
                .Do(_ => { }, () => { _spriteRenderer.material.color = Color.black; }).Subscribe().AddTo(gameObject);
        }

        protected override void OnDirectionSet(int direction)
        {
            _spriteRenderer.flipX = direction == 1;
            directionalObjectGroup.localScale = new Vector3(-direction, directionalObjectGroup.localScale.y);
        }

        private void NormalAttack()
        {
            StopCoroutine(_aiMoveCoroutine);
            _animator.SetBool(IsWalk, false);
            _animator.Play("Attack");
            _isAttack = true;
            SetDirection();
        }

        protected override void OnPlayerLost()
        {
            GameUIManager.Instance.TryPopHpBar(GetInstanceID().ToString());
        }

        protected override void OnPlayerFound()
        {
            GameUIManager.Instance.TryPushHpBar(GetInstanceID().ToString(), "�ݵ���", (float)hp.Value / maxHp.Value);
            RefreshHp();

            if (!_isAttack && !_attack4Waiting)
            {
                StopCoroutine(_aiMoveCoroutine);
                _animator.SetBool(IsWalk, false);
                NormalAttack();
            }
        }

        private void AttackRange(int index, int dmg)
        {
            var players = new List<Collider2D>();
            var counts = attackColliders.OverlapCollider(_attackContactFilter, players);
            if (counts == 0) return;
            foreach (var col in players)
            {
                var player = col.GetComponent<Player.Player>();
                player.GetDamage(dmg);
            }
        }

        private void SetDirection()
        {
            switch (transform.position.x - lastTargetPlayer.player.transform.position.x)
            {
                case > 0:
                    OnDirectionSet(-1);
                    break;
                case < 0:
                    OnDirectionSet(1);
                    break;
            }
        }

        #region �ִϸ��̼� �̺�Ʈ

        public void OnDieEnd()
        {
            Destroy(gameObject);
        }

        public void OnAttack1Event1()
        {
            AttackRange(0, 20);
        }

        public void OnAttack1Event2()
        {
            AttackRange(1, 20);
        }

       
        #endregion
    }
}