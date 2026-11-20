using UnityEngine;

public class ModelInteractor : MonoBehaviour
{
    [Header("旋转速度")]
    public float rotateSpeed = 20f;

    private bool isDragging = false;
    private bool isRotating = false;
    private Vector3 offset;
    private Vector3 lastMousePosition;

    void Update()
    {
        // 鼠标左键按下：准备拖动位置
        if (Input.GetMouseButtonDown(0))
        {
            if (CheckMouseOnModel())
            {
                isDragging = true;
                // 计算物体与鼠标的偏移量
                offset = transform.position - GetMouseWorldPos();
            }
        }
        // 鼠标右键按下：准备旋转
        else if (Input.GetMouseButtonDown(1))
        {
            if (CheckMouseOnModel())
            {
                isRotating = true;
                lastMousePosition = Input.mousePosition;
            }
        }

        // 鼠标抬起：停止所有交互
        if (Input.GetMouseButtonUp(0)) isDragging = false;
        if (Input.GetMouseButtonUp(1)) isRotating = false;

        // 拖动逻辑：左键按住，修改 X 和 Y 轴位置
        if (isDragging)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            // 我们只改变 X 和 Y，保持 Z 不变，防止模型跑到相机后面去
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }

        // 旋转逻辑：右键按住，修改 Y 轴旋转
        if (isRotating)
        {
            Vector3 deltaMouse = Input.mousePosition - lastMousePosition;
            // 鼠标往右移动，模型沿 Y 轴顺时针转（根据实际情况可调整正负号）
            float rotationY = -deltaMouse.x * rotateSpeed * Time.deltaTime;
            transform.Rotate(0, rotationY, 0, Space.World);
            
            lastMousePosition = Input.mousePosition;
        }
    }

    // 射线检测：判断鼠标是否点在了模型上
    private bool CheckMouseOnModel()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // 注意：模型需要有 Collider 才能被射线检测到
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    // 获取鼠标在 3D 世界中的位置（视口转世界）
    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        // 假设模型距离相机 10 个单位
        mousePoint.z = 10f; 
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}