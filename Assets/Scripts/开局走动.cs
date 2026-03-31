using UnityEngine;
using System.Collections;

public class 开局走动 : MonoBehaviour
{
    [Header("目标位置与旋转")]
    [Tooltip("你想让摄像机移动到的新位置和旋转方向，可以创建一个空的GameObject作为目标点拖入这里")]
    public Transform targetTransform;
    
    [Header("移动耗时（秒）")]
    [Tooltip("完成移动所需的秒数，可以随意调节")]
    public float moveDuration = 3.0f;

    private bool isMoving = false;

    void Update()
    {
        // 按下空格键触发测试，如果正在移动中则忽略
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
        {
            if (targetTransform != null)
            {
                StartCoroutine(MoveToTarget());
            }
            else
            {
                Debug.LogWarning("请在Inspector面板中赋值 targetTransform！");
            }
        }
    }

    private IEnumerator MoveToTarget()
    {
        isMoving = true;
        float elapsedTime = 0f;

        // 记录起点的位置和旋转
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // 计算0到1之间的进度比例
            float t = elapsedTime / moveDuration;
            
            // 使用 Mathf.SmoothStep 让移动起步和停止时更平滑，避免生硬的启动和停止
            t = Mathf.SmoothStep(0, 1, t);

            // 更新摄像机的位置和旋转
            transform.position = Vector3.Lerp(startPosition, targetTransform.position, t);
            transform.rotation = Quaternion.Lerp(startRotation, targetTransform.rotation, t);

            // 等待下一帧继续
            yield return null;
        }

        // 循环结束后，确保最终的位置和旋转与目标完全一致（消除浮点数误差）
        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
        
        isMoving = false;
    }
}
