using System;
using System.Collections.Generic;

namespace TPD
{
    public static class PayoutCalculator
    {
        static readonly Dictionary<HandRank,float> Mult = new()
        {
            { HandRank.StraightFlush, 5.0f },
            { HandRank.Trips,         3.0f },
            { HandRank.Straight,      2.0f },
            { HandRank.Flush,         1.5f },
            { HandRank.Pair,          1.5f },
            { HandRank.High,          1.2f },
        };

        public static (Outcome outcome,int payout) Settle(int bet, (HandRank,int) p, (HandRank,int) d)
        {
            int cmp = HandEvaluator3.Compare(p, d);
            if (cmp > 0) { int pay=(int)MathF.Floor(bet + bet*Mult[p.Item1]); return (Outcome.Win, pay); }
            if (cmp == 0) return (Outcome.Push, bet);
            return (Outcome.Loss, 0);
        }
    }
}
