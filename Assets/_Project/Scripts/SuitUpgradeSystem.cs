using UnityEngine;

namespace TPD
{
    public class SuitUpgradeSystem : MonoBehaviour
    {
        public static SuitUpgradeSystem Instance { get; private set; }

        [System.Serializable]
        public class SuitData
        {
            [Header("Progress")]
            public int level = 0;
            public int maxLevel = 20;

            [Header("Cost Formula: cost = baseCost * growth^level")]
            public int baseCost = 50;
            public float growth = 1.5f;

            [Header("Per-level bonuses")]
            public float atk_pct = 0.05f;        // +5%/lv
            public float atk_spd_pct = 0.04f;    // +4%/lv
            public float crit_chance_pct = 0.00f; // +0%p/ lv (필요시 조정)
            public float crit_mult_pct = 0.00f;   // +0% / lv (필요시 조정)
        }

        [Header("Per-Suit Settings")]
        public SuitData spades   = new SuitData { atk_pct = 0.06f, crit_chance_pct = 0.02f, crit_mult_pct = 0.10f, atk_spd_pct = 0.00f };
        public SuitData hearts   = new SuitData { atk_pct = 0.05f, atk_spd_pct    = 0.05f };
        public SuitData diamonds = new SuitData { atk_pct = 0.04f, atk_spd_pct    = 0.03f };
        public SuitData clubs    = new SuitData { atk_pct = 0.04f, atk_spd_pct    = 0.00f, crit_mult_pct = 0.05f };

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        SuitData GetData(Suit s) => s switch
        {
            Suit.Spades => spades,
            Suit.Hearts => hearts,
            Suit.Diamonds => diamonds,
            _ => clubs,
        };

        // ===== 비용/상태 =====
        public int CurrentLevel(Suit s) => GetData(s).level;
        public int MaxLevel(Suit s) => GetData(s).maxLevel;
        public bool IsMax(Suit s) => GetData(s).level >= GetData(s).maxLevel;

        public int Cost(Suit s)
        {
            var d = GetData(s);
            double c = d.baseCost * System.Math.Pow(d.growth, d.level);
            if (c < 1) c = 1;
            return (int)System.Math.Floor(c);
        }

        // ===== 실제 강화 처리(레벨 증가만) =====
        public bool Upgrade(Suit s)
        {
            var d = GetData(s);
            if (d.level >= d.maxLevel) return false;
            d.level++;
            return true;
        }

        // ===== 배율/가산치 (TowerShooter에서 사용) =====
        public float AtkMul(Suit s)
        {
            var d = GetData(s);
            return 1f + Mathf.Max(0, d.level) * Mathf.Max(0f, d.atk_pct);
        }
        public float AtkSpdMul(Suit s)
        {
            var d = GetData(s);
            return 1f + Mathf.Max(0, d.level) * Mathf.Max(0f, d.atk_spd_pct);
        }
        public float CritChanceAdd(Suit s)
        {
            var d = GetData(s);
            return Mathf.Max(0, d.level) * Mathf.Max(0f, d.crit_chance_pct);
        }
        public float CritMultMul(Suit s)
        {
            var d = GetData(s);
            return 1f + Mathf.Max(0, d.level) * Mathf.Max(0f, d.crit_mult_pct);
        }
    }
}
