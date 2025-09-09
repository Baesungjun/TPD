using System.Linq;
using UnityEngine;

namespace TPD
{
    public class Projectile : MonoBehaviour
    {
        Enemy target;
        int damage;
        float speed;
        float life;
        float splashRadius;
        int chainsLeft;
        float chainRadius;

        Vector3 lastHitPos;
        float lifeTimer;

        // 시각화(없어도 자동 생성)
        SpriteRenderer sr;

        public void Setup(
            Vector3 startPos,
            Enemy target,
            int damage,
            float speed = 12f,
            float lifetime = 3f,
            float splashRadius = 0f,
            int chains = 0,
            float chainRadius = 2f)
        {
            transform.position = startPos;
            this.target = target;
            this.damage = Mathf.Max(1, damage);
            this.speed = Mathf.Max(0.1f, speed);
            this.life  = Mathf.Max(0.1f, lifetime);
            this.splashRadius = Mathf.Max(0f, splashRadius);
            this.chainsLeft = Mathf.Max(0, chains);
            this.chainRadius = Mathf.Max(0.1f, chainRadius);

            // 시각요소 폴백
            sr = GetComponent<SpriteRenderer>();
            if (!sr) sr = gameObject.AddComponent<SpriteRenderer>();
            if (!sr.sprite) sr.sprite = MakeSolidSprite(8, 8, new Color(1f, 1f, 1f, 0.9f));
            sr.sortingOrder = 10;
        }

        void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= life) { Destroy(gameObject); return; }

            if (target == null || !target.IsAlive)
            {
                if (!TryChainNext(lastHitPos)) { Destroy(gameObject); }
                return;
            }

            var pos = transform.position;
            var tpos = target.transform.position;
            var dir = tpos - pos;
            float step = speed * Time.deltaTime;

            if (dir.magnitude <= step)
            {
                // 명중
                HitAt(tpos, target);
                return;
            }

            transform.position = pos + dir.normalized * step;
        }

        void HitAt(Vector3 hitPos, Enemy victim)
        {
            lastHitPos = hitPos;

            if (victim && victim.IsAlive)
                victim.ApplyDamage(damage);

            // 스플래시
            if (splashRadius > 0f)
            {
                float r2 = splashRadius * splashRadius;
                foreach (var e in Enemy.Active.ToList())
                {
                    if (!e || !e.IsAlive || e == victim) continue;
                    if ((e.transform.position - hitPos).sqrMagnitude <= r2)
                        e.ApplyDamage(damage);
                }
            }

            // 체인
            if (chainsLeft > 0 && TryChainNext(hitPos)) return;

            Destroy(gameObject);
        }

        bool TryChainNext(Vector3 fromPos)
        {
            if (chainsLeft <= 0) return false;

            Enemy best = null;
            float bestSqr = float.MaxValue;
            float r2 = chainRadius * chainRadius;

            foreach (var e in Enemy.Active)
            {
                if (!e || !e.IsAlive) continue;
                float d2 = (e.transform.position - fromPos).sqrMagnitude;
                if (d2 <= r2 && d2 < bestSqr)
                {
                    bestSqr = d2; best = e;
                }
            }

            if (best != null)
            {
                target = best;
                chainsLeft--;
                return true;
            }
            return false;
        }

        // 단색 스프라이트 폴백
        static Sprite MakeSolidSprite(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = Enumerable.Repeat(c, w * h).ToArray();
            tex.SetPixels(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
