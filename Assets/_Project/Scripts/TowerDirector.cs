using UnityEngine;

namespace TPD
{
    public enum TowerArchetype { Bonus, AoE, Rapid, Synergy, Standard, Cheap }

    public class TowerDirector : MonoBehaviour
    {
        public static TowerDirector Instance { get; private set; }

        [Header("Spawn Roots")]
        [SerializeField] Transform towerParent;   // DefenseWorld/TowerParent
        [SerializeField] Transform[] buildSlots;  // DefenseWorld/BuildSlot0..2

        [Header("Prefabs (Archetype)")]
        [SerializeField] Tower prefabBonus;   // STRAIGHT_FLUSH
        [SerializeField] Tower prefabAoE;     // TRIPS
        [SerializeField] Tower prefabRapid;   // STRAIGHT
        [SerializeField] Tower prefabSynergy; // FLUSH
        [SerializeField] Tower prefabStd;     // PAIR
        [SerializeField] Tower prefabCheap;   // HIGH

        [Header("Base Stats by Archetype")]
        [SerializeField] float baseAtkSpd_Bonus   = 0.8f;
        [SerializeField] float baseAtkSpd_AoE     = 0.7f;
        [SerializeField] float baseAtkSpd_Rapid   = 1.5f;
        [SerializeField] float baseAtkSpd_Synergy = 1.0f;
        [SerializeField] float baseAtkSpd_Std     = 1.0f;
        [SerializeField] float baseAtkSpd_Cheap   = 0.9f;

        [SerializeField] float baseRange_Bonus    = 3.5f;
        [SerializeField] float baseRange_AoE      = 2.8f;
        [SerializeField] float baseRange_Rapid    = 3.0f;
        [SerializeField] float baseRange_Synergy  = 3.2f;
        [SerializeField] float baseRange_Std      = 3.0f;
        [SerializeField] float baseRange_Cheap    = 2.8f;

        [Header("Shooter Defaults")]
        [SerializeField] GameObject defaultProjectilePrefab; // 비워도 폴백
        [SerializeField] float defaultProjectileSpeed = 12f;
        [SerializeField] float defaultProjectileLifetime = 3f;
        [SerializeField] float defaultChainRadius = 2f;

        int nextSlot = 0;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // 기존 콜(호환): suit 미지정 → Spades로 태깅
        public void SpawnFromHand(HandRank handRank, int topRank)
        {
            SpawnFromHand(handRank, topRank, Suit.Spades);
        }

        // 새 콜: DealController가 계산한 suit를 태깅만 해서 넘김
        public void SpawnFromHand(HandRank handRank, int topRank, Suit suit)
        {
            var (arc, mul) = Map(handRank);
            int finalAtk = Mathf.Max(2, topRank) * mul;

            if (buildSlots == null || buildSlots.Length == 0)
            {
                Debug.LogWarning("[TowerDirector] No build slots assigned");
                return;
            }
            var slot = buildSlots[nextSlot % buildSlots.Length];
            nextSlot++;

            var prefab = PickPrefab(arc);
            if (!prefab)
            {
                Debug.LogWarning($"[TowerDirector] Prefab for {arc} not set");
                return;
            }

            var t = Instantiate(prefab, slot.position, Quaternion.identity, towerParent);
            ApplyBaseStats(t, arc);
            t.Setup(finalAtk, suit, handRank, $"{handRank}/{suit}");

            EnsureShooter(t);
        }

        (TowerArchetype, int) Map(HandRank r) => r switch {
            HandRank.StraightFlush => (TowerArchetype.Bonus,   30),
            HandRank.Trips         => (TowerArchetype.AoE,     15),
            HandRank.Straight      => (TowerArchetype.Rapid,    5),
            HandRank.Flush         => (TowerArchetype.Synergy,  2),
            HandRank.Pair          => (TowerArchetype.Standard, 1),
            _                      => (TowerArchetype.Cheap,    1),
        };

        Tower PickPrefab(TowerArchetype arc) => arc switch {
            TowerArchetype.Bonus    => prefabBonus,
            TowerArchetype.AoE      => prefabAoE,
            TowerArchetype.Rapid    => prefabRapid,
            TowerArchetype.Synergy  => prefabSynergy,
            TowerArchetype.Standard => prefabStd,
            _                       => prefabCheap,
        };

        void ApplyBaseStats(Tower t, TowerArchetype arc)
        {
            switch (arc)
            {
                case TowerArchetype.Bonus:
                    t.atk_spd = baseAtkSpd_Bonus;   t.range = baseRange_Bonus;   t.multi_target = true;  t.chain_count = 1; break;
                case TowerArchetype.AoE:
                    t.atk_spd = baseAtkSpd_AoE;     t.range = baseRange_AoE;     t.splash_radius = 1.2f; break;
                case TowerArchetype.Rapid:
                    t.atk_spd = baseAtkSpd_Rapid;   t.range = baseRange_Rapid;   break;
                case TowerArchetype.Synergy:
                    t.atk_spd = baseAtkSpd_Synergy; t.range = baseRange_Synergy; break;
                case TowerArchetype.Standard:
                    t.atk_spd = baseAtkSpd_Std;     t.range = baseRange_Std;     break;
                case TowerArchetype.Cheap:
                    t.atk_spd = baseAtkSpd_Cheap;   t.range = baseRange_Cheap;   break;
            }
        }

        void EnsureShooter(Tower t)
        {
            var shooter = t.GetComponent<TowerShooter>();
            if (!shooter) shooter = t.gameObject.AddComponent<TowerShooter>();

            if (defaultProjectilePrefab) shooter.projectilePrefab = defaultProjectilePrefab;
            shooter.projectileSpeed = defaultProjectileSpeed;
            shooter.projectileLifetime = defaultProjectileLifetime;
            shooter.chainRadius = defaultChainRadius;
        }
    }
}
