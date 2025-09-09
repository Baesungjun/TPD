using UnityEngine;

namespace TPD
{
    public enum Phase { Poker, Defense }

    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        [Header("Roots (UI)")]
        [SerializeField] GameObject pokerRootUI;     // Canvas/PokerRootUI
        [SerializeField] GameObject defenseRootUI;   // Canvas/DefenseRootUI

        [Header("Roots (World)")]
        [SerializeField] GameObject defenseWorldRoot; // Canvas 밖 DefenseWorld

        [Header("Refs")]
        [SerializeField] DealController2D deal;       // 포커 컨트롤러
        [SerializeField] WaveSpawner spawner;         // ✅ 웨이브 스포너 (아래 스크립트)

        public Phase Current { get; private set; } = Phase.Poker;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnterPoker();
        }

        public void EnterPoker()
        {
            Current = Phase.Poker;
            if (pokerRootUI)      pokerRootUI.SetActive(true);
            if (defenseRootUI)    defenseRootUI.SetActive(false);
            if (defenseWorldRoot) defenseWorldRoot.SetActive(false);
        }

        public void EnterDefense()
        {
            Current = Phase.Defense;
            if (pokerRootUI)      pokerRootUI.SetActive(false);
            if (defenseRootUI)    defenseRootUI.SetActive(true);
            if (defenseWorldRoot) defenseWorldRoot.SetActive(true);

            // ✅ 디펜스 들어오면 웨이브 시작
            if (spawner) spawner.BeginWave();
            else Debug.LogWarning("[GameFlow] WaveSpawner not assigned.");
        }

        // 디펜스 웨이브 종료 버튼이나 자동 종료에서 호출
        public void OnWaveFinished()
        {
            EnterPoker();
            if (deal) deal.ResetToIdleScreen();
        }

        // ===== 디펜스 쪽에서 잔고 접근용 래퍼 =====
        public void ApplyLeakToBalance(int amount)  { if (deal) deal.ApplyLeak(amount); }
        public void AddGoldToBalance(int amount)    { if (deal) deal.AddGold(amount); }
    }
}
