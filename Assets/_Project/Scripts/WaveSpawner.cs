using System.Collections;
using UnityEngine;

namespace TPD
{
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Enemy enemyPrefab;    // 스프라이트+Enemy 컴포넌트 프리팹
        [SerializeField] Path2D path;          // 경로

        [Header("Wave Numbers")]
        [SerializeField] int wave = 1;

        [Header("Scales (설계서 기반 기본값)")]
        [SerializeField] int hp_base = 30;
        [SerializeField] float hp_growth = 1.15f;
        [SerializeField] int leak_base = 20;
        [SerializeField] float leak_growth = 1.10f;
        [SerializeField] float move_spd_base = 2.0f; // 러너 기준
        [SerializeField] float spawn_interval = 0.6f;

        [Header("Spawn Count")]
        [SerializeField] int N_base = 8;
        [SerializeField] float g_spawn = 0.12f; // 웨이브당 증가율
        [SerializeField] int N_max = 25;

        int totalToSpawn;
        int spawned;
        int alive;

        public void BeginWave()
        {
            StopAllCoroutines();
            spawned = 0; alive = 0;

            // 5n 미니보스, 10n 보스
            bool isMini = (wave % 5 == 0) && (wave % 10 != 0);
            bool isBoss = (wave % 10 == 0);

            // 스폰 수
            totalToSpawn = Mathf.Clamp(Mathf.RoundToInt(N_base * (1f + g_spawn * (wave - 1))), N_base, N_max);
            if (isMini) totalToSpawn = Mathf.Max(1, totalToSpawn / 2);
            if (isBoss) totalToSpawn = 1;

            StartCoroutine(CoSpawnWave(isMini, isBoss));
        }

        IEnumerator CoSpawnWave(bool isMini, bool isBoss)
        {
            if (!enemyPrefab || !path)
            {
                Debug.LogError("[WaveSpawner] Prefab 또는 Path가 비었습니다.");
                yield break;
            }

            // 스탯 계산
            int hp = Mathf.RoundToInt(hp_base * Mathf.Pow(hp_growth, wave - 1));
            int leak = Mathf.RoundToInt(leak_base * Mathf.Pow(leak_growth, wave - 1));
            float spd = move_spd_base;

            int bountyPer = wave * 100; // 설계서: 웨이브 기본 보상
            if (isMini) bountyPer *= 2;
            if (isBoss) bountyPer *= 4;

            for (int i = 0; i < totalToSpawn; i++)
            {
                var e = Instantiate(enemyPrefab, path.GetPoint(0), Quaternion.identity, transform.parent);
                var fam = isBoss ? EnemyFamily.BOSS : (isMini ? EnemyFamily.M_BOSS : EnemyFamily.RUNNER);

                bool bossFlag = isBoss;
                int leakMoney = isMini ? leak * 2 : (isBoss ? leak * 4 : leak);

                e.Setup(path, fam, hp, spd, 0, leakMoney, bountyPer, bossFlag);
                e.OnRemoved += HandleRemoved;

                spawned++;
                alive++;

                yield return new WaitForSeconds(spawn_interval);
            }

            // 모두 제거될 때까지 대기
            while (alive > 0) yield return null;

            // 웨이브 종료 → 포커로
            wave++;
            GameFlowController.Instance?.OnWaveFinished();
        }

        void HandleRemoved(Enemy e)
        {
            if (e) e.OnRemoved -= HandleRemoved;
            alive = Mathf.Max(0, alive - 1);
        }
    }
}
