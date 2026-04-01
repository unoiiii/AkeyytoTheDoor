using UnityEngine;
using System;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("摄像机移动设置")]
    [Tooltip("摄像机的移动时间")]
    public float moveDuration = 1.5f;
    [Tooltip("移动的缓动效果")]
    public Ease moveEase = Ease.InOutQuad;

    [Header("目标位置 (对应正确选项)")]
    [Tooltip("玩家点击对话1正确选项时，摄像机移动到的位置")]
    public Transform dialogue1CorrectPosition;
    
    [Tooltip("玩家点击对话2正确选项时，摄像机移动到的位置")]
    public Transform dialogue2CorrectPosition;

    private void Start()
    {
        // 监听 UIManager2 的广播事件
        // 确保 UIManager2.Instance 存在后再进行监听，避免空引用异常
        if (UIManager2.Instance != null)
        {
            UIManager2.Instance.OnDialogueOptionClicked += HandleDialogueOptionClicked;
        }
        else
        {
            Debug.LogWarning("CameraController: 找不到 UIManager2.Instance，监听事件失败！");
        }
    }

    private void OnDestroy()
    {
        // 移除监听，防止内存泄漏或空引用异常
        if (UIManager2.Instance != null)
        {
            UIManager2.Instance.OnDialogueOptionClicked -= HandleDialogueOptionClicked;
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
            Transform targetTransform = null;
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

            if (targetTransform != null)
            {
                // 停止之前的动画，防止冲突
                transform.DOKill();
                // 使用 DOTween 移动摄像机
                transform.DOMove(targetTransform.position, moveDuration).SetEase(moveEase);
                transform.DORotateQuaternion(targetTransform.rotation, moveDuration).SetEase(moveEase);
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
                transform.DOKill();
                transform.DOMove(dialogue1CorrectPosition.position, moveDuration).SetEase(moveEase);
                transform.DORotateQuaternion(dialogue1CorrectPosition.rotation, moveDuration).SetEase(moveEase);
                Debug.Log("CameraController: 测试功能触发 -> 准备移动到对话1正确位置");
            }
            else
            {
                Debug.LogWarning("CameraController: 测试功能触发失败，未在Inspector中分配 dialogue1CorrectPosition！");
            }
        }
    }
}
