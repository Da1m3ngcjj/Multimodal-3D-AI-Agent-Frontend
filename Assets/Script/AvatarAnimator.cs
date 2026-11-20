using UnityEngine;

public class AvatarAnimator : MonoBehaviour
{
    private Animator animator;
    private int lastRandomIndex = 0;

    [Header("过渡时间")]
    public float transitionToTalk = 0.2f; 
    public float transitionToIdle = 0.2f; 

    [Header("高度补偿 (脚陷进去就调大这个值)")]
    public float talkYOffset = 0.08f; 
    
    private Vector3 originalPos;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // 记录模型原本完美的位置
        originalPos = transform.localPosition; 

        if (animator != null)
        {
            animator.CrossFade("Idle", 0.1f);
        }
    }

    public void StartTalking()
    {
        if (animator == null) return;

        // 【瞬间抬高】配合动画切换，直接拔高，不做 Lerp 渐变
        transform.localPosition = originalPos + new Vector3(0, talkYOffset, 0);

        int randomIndex = Random.Range(1, 3);
        if (randomIndex == lastRandomIndex)
        {
            randomIndex = (randomIndex == 1) ? 2 : 1;
        }
        lastRandomIndex = randomIndex;

        string targetAnim = (randomIndex == 1) ? "Talk1" : "Talk2";
        animator.CrossFade(targetAnim, transitionToTalk);
    }

    public void StopTalking()
    {
        if (animator == null) return;
        
        // 【瞬间落回】说完话瞬间回到原本的位置
        transform.localPosition = originalPos;

        animator.CrossFade("Idle01", transitionToIdle);
    }
}