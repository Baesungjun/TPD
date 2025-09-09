using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] Image frontImage; // Front 오브젝트의 Image
    [SerializeField] Image backImage;  // Back 오브젝트의 Image

    // 모드 선택: FillParent = UI 크기 통일, Native = 스프라이트 PPU 반영
    public enum SizeMode { FillParent, Native }
    [SerializeField] SizeMode sizeMode = SizeMode.FillParent;

    bool isBack = true;

    void Awake()
    {
        ApplySizing();
    }

    public void SetFront(Sprite sprite)
    {
        if (!frontImage) { Debug.LogError("CardView: Front Image 미지정"); return; }
        frontImage.sprite = sprite;
        ApplySizing(); // 앞면 교체 시에도 현재 모드대로 사이즈 동기화
    }

    public void ShowFront()
    {
        if (frontImage) frontImage.enabled = true;
        if (backImage)  backImage.enabled  = false;
        isBack = false;
    }

    public void ShowBack()
    {
        if (frontImage) frontImage.enabled = false;
        if (backImage)  backImage.enabled  = true;
        isBack = true;
    }

    public void Flip()
    {
        if (isBack) ShowFront();
        else ShowBack();
    }

    void ApplySizing()
    {
        if (!frontImage || !backImage) return;

        frontImage.preserveAspect = true;
        backImage.preserveAspect  = true;

        if (sizeMode == SizeMode.Native)
        {
            // 중앙 앵커/피벗 + 네이티브 크기 적용 (양면 동일하게)
            SetCenter(frontImage.rectTransform);
            SetCenter(backImage.rectTransform);
            if (frontImage.sprite) frontImage.SetNativeSize();
            if (backImage.sprite)  backImage.SetNativeSize();
            frontImage.rectTransform.anchoredPosition = Vector2.zero;
            backImage.rectTransform.anchoredPosition  = Vector2.zero;
        }
        else // FillParent
        {
            // 부모 크기에 맞춰 꽉 채우기 (PPU 무시)
            StretchToParent(frontImage.rectTransform);
            StretchToParent(backImage.rectTransform);
        }
    }

    static void SetCenter(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
