using System.Linq;

namespace TPD
{
    public static class HandEvaluator3
    {
        public static (HandRank rank, int key) Evaluate(int r1,int r2,int r3, Suit s1,Suit s2,Suit s3)
        {
            var ranks = new[] { r1,r2,r3 }.OrderByDescending(x=>x).ToArray();
            bool flush = (s1==s2) && (s2==s3);

            bool straight=false; int top=0;
            var r = ranks.Distinct().OrderBy(x=>x).ToArray();
            if (r.Length==3)
            {
                straight = (r[1]==r[0]+1) && (r[2]==r[1]+1);
                top = r[2];
                if (!straight && r[0]==2 && r[1]==3 && r[2]==14) { straight=true; top=3; } // A-2-3
            }

            bool trips = (ranks[0]==ranks[1]) && (ranks[1]==ranks[2]);
            bool pair  = !trips && (ranks[0]==ranks[1] || ranks[1]==ranks[2]);

            HandRank hr; int a=0,b=0,c=0;
            if (straight && flush) { hr=HandRank.StraightFlush; a=top; }
            else if (trips)        { hr=HandRank.Trips;         a=ranks[0]; }
            else if (straight)     { hr=HandRank.Straight;      a=top; }
            else if (flush)        { hr=HandRank.Flush;         (a,b,c)=(ranks[0],ranks[1],ranks[2]); }
            else if (pair)         { hr=HandRank.Pair;          (a,b) = ((ranks[0]==ranks[1])? (ranks[0],ranks[2]) : (ranks[1],ranks[0])); }
            else                   { hr=HandRank.High;          (a,b,c)=(ranks[0],ranks[1],ranks[2]); }

            int key = ((int)hr)*1_000_000 + a*10_000 + b*100 + c;
            return (hr, key);
        }

        public static int Compare((HandRank rank,int key) p, (HandRank rank,int key) d)
            => (p.rank != d.rank) ? p.rank.CompareTo(d.rank) : p.key.CompareTo(d.key);
    }
}
