using System.Collections.Generic;
using UnityEngine;

namespace TPD
{
    [RequireComponent(typeof(Tower))]
    public class TowerShooter : MonoBehaviour
    {
        [Header("Projectile")]
        public GameObject projectilePrefab;   // 비워두면 폴백 생성(Projectile가 자체 생성)
        public float projectileSpeed = 12f;
        public float projectileLifetime = 3f;
        public float chainRadius = 2f;

        [Header("Targeting")]
        public float reacquireInterval = 0.2f;

        Tower tower;
        float fireTimer;
        readonly List<Enemy> tempTargets = new List<Enemy>();

        void Awake()
        {
            tower = GetComponent<Tower>();
        }

        void Update()
        {
            if (tower == null) return;

            // 공속 = 타워 공속 × 문양 업그레이드 배율
            float atkSpdMul = SuitUpgradeSystem.Instance ? SuitUpgradeSystem.Instance.AtkSpdMul(tower.suit) : 1f;
            float effectiveAtkSpd = Mathf.Max(0.01f, tower.atk_spd * atkSpdMul);
            float interval = 1f / effectiveAtkSpd;

            fireTimer += Time.deltaTime;
            if (fireTimer >= interval)
            {
                Fire();
                fireTimer = 0f;
            }
        }

        void Fire()
        {
            // 사거리 내 타겟 수집
            tempTargets.Clear();
            float r2 = tower.range * tower.range;
            foreach (var e in Enemy.Active)
            {
                if (!e || !e.IsAlive) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= r2)
                    tempTargets.Add(e);
            }
            if (tempTargets.Count == 0) return;

            // 가까운 순
            tempTargets.Sort((a, b) =>
            {
                float da = (a.transform.position - transform.position).sqrMagnitude;
                float db = (b.transform.position - transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            int shots = tower.multi_target ? Mathf.Min(1 + tower.chain_count, tempTargets.Count) : 1;

            // 문양 배율
            float atkMul  = SuitUpgradeSystem.Instance ? SuitUpgradeSystem.Instance.AtkMul(tower.suit) : 1f;
            float ccAdd   = SuitUpgradeSystem.Instance ? SuitUpgradeSystem.Instance.CritChanceAdd(tower.suit) : 0f;
            float cmMul   = SuitUpgradeSystem.Instance ? SuitUpgradeSystem.Instance.CritMultMul(tower.suit) : 1f;

            for (int i = 0; i < shots; i++)
            {
                var target = tempTargets[i % tempTargets.Count];
                if (!target || !target.IsAlive) continue;

                int baseDmg = Mathf.Max(1, tower.atk);
                int dmgMul  = Mathf.RoundToInt(baseDmg * atkMul);

                float critChance = Mathf.Clamp01(tower.crit_chance + ccAdd);
                float critMult   = tower.crit_mult * cmMul;
                bool crit = Random.value < critChance;
                int dmg = Mathf.RoundToInt(dmgMul * (crit ? critMult : 1f));

                SpawnProjectile(target, dmg);
            }
        }

        void SpawnProjectile(Enemy target, int dmg)
        {
            GameObject go = projectilePrefab ? Instantiate(projectilePrefab) : new GameObject("Projectile");
            var proj = go.GetComponent<Projectile>();
            if (!proj) proj = go.AddComponent<Projectile>();

            proj.Setup(
                startPos: transform.position,
                target: target,
                damage: dmg,
                speed: projectileSpeed,
                lifetime: projectileLifetime,
                splashRadius: Mathf.Max(0f, tower.splash_radius),
                chains: Mathf.Max(0, tower.chain_count),
                chainRadius: chainRadius
            );
        }
    }
}
