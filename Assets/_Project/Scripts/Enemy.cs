using System.Collections.Generic;
using UnityEngine;

namespace TPD
{
    public enum EnemyFamily { RUNNER, TANK, SPLITTER, BUFFER, FLYER, SHIELD, M_BOSS, BOSS }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        // ==== 활성 적 목록(타겟팅/스플래시용) ====
        public static readonly List<Enemy> Active = new List<Enemy>();

        [Header("Runtime Stats")]
        public EnemyFamily family;
        public int hp;
        public float move_spd;
        public int armor;
        public int leak_money;
        public int bounty;
        public bool boss_flag;

        Path2D path;
        int pathIndex = 0;
        const float REACH_EPS = 0.05f;

        public System.Action<Enemy> OnRemoved; // WaveSpawner가 구독
        public bool IsAlive => hp > 0;

        public void Setup(Path2D path, EnemyFamily fam, int hp, float spd, int armor, int leak, int bounty, bool boss)
        {
            this.path = path;
            this.family = fam;
            this.hp = hp;
            this.move_spd = spd;
            this.armor = armor;
            this.leak_money = leak;
            this.bounty = bounty;
            this.boss_flag = boss;

            transform.position = path ? path.GetPoint(0) : transform.position;
            pathIndex = 0;
        }

        void OnEnable()  { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        void Update()
        {
            if (path == null || path.Count == 0) return;

            Vector3 target = path.GetPoint(pathIndex);
            var dir = target - transform.position;
            float dist = dir.magnitude;

            if (dist <= REACH_EPS)
            {
                pathIndex++;
                if (pathIndex >= path.Count) { LeakAndDie(); return; }
                target = path.GetPoint(pathIndex);
                dir = target - transform.position;
            }

            if (dir.sqrMagnitude > 0.0001f)
                transform.position += dir.normalized * move_spd * Time.deltaTime;
        }

        // ====== 데미지 처리(아머 반영, 처치 시 보상) ======
        public void ApplyDamage(int rawDamage)
        {
            if (!IsAlive) return;
            int effective = Mathf.Max(1, rawDamage - armor);
            hp -= effective;
            if (hp <= 0) Remove(byKill: true);
        }

        public void Kill() { Remove(true); }

        void LeakAndDie()
        {
            if (boss_flag) GameFlowController.Instance?.ApplyLeakToBalance(999999999);
            else GameFlowController.Instance?.ApplyLeakToBalance(leak_money);
            Remove(byKill: false);
        }

        void Remove(bool byKill)
        {
            if (byKill && bounty > 0)
                GameFlowController.Instance?.AddGoldToBalance(bounty);

            OnRemoved?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
