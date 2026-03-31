using UnityEngine;
using System;

public class CameraController : MonoBehaviour
{
    [Header("摄像机移动设置")]
    [Tooltip("摄像机的移动速度")]
    public float moveSpeed = 5f;
    [Tooltip("摄像机的旋转速度")]
    public float rotationSpeed = 5f;

    [Header("目标位置 (对应正确选项)")]
    [Tooltip("玩家点击对话1正确选项时，摄像机移动到的位置")]
    public Transform dialogue1CorrectPosition;
    
    [Tooltip("玩家点击对话2正确选项时，摄像机移动到的位置")]
    public Transform dialogue2CorrectPosition;

    // 当前摄像机需要移动到的目标位置
    private Transform targetTransform;

    private void Start()
    {
        // 监听 UIManager 的广播事件
        // 确保 UIManager.Instance 存在后再进行监听，避免空引用异常
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnDialogueOptionClicked += HandleDialogueOptionClicked;
        }
        else
        {
            Debug.LogWarning("CameraController: 找不到 UIManager.Instance，监听事件失败！");
        }
    }

    private void OnDestroy()
    {
        // 移除监听，防止内存泄漏或空引用异常
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnDialogueOptionClicked -= HandleDialogueOptionClicked;
        }
    }

    /// <summary>
    /// 处理选项点击的广播事件
    /// </summary>
    /// <param name="dialogueIndex">对话编号</param>
    /// <param name="isCorrect">是否点击了正确的选项</param>
    private void HandleDialogueOptionClicked(int dialogueIndex, bool isCorrect)
    {
        // 如果玩家点击了正确的选项，根据对话编号设置对应的目标位置
        if (isCorrect)
        {
            switch (dialogueIndex)
            {
                case 1:
                    if (dialogue1CorrectPosition != null)
                        targetTransform = dialogue1CorrectPosition;
                    else
                        Debug.LogWarning("CameraController: 未分配 对话1 的正确位置 (dialogue1CorrectPosition)！");
                    break;
                case 2:
                    if (dialogue2CorrectPosition != null)
                        targetTransform = dialogue2CorrectPosition;
                    else
                        Debug.LogWarning("CameraController: 未分配 对话2 的正确位置 (dialogue2CorrectPosition)！");
                    break;
                default:
                    Debug.LogWarning($"CameraController: 未配置对话 {dialogueIndex} 的目标位置。");
                    break;
            }
        }
    }

    private void Update()
    {
        // 测试功能：按下键盘的数字键2（主键盘或小键盘），强制触发摄像机移动到对话1正确位置
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (dialogue1CorrectPosition != null)
            {
                targetTransform = dialogue1CorrectPosition;
                Debug.Log("CameraController: 测试功能触发 -> 准备移动到对话1正确位置");
            }
            else
            {
                Debug.LogWarning("CameraController: 测试功能触发失败，未在Inspector中分配 dialogue1CorrectPosition！");
            }
        }

        // 如果有目标位置，则平滑移动和旋转摄像机
        if (targetTransform != null)
        {
            // 使用 Vector3.Lerp 实现平滑的位置移动
            transform.position = Vector3.Lerp(transform.position, targetTransform.position, moveSpeed * Time.deltaTime);
            
            // 使用 Quaternion.Lerp 实现平滑的旋转过渡
            transform.rotation = Quaternion.Lerp(transform.rotation, targetTransform.rotation, rotationSpeed * Time.deltaTime);

            // 可选优化：如果摄像机已经非常接近目标位置和角度，则直接贴合目标并停止插值，节省性能
            if (Vector3.Distance(transform.position, targetTransform.position) < 0.001f &&
                Quaternion.Angle(transform.rotation, targetTransform.rotation) < 0.01f)
            {
                transform.position = targetTransform.position;
                transform.rotation = targetTransform.rotation;
                targetTransform = null; // 到达目标，清空目标变量
            }
        }
    }
}
