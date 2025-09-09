using UnityEngine;

namespace TPD
{
    public class Tower : MonoBehaviour
    {
        [Header("Stats")]
        public int atk = 1;
        public float atk_spd = 1f;
        public float range = 3f;
        public float crit_chance = 0f;
        public float crit_mult = 1.5f;
        public bool  multi_target = false;
        public int   chain_count = 0;
        public float splash_radius = 0f;

        [Header("Identity")]
        public HandRank handRank;   // 어떤 족보로 나왔는지(정보용)
        public Suit suit = Suit.Spades; // 문양 업그레이드 적용용 태그

        [Header("Debug")]
        [SerializeField] string label;

        // 새: 문양 포함 Setup
        public void Setup(int finalAtk, Suit suit, HandRank hr, string debugLabel = "")
        {
            atk = finalAtk;
            this.suit = suit;
            this.handRank = hr;
            label = debugLabel;
            name = string.IsNullOrEmpty(debugLabel)
                ? $"Tower({hr},{suit})[{atk}]"
                : $"Tower {debugLabel} ({atk})";
        }

        // 구버전 호환 Setup (문양 미지정 시 Spades)
        public void Setup(int finalAtk, string debugLabel = "")
        {
            Setup(finalAtk, Suit.Spades, HandRank.High, debugLabel);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
            if (splash_radius > 0f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, splash_radius);
            }
        }
#endif
    }
}
