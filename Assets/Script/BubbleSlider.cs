using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class BubbleText
{
    public string text;
    public string textName;
    public bool isRight;
}

public class BubbleSlider : MonoBehaviour
{   
    private float lowHeight;         // 滑块/上一个气泡的最低位置
    private float spacing = 0f;      // 气泡之间的间距
    public GameObject leftBubblePrefab;  // 左气泡预设
    public GameObject rightBubblePrefab;  // 右气泡预设
    public Transform content;        // Content 容器
    public ScrollRect scrollRect;
    public List<BubbleText> textBubbles;
    void Start()
    {
        lowHeight = 0f; //默认为0
    }

    void CreateBubbles()
    {
        // 清空现有的气泡
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        RectTransform st = gameObject.GetComponent<RectTransform>(); // 获取自身的 RectTransform


        // 创建新的气泡
        for (int i = 0; i < textBubbles.Count; i++)
        {
            GameObject bubble;
            // 判断左右气泡
            if (textBubbles[i].isRight == false) bubble = Instantiate(leftBubblePrefab, content);
            else bubble = Instantiate(rightBubblePrefab, content);

            // 修改 bubble 中的 textComponent 为自己设置的 text
            TextBackground textBackground = bubble.GetComponent<TextBackground>();
            textBackground.textComponent.text = textBubbles[i].text;

            //textBackground.bubbleName.text = textBubbles[i].textName;

            RectTransform rt = bubble.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(0, -1 * (lowHeight + spacing));  // 垂直排列
            lowHeight += textBackground.padding * 2 + textBackground.textComponent.preferredHeight + textBackground.bubblePadding;
            st.sizeDelta = new Vector2(st.sizeDelta.x, lowHeight);
        }
        scrollRect.verticalNormalizedPosition = 0; // 滚动到最底部
    }

}
