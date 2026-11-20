using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Networking;

public class ChatController : MonoBehaviour
{
    [Header("挂载的组件")]
    public AIAgentClient agentClient;
    public ChatUIManager uiManager;
    public VoicePlayer voicePlayer;

    [Header("语音设置")]
    public Toggle voiceToggle;
    private string pendingReply = "";

    [Header("打字速度设置")]
    public float typingSpeedWithVoice = 0.08f; 
    public float typingSpeedNoVoice = 0.02f;   

    [Header("Agent 动作 UI")]
    public Image displayImage; 
    public Toggle displayToggle;    
    public GameObject targetParent; 
    private Coroutine currentImageLoadCoroutine;
    private string currentModelKeyword = "";

    public AvatarModelSpawner modelSpawner; 

    // 内部状态记录
    private GameObject currentThinkingBubble;
    private float thinkingStartTime;
    private Coroutine sendCoroutine;

    void Start()
    {
        uiManager.SendButton.onClick.AddListener(OnSendClicked);
        uiManager.InputField.onValueChanged.AddListener(OnInputValueChanged);

        agentClient.OnResponseReceived += HandleAIResponse;
        agentClient.OnErrorOccurred += HandleAIError;

        if (voiceToggle != null)
        {
            voiceToggle.onValueChanged.AddListener((isOn) =>
            {
                if (!isOn && voicePlayer != null)
                {
                    voicePlayer.StopVoice();
                    uiManager.StopAllTypewriters(); 
                }
            });
        }

        if (voicePlayer != null)
        {
            voicePlayer.OnVoiceReady += HandleVoiceReady;
            voicePlayer.OnVoiceFailed += (err) =>
            {
                Debug.LogError("语音失败: " + err);
                StartTypewriter(pendingReply); 
            };
        }

        if (displayToggle != null && targetParent != null)
        {
            targetParent.SetActive(displayToggle.isOn);
            displayToggle.onValueChanged.AddListener((isOn) =>
            {
                StartCoroutine(ToggleParentWithAnimation(isOn));
            });
        }
    }

    void Update()
    {
        if (uiManager.InputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(uiManager.InputField.text))
                {
                    OnSendClicked();
                }
            }
        }
    }

    void OnInputValueChanged(string text)
    {
        bool hasText = !string.IsNullOrWhiteSpace(text);
        uiManager.SendButton.interactable = hasText;

        if (uiManager.sendButtonText != null)
        {
            uiManager.sendButtonText.color = hasText ? Color.white : new Color(1f, 1f, 1f, 0.1f);
        }
    }

    void OnSendClicked()
    {
        if (string.IsNullOrEmpty(uiManager.InputField.text)) return;

        if (currentImageLoadCoroutine != null)
        {
            StopCoroutine(currentImageLoadCoroutine);
            currentImageLoadCoroutine = null;
        }

        string question = uiManager.InputField.text;
        uiManager.ClearInputField();

        // 判断用户是不是要生成模型
        currentModelKeyword = "";
        if (question.Contains("刺猬") || question.ToLower().Contains("hedgehog")) 
            currentModelKeyword = "Hedgehog"; 
        else if (question.Contains("机械鸟") || question.ToLower().Contains("bird")) 
            currentModelKeyword = "MechBird";
        else if (question.Contains("茶杯") || question.ToLower().Contains("teacup")) 
            currentModelKeyword = "teacup";

        // 打断“思考中”气泡
        if (currentThinkingBubble != null)
        {
            TMP_Text text = currentThinkingBubble.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = "<color=#FF6E5A>[已中断]</color>";
            currentThinkingBubble = null;
        }

        uiManager.StopAllTypewriters();
        if (voicePlayer != null) voicePlayer.StopVoice(); 

        if (sendCoroutine != null)
        {
            StopCoroutine(sendCoroutine);
        }

        sendCoroutine = StartCoroutine(SpawnBubbleNextFrame(question));
    }

    IEnumerator SpawnBubbleNextFrame(string question)
    {
        yield return null;
        uiManager.AddUserBubble(question);
        StartCoroutine(SendAndThink(question));
    }

    IEnumerator SendAndThink(string question)
    {
        yield return new WaitForSeconds(0.8f);
        currentThinkingBubble = uiManager.AddAIBubble("思考中");
        StartCoroutine(uiManager.AnimateThinking(currentThinkingBubble));
        thinkingStartTime = Time.time;
        agentClient.SendMessageToAI(question);
    }

    // 接收后端的三个参数：reply, action, model_name
    void HandleAIResponse(string reply, string action = "chat", string model_name = "")
    {
        bool hasVoice = (voiceToggle != null && voiceToggle.isOn && voicePlayer != null);

                // --- 1. 搜图任务 ---
        if (action == "download_image")
        {
            string[] parts = reply.Split(new string[] { "|PATH|" }, System.StringSplitOptions.None);
            
            if (parts.Length == 2)
            {
                string textToShow = parts[0].Trim(); 
                string serverImagePath = parts[1].Trim();  

                pendingReply = textToShow;

                if (hasVoice) voicePlayer.RequestVoice(textToShow);
                else
                {
                    if (voicePlayer != null) voicePlayer.StopVoice();
                    StartTypewriter(textToShow);
                }

                if (currentImageLoadCoroutine != null) StopCoroutine(currentImageLoadCoroutine);

                // 【导演核心】如果用户是要建模型，我们直接展示本地准备好的完美图片！
                if (!string.IsNullOrEmpty(currentModelKeyword))
                {
                    // 根据模型名字找对应的本地图片名字
                    string localImageName = "";
                    if (currentModelKeyword == "MechBird") localImageName = "mechbird";
                    else if (currentModelKeyword == "Hedgehog") localImageName = "hedgehog";
                    else if (currentModelKeyword == "teacup") localImageName = "teacup";

                    // 加载本地图片
                    Sprite localSprite = Resources.Load<Sprite>($"AgentImages/{localImageName}");
                    if (localSprite != null)
                    {
                        // 强行替换 UI 上的图片
                        displayImage.sprite = localSprite;
                        displayImage.gameObject.SetActive(true);
                        if (uiManager != null) StartCoroutine(uiManager.PopAnimation(displayImage.gameObject));
                        
                        Debug.Log($"🎬 [导演模式] 已使用本地完美图片替换：{localImageName}");
                    }
                    else
                    {
                        Debug.LogError($"❌ 找不到本地图片：Resources/AgentImages/{localImageName}");
                        // 如果本地没找到，就老实显示后端下载的图
                        currentImageLoadCoroutine = StartCoroutine(LoadLocalImage(serverImagePath));
                    }

                    // 触发后续的自动建模流程
                    StartCoroutine(AutoTriggerModelGeneration());
                }
                else
                {
                    // 如果只是单纯搜图，就正常显示后端下载的图片
                    currentImageLoadCoroutine = StartCoroutine(LoadLocalImage(serverImagePath));
                }
            }
        }
        // --- 2. 3D 建模任务 ---
        else if (action == "generate_3d")
        {
            pendingReply = reply;

            if (hasVoice) voicePlayer.RequestVoice(reply);
            else
            {
                if (voicePlayer != null) voicePlayer.StopVoice();
                StartTypewriter(reply);
            }

            if (modelSpawner != null)
            {
                string prefabToLoad = "Hedgehog"; 
                if (model_name.Contains("机械鸟") || model_name.ToLower().Contains("bird"))
                    prefabToLoad = "MechBird";
                else if (model_name.Contains("刺猬") || model_name.ToLower().Contains("hedgehog"))
                    prefabToLoad = "Hedgehog";

                modelSpawner.SpawnModel(prefabToLoad); 
            }
        }
        // --- 3. 普通聊天 ---
        else
        {
            pendingReply = reply;
            if (hasVoice) voicePlayer.RequestVoice(reply);
            else
            {
                if (voicePlayer != null) voicePlayer.StopVoice();
                StartTypewriter(reply);
            }
        }
    }

    void HandleVoiceReady(AudioClip clip)
    {
        if (voiceToggle != null && !voiceToggle.isOn)
        {
            StartTypewriter(pendingReply);
            return;
        }
        voicePlayer.PlayClip(clip);
        StartTypewriter(pendingReply);
    }

    void StartTypewriter(string reply)
    {
        bool hasVoice = (voiceToggle != null && voiceToggle.isOn);
        float currentSpeed = hasVoice ? typingSpeedWithVoice : typingSpeedNoVoice;

        if (currentThinkingBubble != null)
        {
            uiManager.StartTypewriterOnBubble(currentThinkingBubble, reply, currentSpeed);
            currentThinkingBubble = null;
        }
    }

    void HandleAIError(string errorMsg)
    {
        if (currentThinkingBubble != null)
        {
            TMP_Text text = currentThinkingBubble.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = "<color=#FF6E5A>" + errorMsg + "</color>";
            currentThinkingBubble = null;
        }
        else
        {
            uiManager.AddErrorBubble(errorMsg);
        }
    }

    IEnumerator LoadLocalImage(string path)
    {
        yield return new WaitForSeconds(0.5f);
        string fileUrl = "file:///" + path.Replace("\\", "/");

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(fileUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                displayImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                AspectRatioFitter fitter = displayImage.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                {
                    fitter = displayImage.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                }
                fitter.aspectRatio = (float)texture.width / texture.height;

                displayImage.gameObject.SetActive(true);

                if (uiManager != null) StartCoroutine(uiManager.PopAnimation(displayImage.gameObject));

                Debug.Log("✅ 图片加载成功并显示！");
            }
            else
            {
                Debug.LogError("❌ 加载图片失败: " + uwr.error);
            }
        }
    }

    IEnumerator ToggleParentWithAnimation(bool isOn)
    {
        if (isOn)
        {
            targetParent.SetActive(true);
            if (uiManager != null) yield return StartCoroutine(uiManager.PopAnimation(targetParent));
        }
        else
        {
            float timer = 0f;
            float duration = 0.15f;
            Vector3 startScale = targetParent.transform.localScale;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                targetParent.transform.localScale = Vector3.Lerp(startScale, new Vector3(0.01f, 0.01f, 0.01f), timer / duration);
                yield return null;
            }

            targetParent.SetActive(false);
            targetParent.transform.localScale = Vector3.one;
        }
    }

    IEnumerator AutoTriggerModelGeneration()
    {
        if (string.IsNullOrEmpty(currentModelKeyword)) yield break;

        // 1. 等语音播完（如果你觉得语音慢，也可以把这部分全删了，让它立马弹）
        AudioSource audio = voicePlayer.GetComponent<AudioSource>();
        if (audio != null && audio.isPlaying)
        {
            while (audio.isPlaying) yield return null;
            yield return new WaitForSeconds(0.3f);
        }

        // 2. 唤起 Blender
        currentThinkingBubble = uiManager.AddAIBubble("Blender 处理中...");
        Debug.Log("==================================================");
        Debug.Log("🛠️ 正在唤起 Blender...");
        
        // 【更新】你的新 Blender 路径
        string blenderPath = @"Z:\Program Files\Blender Foundation\Blender 5.3\blender-launcher.exe"; 
        string baseDir = @"E:\AI\AgentProject\backend\agent_tools\blender_files";
        string blendFile = System.IO.Path.Combine(baseDir, currentModelKeyword == "MechBird" ? "mock_mechbird.blend" : "mock_hedgehog.blend");

        try
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = blenderPath;
            startInfo.Arguments = $"\"{blendFile}\"";
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception e) { Debug.LogError("⚠️ 启动 Blender 失败: " + e.Message); }

        // =========================================================
        // 第一阶段：等你按【数字键 1】
        // =========================================================
        TMP_Text bubbleText = currentThinkingBubble.GetComponentInChildren<TMP_Text>();
        if (bubbleText != null) bubbleText.text = "Blender 处理中...";

        while (!Input.GetKeyDown(KeyCode.Alpha1))
        {
            yield return null;
        }

        // --- 你按下 1 之后，立刻无缝切换文字，不等待！---
        Debug.Log("📦 接收到 Blender 导出信号，正在导入 Unity 资源库...");
        if (bubbleText != null) bubbleText.text = "正在接收 Blender 导出的 FBX 并导入 Unity...";

        // =========================================================
        // 第二阶段：等你按【数字键 2】
        // =========================================================
        while (!Input.GetKeyDown(KeyCode.Alpha2))
        {
            yield return null;
        }

        // --- 你按下 2 之后，触发最终召唤 ---
        Debug.Log("✅ 流水线执行完毕！");
        Debug.Log("==================================================");

        if (currentThinkingBubble != null)
        {
            string finalReply = "3D 模型已生成并导入场景！";
            if (voiceToggle != null && voiceToggle.isOn && voicePlayer != null)
                voicePlayer.RequestVoice(finalReply);
            else
                StartTypewriter(finalReply);

            if (modelSpawner != null)
            {
                modelSpawner.SpawnModel(currentModelKeyword);
            }
            
            currentThinkingBubble = null;
        }
    }
}