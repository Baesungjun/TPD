using UnityEngine;

namespace TPD
{
    public class Path2D : MonoBehaviour
    {
        public Transform[] points;

        public int Count => points == null ? 0 : points.Length;
        public Vector3 GetPoint(int i)
        {
            if (points == null || points.Length == 0) return transform.position;
            i = Mathf.Clamp(i, 0, points.Length - 1);
            var p = points[i];
            return p ? p.position : transform.position;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (points == null || points.Length == 0) return;
            Gizmos.color = Color.white;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i] ? points[i].position : transform.position;
                var b = points[i + 1] ? points[i + 1].position : transform.position;
                Gizmos.DrawLine(a, b);
            }
        }
#endif
    }
}
