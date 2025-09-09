using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TPD
{
    public class DealController2D : MonoBehaviour
    {
        // === 업그레이드 UI에서 사용할 공개 API ===
        public int GetBalance() => balance;

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (balance < amount) return false;
            balance -= amount;
            UpdateBalanceUI();
            return true;
        }

        [Header("Data")]
        [SerializeField] Deck2DConfig deckConfig;

        [Header("Prefabs/Areas")]
        [SerializeField] CardView cardPrefab;
        [SerializeField] Transform playerArea; // Slot0..2
        [SerializeField] Transform dealerArea; // Slot0..2
        [SerializeField] Sprite backSprite;

        [Header("UI")]
        [SerializeField] TMP_InputField betInput;
        [SerializeField] TMP_Text outcomeText;
        [SerializeField] TMP_Text payoutText;
        [SerializeField] TMP_Text balanceText;

        [Header("UI - Reroll Buttons")]
        [SerializeField] Button[] rerollButtons;

        [Header("UI - Round Controls")]
        [SerializeField] Button dealButton;
        [SerializeField] GameObject betGroupRoot;

        [Header("UI - Second Phase")]
        [SerializeField] Button foldButton;
        [SerializeField] Button secondBetButton;
        [SerializeField] GameObject secondPhaseRoot;

        [Header("Flow")]
        [SerializeField] float resultDelayToDefense = 3f;

        [Header("Economy")]
        [SerializeField] int startingBalance = 200;
        int balance;
        int currentBet = 0;

        [Header("Round Rules")]
        [SerializeField] bool shuffleEachDeal = true;
        [SerializeField] bool useFixedSeed = false;
        [SerializeField] int fixedSeed = 1234;
        [SerializeField] bool autoDealOnPlay = false;

        [Header("Animation")]
        [SerializeField, Min(0.05f)] float dealFlyDuration = 0.25f;
        [SerializeField] float dealFlyHeight = 1200f;
        [SerializeField] float dealBetweenDelay = 0.05f;
        [SerializeField, Min(0.05f)] float openingFlipDuration = 0.25f;
        [SerializeField] float openingFlipStagger = 0.08f;
        [SerializeField, Min(0.05f)] float rerollFlipDuration = 0.25f;
        [SerializeField, Min(0.05f)] float dealerRevealFlipDuration = 0.25f;

        Deck2D deck = new();
        CardView[] player = new CardView[3];
        CardView[] dealer = new CardView[3];
        CardDraw[] playerData = new CardDraw[3];
        CardDraw[] dealerData = new CardDraw[3];

        bool rerollUsed = false;
        bool dealerRevealed = false;
        bool dealingNow = false;
        bool gameOver = false;
        bool secondBetPlaced = false;

        Coroutine toDefenseCo;

        void Awake()
        {
            SetRerollButtonsVisible(false);
            SetRerollButtonsInteractable(false);

            balance = startingBalance;
            UpdateBalanceUI();

            SetRoundControlsVisible(true);
            SetRoundControlsInteractable(true);
            SetSecondPhaseVisible(false);

            if (outcomeText) outcomeText.text = "";
            if (payoutText)  payoutText.text  = "";

            if (balance <= 0) TriggerGameOver();
        }

        void Start()
        {
            if (autoDealOnPlay) Deal();
        }

        // ===== 디펜스에서 사용: 잔고 증감 =====
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            balance += amount;
            UpdateBalanceUI();
        }
        public void ApplyLeak(int amount)
        {
            if (amount <= 0) return;
            balance -= amount;
            UpdateBalanceUI();
            if (balance <= 0) TriggerGameOver();
        }

        // ---------------- 라운드 시작 ----------------
        public void Deal()
        {
            if (dealingNow || gameOver) return;

            if (!TryReadFirstBet(out int bet, out string err))
            {
                ShowMessage(err);
                return;
            }

            currentBet = bet;
            balance -= currentBet;
            UpdateBalanceUI();

            SetRoundControlsVisible(false);

            ClearAll();
            rerollUsed = false;
            dealerRevealed = false;
            secondBetPlaced = false;

            if (shuffleEachDeal)
            {
                int? seed = useFixedSeed ? fixedSeed : (int)(System.DateTime.UtcNow.Ticks & 0x0000FFFF);
                deck.Reset(deckConfig, seed);
            }
            else
            {
                deck.Reset(deckConfig, null);
            }

            if (outcomeText) outcomeText.text = "";
            if (payoutText)  payoutText.text  = "";

            SetRerollButtonsVisible(false);
            SetRerollButtonsInteractable(false);
            SetSecondPhaseVisible(false);

            StartCoroutine(CoDealSequence());
        }

        IEnumerator CoDealSequence()
        {
            dealingNow = true;

            // 플레이어 3장
            for (int i = 0; i < 3; i++)
            {
                var slot = playerArea.GetChild(i);
                var cv = Instantiate(cardPrefab, slot);
                var draw = deck.Draw();

                cv.SetFront(draw.front);
                cv.ShowBack();

                var rt = (RectTransform)cv.transform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, dealFlyHeight);
                yield return MoveY(rt, 0f, dealFlyDuration);

                player[i] = cv; playerData[i] = draw;

                yield return new WaitForSeconds(dealBetweenDelay);
            }

            // 딜러 3장
            for (int i = 0; i < 3; i++)
            {
                var slot = dealerArea.GetChild(i);
                var cv = Instantiate(cardPrefab, slot);
                var draw = deck.Draw();

                cv.SetFront(draw.front);
                cv.ShowBack();

                var rt = (RectTransform)cv.transform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, dealFlyHeight);
                yield return MoveY(rt, 0f, dealFlyDuration);

                dealer[i] = cv; dealerData[i] = draw;

                yield return new WaitForSeconds(dealBetweenDelay);
            }

            // 플레이어 오프닝 플립
            for (int i = 0; i < 3; i++)
            {
                yield return FlipToFront(player[i], openingFlipDuration);
                yield return new WaitForSeconds(openingFlipStagger);
            }

            SetRerollButtonsVisible(true);
            SetRerollButtonsInteractable(true);
            SetSecondPhaseVisible(true);

            dealingNow = false;
        }

        // ---------------- 리롤 ----------------
        public void RerollPlayer(int idx)
        {
            if (dealingNow || gameOver) return;
            if (dealerRevealed || rerollUsed) return;
            if (idx < 0 || idx > 2 || player[idx] == null) return;

            var draw = deck.Draw();
            rerollUsed = true;

            SetRerollButtonsVisible(false);

            StartCoroutine(CoRerollAnim(idx, draw));
        }

        IEnumerator CoRerollAnim(int idx, CardDraw draw)
        {
            var cv = player[idx];
            if (cv == null) yield break;

            var rt = (RectTransform)cv.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);

            float half = Mathf.Max(0.01f, rerollFlipDuration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = t / half;
                float sx = Mathf.Lerp(1f, 0f, p);
                rt.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            rt.localScale = new Vector3(0f, 1f, 1f);

            cv.SetFront(draw.front);
            cv.ShowFront();

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = t / half;
                float sx = Mathf.Lerp(0f, 1f, p);
                rt.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;

            playerData[idx] = draw;
        }

        // ---------------- 2차 선택 ----------------
        public void OnFold()
        {
            if (dealingNow || gameOver) return;

            SetRerollButtonsVisible(false);
            SetSecondPhaseVisible(false);

            if (outcomeText) outcomeText.text = $"폴드: {-currentBet:n0} 손실";
            if (payoutText)  payoutText.text  = "";

            var p = HandEvaluator3.Evaluate(
                playerData[0].rank, playerData[1].rank, playerData[2].rank,
                playerData[0].suit, playerData[1].suit, playerData[2].suit);

            int topRank = Mathf.Max(playerData[0].rank, Mathf.Max(playerData[1].rank, playerData[2].rank));
            var towerSuit = DetermineTowerSuit(p.rank, playerData[0], playerData[1], playerData[2]);

            if (toDefenseCo != null) StopCoroutine(toDefenseCo);
            toDefenseCo = StartCoroutine(CoEnterDefenseAfterDelay(resultDelayToDefense, p.rank, topRank, towerSuit));
        }

        public void OnSecondBetAndSettle()
        {
            if (dealingNow || gameOver) return;
            if (dealerRevealed || secondBetPlaced) return;

            if (balance < currentBet)
            {
                ShowMessage("잔고 부족: 추가 배팅 불가");
                return;
            }

            balance -= currentBet;
            UpdateBalanceUI();
            secondBetPlaced = true;

            SetRerollButtonsVisible(false);
            SetSecondPhaseVisible(false);

            StartCoroutine(CoSettle());
        }

        // ---------------- 정산 ----------------
        IEnumerator CoSettle()
        {
            dealingNow = true;

            var p = HandEvaluator3.Evaluate(
                playerData[0].rank, playerData[1].rank, playerData[2].rank,
                playerData[0].suit, playerData[1].suit, playerData[2].suit);

            var d = HandEvaluator3.Evaluate(
                dealerData[0].rank, dealerData[1].rank, dealerData[2].rank,
                dealerData[0].suit, dealerData[1].suit, dealerData[2].suit);

            if (!dealerRevealed)
            {
                dealerRevealed = true;
                yield return FlipGroupToFront(dealer, dealerRevealFlipDuration);
            }

            int totalBet = currentBet * (secondBetPlaced ? 2 : 1);
            var (o, pay) = PayoutCalculator.Settle(totalBet, p, d);

            if (o == Outcome.Win)       balance += pay;
            else if (o == Outcome.Push) balance += pay;

            UpdateBalanceUI();

            if (outcomeText) outcomeText.text = $"결과: {o} (P:{p.rank}, D:{d.rank})";
            if (payoutText)  payoutText.text  = (o == Outcome.Push) ? $"환급: {totalBet:n0}" : $"배당: {pay:n0}";

            dealingNow = false;

            if (balance <= 0)
            {
                TriggerGameOver();
                yield break;
            }

            int topRank = Mathf.Max(playerData[0].rank, Mathf.Max(playerData[1].rank, playerData[2].rank));
            var towerSuit = DetermineTowerSuit(p.rank, playerData[0], playerData[1], playerData[2]);

            if (toDefenseCo != null) StopCoroutine(toDefenseCo);
            toDefenseCo = StartCoroutine(CoEnterDefenseAfterDelay(resultDelayToDefense, p.rank, topRank, towerSuit));
        }

        IEnumerator CoEnterDefenseAfterDelay(float delay, HandRank rank, int topRank, Suit suit)
        {
            yield return new WaitForSeconds(delay);
            TPD.TowerDirector.Instance?.SpawnFromHand(rank, topRank, suit); // ✅ 문양 태그 전달
            TPD.GameFlowController.Instance?.EnterDefense();
            toDefenseCo = null;
        }

        // ---------------- GameOver/유틸 ----------------
        public void ResetToIdleScreen()
        {
            if (toDefenseCo != null) { StopCoroutine(toDefenseCo); toDefenseCo = null; }

            StopAllCoroutines();
            dealingNow = false;
            dealerRevealed = false;
            rerollUsed = false;
            secondBetPlaced = false;
            currentBet = 0;

            ClearAll();
            SetRerollButtonsVisible(false);
            SetRerollButtonsInteractable(false);
            SetSecondPhaseVisible(false);
            SetRoundControlsVisible(true);
            SetRoundControlsInteractable(!gameOver);

            if (outcomeText) outcomeText.text = "";
            if (payoutText)  payoutText.text  = "";
            if (betInput) betInput.text = "";
        }

        void TriggerGameOver()
        {
            gameOver = true;
            SetRerollButtonsVisible(false);
            SetSecondPhaseVisible(false);
            SetRoundControlsVisible(true);
            SetRoundControlsInteractable(false);
            if (outcomeText) outcomeText.text = "GAME OVER";
        }

        bool TryReadFirstBet(out int bet, out string error)
        {
            bet = 0; error = null;

            if (balance < 2)
            {
                error = "잔고가 부족합니다. (최소 2원 이상 필요)";
                return false;
            }
            if (betInput == null || string.IsNullOrWhiteSpace(betInput.text))
            {
                error = "배팅 금액을 입력하세요.";
                return false;
            }
            if (!int.TryParse(betInput.text, out bet) || bet <= 0)
            {
                error = "배팅 금액이 올바르지 않습니다.";
                return false;
            }
            if (bet > balance)
            {
                error = "잔고를 초과했습니다.";
                return false;
            }
            if (bet * 2 > balance)
            {
                int maxAllowed = balance / 2;
                error = $"배팅 금액은 잔고의 절반 이하이어야 합니다. (최대 {maxAllowed:n0})";
                return false;
            }
            return true;
        }

        void ShowMessage(string msg) { if (outcomeText) outcomeText.text = msg; }
        void UpdateBalanceUI() { if (balanceText) balanceText.text = $"잔고: {balance:n0}"; }

        void ClearAll()
        {
            for (int i = 0; i < 3; i++)
            {
                if (player[i]) Destroy(player[i].gameObject);
                if (dealer[i]) Destroy(dealer[i].gameObject);
                player[i] = null; dealer[i] = null;
            }
        }

        IEnumerator MoveY(RectTransform rt, float targetY, float duration)
        {
            float startY = rt.anchoredPosition.y;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p * (3f - 2f * p);
                float y = Mathf.Lerp(startY, targetY, eased);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                yield return null;
            }
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, targetY);
        }

        IEnumerator FlipToFront(CardView cv, float duration)
        {
            if (cv == null) yield break;
            var rt = (RectTransform)cv.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            float half = Mathf.Max(0.01f, duration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = t / half;
                float sx = Mathf.Lerp(1f, 0f, p);
                rt.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            rt.localScale = new Vector3(0f, 1f, 1f);

            cv.ShowFront();

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float p = t / half;
                float sx = Mathf.Lerp(0f, 1f, p);
                rt.localScale = new Vector3(sx, 1f, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        IEnumerator FlipGroupToFront(CardView[] group, float duration)
        {
            int count = 0;
            foreach (var cv in group) if (cv) count++;
            if (count == 0) yield break;

            int done = 0;
            foreach (var cv in group)
            {
                if (!cv) continue;
                StartCoroutine(FlipToFrontWithCallback(cv, duration, () => done++));
            }
            while (done < count) yield return null;
        }

        IEnumerator FlipToFrontWithCallback(CardView cv, float duration, System.Action onDone)
        {
            yield return FlipToFront(cv, duration);
            onDone?.Invoke();
        }

        void SetRerollButtonsVisible(bool visible)
        {
            if (rerollButtons == null) return;
            foreach (var b in rerollButtons)
                if (b) b.gameObject.SetActive(visible);
        }
        void SetRerollButtonsInteractable(bool interactable)
        {
            if (rerollButtons == null) return;
            foreach (var b in rerollButtons)
                if (b) b.interactable = interactable;
        }
        void SetRoundControlsVisible(bool visible)
        {
            if (dealButton) dealButton.gameObject.SetActive(visible);
            if (betGroupRoot) betGroupRoot.SetActive(visible);
            else if (betInput) betInput.gameObject.SetActive(visible);
        }
        void SetRoundControlsInteractable(bool interactable)
        {
            if (dealButton) dealButton.interactable = interactable;
            if (betInput)   betInput.interactable   = interactable;
        }
        void SetSecondPhaseVisible(bool visible)
        {
            if (secondPhaseRoot) secondPhaseRoot.SetActive(visible);
            if (foldButton) foldButton.gameObject.SetActive(visible);
            if (secondBetButton) secondBetButton.gameObject.SetActive(visible);
        }

        // ===== 문양 결정 로직 (타워 태깅용) =====
        // Straight/High : 최고 카드 문양
        // Flush/StraightFlush : 동일 문양 → 아무 카드 문양
        // Pair : 페어를 이루는 두 장 중 임의(여기선 Spades>Hearts>Diamonds>Clubs 우선)
        Suit DetermineTowerSuit(HandRank r, CardDraw a, CardDraw b, CardDraw c)
        {
            int Score(Suit s) => s switch { Suit.Spades => 4, Suit.Hearts => 3, Suit.Diamonds => 2, _ => 1 };

            if (r == HandRank.StraightFlush || r == HandRank.Flush)
                return a.suit; // 셋 다 동일 → 임의 사용

            if (r == HandRank.Pair)
            {
                if (a.rank == b.rank) return (Score(a.suit) >= Score(b.suit)) ? a.suit : b.suit;
                if (a.rank == c.rank) return (Score(a.suit) >= Score(c.suit)) ? a.suit : c.suit;
                return (Score(b.suit) >= Score(c.suit)) ? b.suit : c.suit; // b==c
            }

            // Straight / High
            CardDraw top = a;
            if (b.rank > top.rank) top = b;
            if (c.rank > top.rank) top = c;
            return top.suit;
        }
    }
}
