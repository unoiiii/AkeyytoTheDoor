using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("摄像机移动设置")]
    [Tooltip("摄像机的移动速度 (单位/秒)")]
    public float moveSpeed = 5f;
    [Tooltip("摄像机的旋转速度 (度/秒)")]
    public float rotationSpeed = 90f;
    [Tooltip("移动的缓动效果")]
    public Ease moveEase = Ease.InOutQuad;

    [Header("目标位置 (对应正确选项)")]
    [Tooltip("玩家点击对话1正确选项时，摄像机移动到的位置")]
    public Transform dialogue1CorrectPosition;
    
    [Tooltip("玩家点击对话2正确选项时，摄像机移动到的位置")]
    public Transform dialogue2CorrectPosition;

    [Tooltip("玩家点击对话3正确选项时，摄像机移动到的位置")]
    public Transform dialogue3CorrectPosition;

    [Tooltip("玩家点击对话6正确选项时，摄像机移动到的位置")]
    public Transform dialogue6CorrectPosition;

    [Tooltip("玩家点击对话7正确选项时，摄像机移动到的位置")]
    public Transform dialogue7CorrectPosition;

    [Tooltip("玩家点击对话8时，摄像机移动到的位置")]
    public Transform dialogue8Position;

    private void Start()
    {
        // 监听 UIManager 的广播事件
        // 确保 UIManager.Instance 存在后再进行监听，避免空引用异常
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnDialogueOptionClicked += HandleDialogueOptionClicked;
            UIManager.Instance.OnDialogueShown += HandleDialogueShown;
            UIManager.Instance.OnDialogueClicked += HandleDialogueClicked;
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
            UIManager.Instance.OnDialogueShown -= HandleDialogueShown;
            UIManager.Instance.OnDialogueClicked -= HandleDialogueClicked;
        }
    }

    private void HandleDialogueShown(int dialogueIndex)
    {
        // 对话5显示之后，摄像机再进行移动到对话3正确选项对应位置
        if (dialogueIndex == 5)
        {
            if (dialogue3CorrectPosition != null)
            {
                MoveCameraTo(dialogue3CorrectPosition);
            }
            else
            {
                Debug.LogWarning("CameraController: 未分配 对话3 的正确位置 (dialogue3CorrectPosition)！");
            }
        }
        // 注意：对话9和对话10显示时，摄像机保持在对话8的位置不动，无需添加移动逻辑
    }

    private IEnumerator MoveCameraAndShowDialogue9()
    {
        if (dialogue8Position != null)
        {
            // 移动摄像机并获取移动所需时间
            float moveDuration = MoveCameraTo(dialogue8Position);
            
            // 等待摄像机移动完成，再额外延迟1秒
            yield return new WaitForSeconds(moveDuration + 1f);

            // 显示对话9
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(9);
            }
        }
        else
        {
            Debug.LogWarning("CameraController: 未分配 对话8 的位置 (dialogue8Position)！");
        }
    }

    private void HandleDialogueClicked(int dialogueIndex)
    {
        if (dialogueIndex == 8)
        {
            StartCoroutine(MoveCameraAndShowDialogue9());
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
                case 3:
                    // 移除对话3点击后的摄像机移动逻辑，改为在对话5显示后移动
                    break;
                case 6:
                    if (dialogue6CorrectPosition != null)
                        targetTransform = dialogue6CorrectPosition;
                    else
                        Debug.LogWarning("CameraController: 未分配 对话6 的正确位置 (dialogue6CorrectPosition)！");
                    break;
                case 7:
                    // 对话7选项正确后，摄像机不在此处移动（由对话8显示后的逻辑接管），移除原有的对话7移动摄像机逻辑
                    break;
                default:
                    break;
            }

            if (targetTransform != null)
            {
                MoveCameraTo(targetTransform);
            }
        }
    }

    /// <summary>
    /// 平滑移动摄像机到目标位置，并返回移动所需的总时间
    /// </summary>
    private float MoveCameraTo(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            // 停止之前的动画，防止冲突
            transform.DOKill();
            
            // 计算移动和旋转需要的时间
            float dist = Vector3.Distance(transform.position, targetTransform.position);
            float posDuration = moveSpeed > 0f ? dist / moveSpeed : 0.1f;

            float angle = Quaternion.Angle(transform.rotation, targetTransform.rotation);
            float rotDuration = rotationSpeed > 0f ? angle / rotationSpeed : 0.1f;

            // 为了让移动和旋转同步完成，取较大的时间作为动画的持续时间
            float maxDuration = Mathf.Max(posDuration, rotDuration);

            // 使用 DOTween 移动摄像机
            transform.DOMove(targetTransform.position, maxDuration).SetEase(moveEase);
            transform.DORotateQuaternion(targetTransform.rotation, maxDuration).SetEase(moveEase);
            
            return maxDuration;
        }
        return 0f;
    }

    private void Update()
    {
        // 测试功能：按下键盘的数字键2（主键盘或小键盘），强制触发摄像机移动到对话1正确位置
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (dialogue1CorrectPosition != null)
            {
                transform.DOKill();
                
                float dist = Vector3.Distance(transform.position, dialogue1CorrectPosition.position);
                float posDuration = moveSpeed > 0f ? dist / moveSpeed : 0.1f;

                float angle = Quaternion.Angle(transform.rotation, dialogue1CorrectPosition.rotation);
                float rotDuration = rotationSpeed > 0f ? angle / rotationSpeed : 0.1f;

                float maxDuration = Mathf.Max(posDuration, rotDuration);

                transform.DOMove(dialogue1CorrectPosition.position, maxDuration).SetEase(moveEase);
                transform.DORotateQuaternion(dialogue1CorrectPosition.rotation, maxDuration).SetEase(moveEase);
                Debug.Log("CameraController: 测试功能触发 -> 准备移动到对话1正确位置");
            }
            else
            {
                Debug.LogWarning("CameraController: 测试功能触发失败，未在Inspector中分配 dialogue1CorrectPosition！");
            }
        }
    }
}
