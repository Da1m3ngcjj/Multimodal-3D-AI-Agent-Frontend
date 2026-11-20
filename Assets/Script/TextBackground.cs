using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextBackground : MonoBehaviour
{
    public TextMeshProUGUI textComponent; // Text 或 TextMeshProUGUI 组件

    //public TextMeshProUGUI bubbleName;

    public Image backgroundImage; // 背景 Image 组件

    public Image bubblegroundImage; // 气泡 Image 组件

    public float padding = 10f; // 背景与文本之间的间距
    public float bubblePadding; // 气泡空出名字的宽度
    public bool isRight;
    private float maxWidth;     // 文本最大宽度

    

    void Start()
    {
        // 初始更新背景大小
        UpdateBackgroundSize();
        RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
        maxWidth = rectTransform.rect.width;
    }

    void Update()
    {
        // 在每帧更新背景大小
        UpdateBackgroundSize();
    }

    void UpdateBackgroundSize()
    {
        if (textComponent != null && backgroundImage != null && bubblegroundImage != null)
        {
            // 获取文本的宽度和高度
            float textWidth = textComponent.preferredWidth;
            float textHeight = textComponent.preferredHeight;

            //Debug.Log(textWidth);

            // 设置背景的大小，加入 padding
            if (maxWidth >= textWidth)
            {
                backgroundImage.rectTransform.sizeDelta = new Vector2(textWidth + padding * 2, textHeight + padding * 2);
                if (isRight) textComponent.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);
            }
            else
            {
                backgroundImage.rectTransform.sizeDelta = new Vector2(maxWidth + padding * 2, textHeight + padding * 2);
            }

            bubblegroundImage.rectTransform.sizeDelta = new Vector2(maxWidth + padding * 2,bubblePadding + textHeight + padding * 2);
        }
        else
        {
            Debug.Log("图片或者Text为空");
        }
    }
}
