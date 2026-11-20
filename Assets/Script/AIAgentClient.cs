using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class AIAgentClient : MonoBehaviour
{
    // 你的 Python 后端地址
    private string apiUrl = "http://127.0.0.1:8000/chat";

    // 【修改】事件传递三个参数：回复内容、动作类型、模型名称
    public event Action<string, string, string> OnResponseReceived;
    
    // 发生错误时的通知
    public event Action<string> OnErrorOccurred;

    // 提供给外部的调用接口
    public void SendMessageToAI(string question)
    {
        StartCoroutine(SendRequest(question));
    }

    IEnumerator SendRequest(string question)
    {
        ChatData data = new ChatData();
        data.message = question;
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ResponseData response = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
                
                // 【修改】触发成功事件，把 reply, action, model_name 三个参数都传出去
                OnResponseReceived?.Invoke(response.reply, response.action, response.model_name); 
            }
            else
            {
                // 触发失败事件
                OnErrorOccurred?.Invoke("网络请求失败: " + request.error);
            }
        }
    }

    [System.Serializable]
    public class ChatData { public string message; }
    
    [System.Serializable]
    public class ResponseData 
    { 
        public string reply; 
        public string action; 
        public string model_name; // 【新增】接收后端传来的模型名称
    }
}