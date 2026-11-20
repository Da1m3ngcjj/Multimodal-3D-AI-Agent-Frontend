using UnityEngine;
using System.Collections;

public class AvatarModelSpawner : MonoBehaviour
{
    [Header("模型挂载点")]
    [Tooltip("拖入那个用来装生成的模型的父物体")]
    public Transform modelTargetParent; 
    
    [Header("出场动画")]
    public ChatUIManager uiManager;

    /// <summary>
    /// 供外部调用：从 Resources 加载并生成模型
    /// </summary>
    public void SpawnModel(string prefabName)
    {
        // 1. 从 Resources/Prefabs 文件夹下加载你准备好的模型
        GameObject generatedModel = Resources.Load<GameObject>($"Prefabs/{prefabName}");
        
        if (generatedModel != null)
        {
            // 2. 清理父物体下上一个残留的模型
            foreach (Transform child in modelTargetParent)
            {
                Destroy(child.gameObject);
            }

            // 3. 实例化新模型
            GameObject newModel = Instantiate(generatedModel, modelTargetParent);
            
            // 【新增】给新生成的模型动态挂载交互脚本
            if (newModel.GetComponent<ModelInteractor>() == null)
            {
                newModel.AddComponent<ModelInteractor>();
            }

            // 4. 播放 Q 弹的出场动画
            if (uiManager != null) 
            {
                StartCoroutine(uiManager.PopAnimation(modelTargetParent.gameObject));
            }
            
            Debug.Log($"✅ 成功生成并召唤模型: {prefabName}");
        }
        else
        {
            Debug.LogError($"❌ 找不到模型预制体：Prefabs/{prefabName}");
        }
    }
}