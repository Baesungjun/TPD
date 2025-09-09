using UnityEngine;

public class CardOneShot : MonoBehaviour
{
    [SerializeField] CardView cardPrefab; // CardView 프리팹
    [SerializeField] Sprite front;        // 앞면 스프라이트(Inspector로 드래그)

    void Start()
    {
        Debug.Log("CardOneShot Start");
        if (cardPrefab == null) { Debug.LogError("CardOneShot: cardPrefab 미지정"); return; }
        if (front == null) { Debug.LogError("CardOneShot: front 스프라이트 미지정"); return; }

        var card = Instantiate(cardPrefab, transform);
        card.SetFront(front);

        var rt = (RectTransform)card.transform;
        rt.anchoredPosition = Vector2.zero;       // 중앙
        //rt.sizeDelta = new Vector2(90, 128);     // 보기 좋은 크기
        card.ShowFront();                         // 앞면 보이기
    }
}
