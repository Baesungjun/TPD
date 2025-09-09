using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPD
{
    public class SuitUpgradeUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] DealController2D poker;             // 잔고 차감용
        [SerializeField] SuitUpgradeSystem upgrades;         // 업그레이드 시스템

        [System.Serializable]
        public class Row
        {
            public Suit suit;
            public TMP_Text levelText;
            public TMP_Text costText;
            public Button   upgradeButton;
        }

        [Header("Rows")]
        public Row spadesRow;
        public Row heartsRow;
        public Row diamondsRow;
        public Row clubsRow;

        [Header("UX")]
        [SerializeField] float refreshInterval = 0.2f;       // UI 자동 갱신 주기
        [SerializeField] string lvlFmt = "Lv. {0}";
        [SerializeField] string costFmt = "{0:n0}";

        void Reset()
        {
            upgrades = FindObjectOfType<SuitUpgradeSystem>();
            poker    = FindObjectOfType<DealController2D>();
        }

        void Awake()
        {
            if (!upgrades) upgrades = SuitUpgradeSystem.Instance;
            Hook(spadesRow);
            Hook(heartsRow);
            Hook(diamondsRow);
            Hook(clubsRow);
        }

        void OnEnable()  { StartCoroutine(CoRefresh()); }
        void OnDisable() { StopAllCoroutines(); }

        IEnumerator CoRefresh()
        {
            while (true)
            {
                RefreshAll();
                yield return new WaitForSeconds(refreshInterval);
            }
        }

        void Hook(Row r)
        {
            if (r == null || r.upgradeButton == null) return;
            r.upgradeButton.onClick.RemoveAllListeners();
            r.upgradeButton.onClick.AddListener(() => OnClickUpgrade(r));
        }

        void OnClickUpgrade(Row r)
        {
            if (!poker || !upgrades || r == null) return;

            if (upgrades.IsMax(r.suit)) { Pulse(r.levelText, Color.yellow); return; }

            int cost = upgrades.Cost(r.suit);
            if (poker.GetBalance() < cost) { Pulse(r.costText, Color.red); return; }

            if (poker.TrySpend(cost))
            {
                if (upgrades.Upgrade(r.suit))
                {
                    RefreshRow(r);
                    Pulse(r.levelText, new Color(0.3f, 1f, 0.3f));
                }
            }
        }

        void RefreshAll()
        {
            RefreshRow(spadesRow);
            RefreshRow(heartsRow);
            RefreshRow(diamondsRow);
            RefreshRow(clubsRow);
        }

        void RefreshRow(Row r)
        {
            if (r == null || !upgrades) return;

            int lvl = upgrades.CurrentLevel(r.suit);
            bool isMax = upgrades.IsMax(r.suit);

            if (r.levelText) r.levelText.text = string.Format(lvlFmt, lvl);

            if (r.costText)
                r.costText.text = isMax ? "MAX" : string.Format(costFmt, upgrades.Cost(r.suit));

            if (r.upgradeButton)
                r.upgradeButton.interactable = !isMax && poker && poker.GetBalance() >= upgrades.Cost(r.suit);
        }

        void Pulse(TMP_Text t, Color c, float dur = 0.25f)
        {
            if (!t) return;
            StartCoroutine(CoPulse(t, c, dur));
        }

        IEnumerator CoPulse(TMP_Text t, Color c, float dur)
        {
            var orig = t.color;
            t.color = c;
            yield return new WaitForSeconds(dur);
            t.color = orig;
        }
    }
}
