using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPD
{
    [Serializable]
    public struct CardDraw
    {
        public int id;     // 0..51
        public int rank;   // 2..14
        public Suit suit;  // 0..3
        public Sprite front;
    }

    public class Deck2D
    {
        readonly List<int> ids = new(52);
        System.Random rng;
        Deck2DConfig cfg;
        int idx;

        public void Reset(Deck2DConfig config, int? seed=null)
        {
            cfg = config;
            if (cfg == null || cfg.cardFronts == null || cfg.cardFronts.Length != 52)
            {
                Debug.LogError("Deck2D: cardFronts가 52장이 아닙니다."); return;
            }
            ids.Clear();
            for (int i = 0; i < 52; i++) ids.Add(i);
            Shuffle(seed);
        }

        public void Shuffle(int? seed=null)
        {
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            idx = 0;
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
        }

        public CardDraw Draw()
        {
            int id = ids[idx++];
            int suitIdx = id / 13;           // 0:Sp,1:He,2:Di,3:Cl
            int rank    = (id % 13) + 2;     // 2..14
            return new CardDraw {
                id=id, rank=rank, suit=(Suit)suitIdx, front=cfg.cardFronts[id]
            };
        }
    }
}
