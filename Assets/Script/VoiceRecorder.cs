using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using UnityEngine.UI;

public class VoiceRecorder : MonoBehaviour
{
    public ChatUIManager uiManager; // 用于把识别出的文字填入输入框

        [Header("视觉反馈")]
    public Image buttonBg;    // 拖入你的按钮底图
    public Image buttonIcon;  // 拖入你的麦克风图标
    public Color normalBgColor = new Color(1f, 1f, 1f, 0.2f); // 正常状态底色(半透明白)
    public Color pressedBgColor = new Color(1f, 0f, 0f, 0.5f); // 按住时底色(红色)
    public Color normalIconColor = Color.white; // 正常图标颜色
    public Color pressedIconColor = new Color(1f, 0.8f, 0.8f, 1f); // 按住图标颜色
    private AudioClip recordedClip;
    private string microphoneName;
    private bool isRecording = false;

    void Start()
    {
        if (Microphone.devices.Length > 0)
            microphoneName = Microphone.devices[0];

        // 初始化颜色
        SetVisualState(false);
    }

    // 绑定到 UI 的“按住说话”按钮的 PointerDown
    public void StartRecording()
    {
        if (isRecording || string.IsNullOrEmpty(microphoneName)) return;
        isRecording = true;

        SetVisualState(true);

        // 录制 10 秒，44100 采样率
        recordedClip = Microphone.Start(microphoneName, false, 10, 44100);
        Debug.Log("开始录音...");
    }

    // 绑定到 UI 的“按住说话”按钮的 PointerUp
    public void StopRecordingAndSend()
    {
        if (!isRecording) return;
        isRecording = false;
        
        // 恢复颜色
        SetVisualState(false);

        int lastPos = Microphone.GetPosition(microphoneName);
        Microphone.End(microphoneName);

        // 1. 拦截极短的误触（比如小于 0.5 秒）
        if (lastPos <= 22050) 
        {
            Debug.Log("录音时间太短，忽略");
            return; 
        }

        // 2. 裁剪音频
        float[] soundData = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(soundData, 0);
        float[] trimmedData = new float[lastPos * recordedClip.channels];
        System.Array.Copy(soundData, trimmedData, trimmedData.Length);

        // 3. 计算最大音量，拦截静音
        float maxVolume = 0f;
        foreach (float s in trimmedData)
        {
            if (Mathf.Abs(s) > maxVolume) maxVolume = Mathf.Abs(s);
        }
        if (maxVolume < 0.02f) // 0.02 是经验阈值，可根据麦克风灵敏度微调
        {
            Debug.Log("没有检测到说话声音，忽略");
            return;
        }

        // 4. 生成新的 AudioClip 并发送
        AudioClip trimmedClip = AudioClip.Create("Trimmed", lastPos, recordedClip.channels, 44100, false);
        trimmedClip.SetData(trimmedData, 0);

        StartCoroutine(SaveAndSendVoice(trimmedClip));
    }

    // 控制颜色切换的方法
    private void SetVisualState(bool isPressed)
    {
        if (buttonBg != null)
            buttonBg.color = isPressed ? pressedBgColor : normalBgColor;
        
        if (buttonIcon != null)
            buttonIcon.color = isPressed ? pressedIconColor : normalIconColor;
    }

    IEnumerator SaveAndSendVoice(AudioClip clip)
    {
        // 1. 指定存储路径为 Assets/RecordedAudio 文件夹
        string folderPath = Path.Combine(Application.dataPath, "RecordedAudio");
        
        // 如果文件夹不存在，自动创建它
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 2. 拼接完整的文件路径
        string filePath = Path.Combine(folderPath, "record.wav");
        
        // 3. 存盘
        SavWav.Save(filePath, clip); 

        // 4. 读取文件并打包成表单发给后端
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, "record.wav", "audio/wav");

        // 5. 发送给 8002 端口的 STT 服务
        using (UnityWebRequest request = UnityWebRequest.Post("http://127.0.0.1:8002/voice_to_text", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 解析返回的 JSON
                VoiceResponse response = JsonUtility.FromJson<VoiceResponse>(request.downloadHandler.text);
                
                // 把识别到的文字填入输入框，并激活光标方便直接回车发送
                uiManager.InputField.text = response.text;
                uiManager.InputField.ActivateInputField();
                
                Debug.Log("识别成功: " + response.text);
            }
            else
            {
                Debug.LogError("语音识别失败: " + request.error);
            }
        }

        yield return new WaitForSeconds(0.5f); 

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log("录音文件已清理");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("清理录音文件失败: " + e.Message);
            }
        }

        // 销毁内存中的 AudioClip 释放 RAM
        Destroy(clip); 
    }

    [System.Serializable]
    public class VoiceResponse
    {
        public string text;
    }
}