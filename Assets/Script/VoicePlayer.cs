using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class VoicePlayer : MonoBehaviour
{
    [Header("音频源设置")]
    [Tooltip("挂载了 AudioSource 组件的物体（用于播放语音）")]
    public AudioSource audioSource;

    public AvatarAnimator avatarAnim;

    // 事件：当音频下载并准备好时，通知外部（ChatController）
    public event Action<AudioClip> OnVoiceReady;
    
    // 事件：当音频出错时，通知外部
    public event Action<string> OnVoiceFailed;

    // 用于监控当前播放状态的协程
    private Coroutine checkPlayingCoroutine;

    void Start()
    {
        // 如果面板没有拖入，自动获取自身身上的 AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // ================= 供外部调用：请求语音生成 =================
    public void RequestVoice(string text)
    {
        StartCoroutine(RequestVoiceCoroutine(text));
    }

    // ================= WIndexTTS 语音请求核心逻辑 =================
    IEnumerator RequestVoiceCoroutine(string text)
    {
        TTSData data = new TTSData();
        data.model = "windextts";
        data.input = text;
        data.voice = "my_voice"; // 确保这个对应你 voices 文件夹里的音频名
        
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // 注意：这里请求的是 WIndexTTS 的接口（8001端口），直接返回音频流
        string url = "http://127.0.0.1:8001/v1/audio/speech";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            // 关键点：指定接收器为 AudioClip，直接解析 WAV 流
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV); 
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                Debug.Log("✅ 音频获取成功！通知外部播放");
                // 通知 ChatController 音频好了，可以开始音画同步了
                OnVoiceReady?.Invoke(clip); 
            }
            else
            {
                Debug.LogError("❌ TTS 请求失败: " + request.error);
                OnVoiceFailed?.Invoke(request.error);
            }
        }
    }

    // ================= 供外部调用：真正播放音频 =================
    public void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            // 1. 强制设置音频参数，确保能正常发声
            audioSource.clip = clip;
            audioSource.volume = 1f;
            audioSource.mute = false;
            audioSource.spatialBlend = 0f; // 强制 2D 声音
            
            // 2. 播放声音
            audioSource.Play();

            avatarAnim?.StartTalking();

            // 3. 启动监控协程，用于播完后清理内存
            if (checkPlayingCoroutine != null) 
            {
                StopCoroutine(checkPlayingCoroutine);
            }
            checkPlayingCoroutine = StartCoroutine(DestroyClipAfterPlay());
        }
    }

    // 监控播放完毕并释放内存
    IEnumerator DestroyClipAfterPlay()
    {
        // 等待音频播放完毕
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        if (avatarAnim != null)avatarAnim?.StopTalking();
        
        // 播完了，销毁 AudioClip 释放 RAM
        if (audioSource.clip != null)
        {
            Destroy(audioSource.clip);
            audioSource.clip = null;
        }
        checkPlayingCoroutine = null;
    }

    // ================= 供外部调用：打断时停止播放 =================
    public void StopVoice()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();

            if (avatarAnim != null)avatarAnim?.StopTalking();
            
            // 打断时也立刻销毁内存中的音频
            if (audioSource.clip != null)
            {
                Destroy(audioSource.clip);
                audioSource.clip = null;
            }
        }
    }

    // ================= 数据结构 =================
    [System.Serializable]
    public class TTSData 
    { 
        public string model;
        public string input;
        public string voice;
    }
}