using UnityEngine;

public class CodeLipSync : MonoBehaviour
{
    [Header("基础设置")]
    public AudioSource audioSource;          
    public SkinnedMeshRenderer faceMesh;     

    [Header("灵敏度与平滑度")]
    public float sensitivity = 60f;          
    public float smoothSpeed = 15f;          

    [Header("张嘴最大幅度 (0-100)")]
    public float maxMouthOpen = 70f;
    public float maxJawOpen = 30f;

    [Header("15个标准视素映射 (按顺序 0-14)")]
    public int[] visemeIndices = new int[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

    private float currentVolume = 0f;
    private float[] samples = new float[256];
    private float[] currentWeights = new float[15];

    // 记录时间，用于生成正弦波
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.GetOutputData(samples, 0);
            float maxSample = 0f;
            foreach (float sample in samples)
            {
                if (Mathf.Abs(sample) > maxSample) maxSample = Mathf.Abs(sample);
            }

            float targetVolume = Mathf.Clamp(maxSample * sensitivity, 0f, 1f);
            currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * smoothSpeed);

            if (faceMesh != null)
            {
                ApplyVisemeWeights(currentVolume);
            }
        }
        else
        {
            currentVolume = Mathf.Lerp(currentVolume, 0f, Time.deltaTime * smoothSpeed);
            if (faceMesh != null)
            {
                ApplyVisemeWeights(currentVolume);
            }
        }
    }

    void ApplyVisemeWeights(float volume)
    {
        // 0. 闭嘴 (None) - 声音越大，闭嘴权重越小
        currentWeights[0] = 100f - (volume * 100f);

        // 1. 张开嘴 (Open) / 4. 圆嘴 (Tight_O) / 5. 咧嘴 (Wide)
        // 我们用不同的正弦波频率来让它们交替出现，产生说话时的嘴型变化
        float wave1 = (Mathf.Sin(timer * 12f) + 1f) * 0.5f; // 快速震荡
        float wave2 = (Mathf.Sin(timer * 8f) + 1f) * 0.5f;  // 慢速震荡

        // 主要开口
        currentWeights[1] = volume * maxMouthOpen * wave1;
        
        // 其他嘴型（压低权重，作为辅助混合，让嘴型不那么死板）
        currentWeights[4] = volume * 20f * wave2;   // O型嘴
        currentWeights[6] = volume * 15f * (1f - wave1); // 咧嘴
        currentWeights[2] = volume * 15f * wave2;   // 爆破嘴型

        // 舌头稍微动一动
        currentWeights[9] = volume * 10f * Mathf.PerlinNoise(timer * 5f, 0f);

        // 把权重应用到模型上
        for (int i = 0; i < 15; i++)
        {
            // 确保索引不越界
            if (i < visemeIndices.Length && visemeIndices[i] < faceMesh.sharedMesh.blendShapeCount)
            {
                faceMesh.SetBlendShapeWeight(visemeIndices[i], currentWeights[i]);
            }
        }

        // 额外驱动下巴 (Jaw_Open)，单独写在数组外，因为它是骨骼不是标准视素
        // 如果你之前截图里的 Jaw_Open 索引是 77，就取消下面这行的注释
        faceMesh.SetBlendShapeWeight(77, volume * maxJawOpen);
    }
}