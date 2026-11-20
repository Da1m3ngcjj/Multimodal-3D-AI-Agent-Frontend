using UnityEngine;
using UnityEngine.UI;

public class ToggleColorController : MonoBehaviour
{
    [Header("需要变色的 Image")]
    public Image backgroundImage; // 拖入 Toggle 的背景
    public Image iconImage;       // 拖入 Toggle 里的图标(Checkmark)

    [Header("开启时的颜色")]
    public Color bgColorOn = new Color(0.3f, 0.8f, 0.8f, 1f); // 青色
    public Color iconColorOn = Color.white;

    [Header("关闭时的颜色")]
    public Color bgColorOff = new Color(0.3f, 0.3f, 0.3f, 1f); // 深灰
    public Color iconColorOff = new Color(0.5f, 0.5f, 0.5f, 1f); // 暗灰

    private Toggle toggle;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            // 初始化颜色
            UpdateColor(toggle.isOn);
            // 监听 Toggle 的切换事件
            toggle.onValueChanged.AddListener(UpdateColor);
        }
    }

    private void UpdateColor(bool isOn)
    {
        if (backgroundImage != null)
            backgroundImage.color = isOn ? bgColorOn : bgColorOff;

        if (iconImage != null)
            iconImage.color = isOn ? iconColorOn : iconColorOff;
    }
}