using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ChatUIManager : MonoBehaviour
{
    [Header("UI 组件")]
    public TMP_InputField inputField; 
    public Button sendButton;         
    public TMP_Text sendButtonText;
    public ScrollRect scrollRect;      

    [Header("气泡 Prefab")]
    public GameObject userBubblePrefab; // 用户发出的绿色气泡（靠右）
    public GameObject aiBubblePrefab;   // AI 回复的白色气泡（靠左）

    [Header("排版参数")]
    public float spacing = 0f;          // 气泡之间的上下间距
    public float bottomPadding = 50f;   // 最后一个气泡距离最底部的留白

    private float lowHeight = 0f;       // 记录当前已经排版到了 Y 轴的哪个位置（往下是负数）
    private RectTransform st;           // 指向 ScrollView 的 Content 节点，用于动态撑高

    // 记录当前正在打字的协程，方便中途打断
    private Coroutine currentTypingCoroutine;
    // 记录当前正在打字的气泡物体
    private GameObject currentTypingBubble;
    // 记录当前正在打字的完整文本（打断时直接拿出来用）
    private string currentFullReply;

    public Button SendButton => sendButton;
    public TMP_InputField InputField => inputField;

    void Start()
    {
        // 获取 ScrollView 的 Content 容器
        if (scrollRect != null && scrollRect.content != null)
        {
            st = scrollRect.content;
        }
        lowHeight = 0f; 
    }
    

    // ======================= 生成气泡接口 =======================

    public void AddUserBubble(string text)
    {
        // 生成用户气泡
        GameObject bubble = CreateBubble(userBubblePrefab, text, false);
        // 播放“啵”的弹出动画
        StartCoroutine(PopAnimation(bubble));
    }

    public GameObject AddAIBubble(string text)
    {
        // 生成 AI 气泡
        GameObject bubble = CreateBubble(aiBubblePrefab, text, true);
        StartCoroutine(PopAnimation(bubble));
        // 返回引用，让 Controller 能抓住它
        return bubble; 
    }

    public void AddErrorBubble(string errorMsg)
    {
        // 专门用于生成报错气泡，文字自动标红
        GameObject bubble = CreateBubble(aiBubblePrefab, "<color=#FF6E5A>" + errorMsg + "</color>", true);
        StartCoroutine(PopAnimation(bubble));
    }

    // ======================= 核心排版逻辑 =======================

    private GameObject CreateBubble(GameObject prefab, string text, bool isLeft)
    {
        // 1. 克隆 Prefab 并挂到 Content 下
        GameObject bubble = Instantiate(prefab, st);
        
        // 2. 设置文字内容
        TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();
        textComponent.text = text;
        textComponent.enableWordWrapping = true; 

        // 3. 计算它在 Y 轴上的位置（因为是往下排，所以坐标是负的）
        RectTransform rt = bubble.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -1 * (lowHeight + spacing));

        // 4. 更新当前 Content 的总高度，防止滚动条失效
        UpdateContentHeight(bubble);
        ScrollToBottom();
        
        return bubble;
    }

    private void UpdateContentHeight(GameObject bubble)
    {
        // 获取你自定义的 TextBackground 组件，用来精确计算气泡高度
        TextBackground textBackground = bubble.GetComponent<TextBackground>();
        if (textBackground != null && st != null)
        {
            // 强制刷新文本的底层排版，确保读到最新的高度
            textBackground.textComponent.ForceMeshUpdate();
            
            // 算出当前气泡的总高度
            float currentBubbleHeight = textBackground.padding * 2 + textBackground.textComponent.preferredHeight + textBackground.bubblePadding;
            
            RectTransform rt = bubble.GetComponent<RectTransform>();
            // 当前气泡的底边位置 = 负坐标的绝对值 + 气泡高度
            float currentBottomPos = Mathf.Abs(rt.anchoredPosition.y) + currentBubbleHeight;
            // 加上底部留白，算出 Content 需要达到的总高度
            float targetContentHeight = currentBottomPos + bottomPadding;
            
            // 如果内容超出了现有的高度，就撑大 Content
            if (targetContentHeight > st.sizeDelta.y)
            {
                st.sizeDelta = new Vector2(st.sizeDelta.x, targetContentHeight);
            }

            // 更新 lowHeight，告诉下一个生成的气泡该排在哪个 Y 坐标
            lowHeight = currentBottomPos + spacing;
        }
    }

    // ======================= 打字机与打断逻辑 =======================

    public void StartTypewriterOnBubble(GameObject bubble, string fullText, float speed)
    {
        // 先停掉可能正在进行的旧的打字机
        StopAllTypewriters();
        // 启动新的打字机协程并记录
        currentTypingCoroutine = StartCoroutine(TypewriterRoutine(bubble, fullText, speed));
    }

    private IEnumerator TypewriterRoutine(GameObject bubble, string fullText, float speed)
    {
        currentTypingBubble = bubble;
        currentFullReply = fullText;
        
        TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();
        if (textComponent == null) yield break;

        // 清空原文字（比如清空“思考中”）
        textComponent.text = "";

        // 逐个字符添加
        foreach (char c in fullText)
        {
            textComponent.text += c;
            UpdateContentHeight(bubble);
            ScrollToBottom();
            
            // 【关键修改】使用传入的 speed 变量
            yield return new WaitForSeconds(speed); 
        }
        
        // 打字完成，把当前排版高度再锁死一次
        UpdateContentHeight(bubble);
        currentTypingBubble = null;
        currentFullReply = null;
        currentTypingCoroutine = null;
    }

    public void StopAllTypewriters()
    {
        // 如果当前有正在打字的协程
        if (currentTypingCoroutine != null)
        {
            // 直接掐断协程
            StopCoroutine(currentTypingCoroutine);
            
            if (currentTypingBubble != null)
            {
                TMP_Text textComponent = currentTypingBubble.GetComponentInChildren<TMP_Text>();
                if (textComponent != null && !string.IsNullOrEmpty(currentFullReply))
                {
                    // 关键：瞬间把剩下的所有文字直接填进去
                    textComponent.text = currentFullReply; 
                    
                    // 重新计算一次高度，让气泡瞬间变高
                    UpdateContentHeight(currentTypingBubble);
                    ScrollToBottom();
                }
            }
            // 清理记录状态
            currentTypingCoroutine = null;
            currentTypingBubble = null;
            currentFullReply = null;
        }
    }

    // ======================= 思考中动画 =======================

    public IEnumerator AnimateThinking(GameObject bubble)
    {
        TMP_Text textComponent = bubble.GetComponentInChildren<TMP_Text>();
        if (textComponent == null) yield break;

        string baseText = "思考中";
        int dotCount = 0;

        // 只要文字还是以“思考中”开头，就一直循环
        while (textComponent.text.StartsWith(baseText))
        {
            dotCount = (dotCount % 3) + 1;
            
            // 巧用透明颜色占位，确保气泡不会忽大忽小
            string dots = new string('.', dotCount) + "<color=#00000000>" + new string('.', 3 - dotCount) + "</color>";
            textComponent.text = baseText + dots;
            
            // 强制排版更新
            TextBackground textBackground = bubble.GetComponent<TextBackground>();
            if (textBackground != null)
            {
                textBackground.textComponent.ForceMeshUpdate();
                float currentBubbleHeight = textBackground.padding * 2 + textBackground.textComponent.preferredHeight + textBackground.bubblePadding;
                RectTransform rt = bubble.GetComponent<RectTransform>();
                float targetContentHeight = Mathf.Abs(rt.anchoredPosition.y) + currentBubbleHeight + bottomPadding;
                
                if (targetContentHeight > st.sizeDelta.y)
                {
                    st.sizeDelta = new Vector2(st.sizeDelta.x, targetContentHeight);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // ======================= 辅助方法 =======================

    public IEnumerator PopAnimation(GameObject bubble)
    {
        // 气泡从 0.8 倍缩放瞬间放大到 1 倍
        bubble.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        float timer = 0f;
        float duration = 0.15f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bubble.transform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, timer / duration);
            yield return null;
        }
        bubble.transform.localScale = Vector3.one;
    }

    public void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            // 0 代表滚动到最底部
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearInputField()
    {
        inputField.text = "";
        sendButton.interactable = false; 

        if (sendButtonText != null)
        {
            sendButtonText.color = new Color(1f, 1f, 1f, 0.1f);
        }

        inputField.ActivateInputField(); 
    }
}