using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using DG.Tweening;

public enum UIAnimationType
{
    None,
    Fade,
    Scale
}

public class UIManager : MonoBehaviour
{
    // 单例模式，方便在游戏中的其他脚本在"正确的时间"调用显示和隐藏功能
    public static UIManager Instance { get; private set; }

    [Header("UI Animation Settings / UI动画设置")]
    public UIAnimationType animationType = UIAnimationType.Fade;
    public float animationDuration = 0.5f;
    [Tooltip("UI出现的缓动效果")]
    public Ease showEase = Ease.OutBack;
    [Tooltip("UI消失的缓动效果")]
    public Ease hideEase = Ease.InBack;

    [Header("UI Images / 对话面板 (点击不发广播)")]
    public GameObject dialogue1;
    public GameObject dialogue2;

    [Header("对话1 选项 (点击发送广播)")]
    public GameObject dialogue1OptionCorrect;
    public GameObject dialogue1OptionIncorrect;

    [Header("对话2 选项 (点击发送广播)")]
    public GameObject dialogue2OptionCorrect;
    public GameObject dialogue2OptionIncorrect;

    [Header("Game Over UI / 游戏结束界面")]
    public GameObject gameOverUI;

    // 广播事件：参数1为对话编号（如 1 代表对话1），参数2为是否正确（true为正确，false为错误）
    public event Action<int, bool> OnDialogueOptionClicked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 为普通 Image 添加点击事件监听
        AddClickListenerToGameObject(dialogue1OptionCorrect, 1, true);
        AddClickListenerToGameObject(dialogue1OptionIncorrect, 1, false);
        
        AddClickListenerToGameObject(dialogue2OptionCorrect, 2, true);
        AddClickListenerToGameObject(dialogue2OptionIncorrect, 2, false);

        // 初始化UI状态
        InitializeUI();
    }

    /// <summary>
    /// 初始化UI显示状态，根据规则：
    /// Dialogue1默认出现，对话1选项延迟1秒出现
    /// Dialogue2及选项默认隐藏
    /// </summary>
    private void InitializeUI()
    {
        // 隐藏游戏结束界面
        HideUI(gameOverUI, true);

        // 隐藏所有对话2相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue2, true);
        SetGameObjectActive(dialogue2OptionCorrect, false, true);
        SetGameObjectActive(dialogue2OptionIncorrect, false, true);

        // 隐藏对话1的选项 (瞬间隐藏，不要动画，作为初始状态)
        SetGameObjectActive(dialogue1OptionCorrect, false, true);
        SetGameObjectActive(dialogue1OptionIncorrect, false, true);

        // 为了让对话1在开始时也播放出现动画，先瞬间隐藏它，再调用普通的 ShowUI
        HideUI(dialogue1, true);
        
        // 播放对话1出现的动画
        ShowUI(dialogue1, false);

        // 延迟显示对话1的选项
        // 根据是否有主对话框的动画，适当增加延迟时间
        float optionDelay = 1f + (animationType == UIAnimationType.None ? 0f : animationDuration);
        StartCoroutine(ShowOptionsDelayed(1, optionDelay));
    }

    /// <summary>
    /// 延迟显示指定对话的选项
    /// </summary>
    private IEnumerator ShowOptionsDelayed(int dialogueIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (dialogueIndex == 1)
        {
            // 只有当对话框仍然激活时，才显示选项，防止在快速点击或重复调用时选项错误出现
            if (dialogue1 != null && dialogue1.activeSelf)
            {
                SetGameObjectActive(dialogue1OptionCorrect, true);
                SetGameObjectActive(dialogue1OptionIncorrect, true);
            }
        }
        else if (dialogueIndex == 2)
        {
            if (dialogue2 != null && dialogue2.activeSelf)
            {
                SetGameObjectActive(dialogue2OptionCorrect, true);
                SetGameObjectActive(dialogue2OptionIncorrect, true);
            }
        }
    }

    /// <summary>
    /// 辅助方法：安全地设置GameObject的激活状态
    /// </summary>
    private void SetGameObjectActive(GameObject obj, bool isActive, bool instant = false)
    {
        if (obj != null)
        {
            if (isActive)
            {
                ShowUI(obj, instant);
            }
            else
            {
                HideUI(obj, instant);
            }
        }
    }

    /// <summary>
    /// 为指定的 GameObject 添加 EventTrigger 以实现点击监听
    /// </summary>
    private void AddClickListenerToGameObject(GameObject obj, int dialogueIndex, bool isCorrect)
    {
        if (obj == null) return;

        // 尝试获取 Image 组件以确保它可以接收射线检测
        Image img = obj.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        // 获取或添加 EventTrigger 组件
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = obj.AddComponent<EventTrigger>();
        }

        // 创建 PointerClick 事件
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { HandleOptionClick(dialogueIndex, isCorrect); });
        
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 处理选项点击，并进行广播
    /// </summary>
    public void HandleOptionClick(int dialogueIndex, bool isCorrect)
    {
        // 1. 隐藏当前对话框及其选项
        HideDialogue(dialogueIndex);
        if (dialogueIndex == 1)
        {
            SetGameObjectActive(dialogue1OptionCorrect, false);
            SetGameObjectActive(dialogue1OptionIncorrect, false);
        }
        else if (dialogueIndex == 2)
        {
            SetGameObjectActive(dialogue2OptionCorrect, false);
            SetGameObjectActive(dialogue2OptionIncorrect, false);
        }

        // 2. 广播玩家点击了哪个对话的正确/错误选项
        // 将广播放到 UI 更新之后，防止外部脚本报错中断 UI 逻辑
        try
        {
            OnDialogueOptionClicked?.Invoke(dialogueIndex, isCorrect);
        }
        catch (Exception e)
        {
            Debug.LogError($"Broadcast OnDialogueOptionClicked failed: {e.Message}\n{e.StackTrace}");
        }

        // 3. 结果处理：正确则推进流程，错误则显示Game Over
        float delay = animationType == UIAnimationType.None ? 0f : animationDuration;
        
        if (isCorrect)
        {
            // 规则：如果点击了对话1的【正确】选项，则显示对话2
            if (dialogueIndex == 1)
            {
                // 延迟等待隐藏动画播完再显示下一个，或者直接显示
                StartCoroutine(ShowNextDialogueDelayed(2, delay));
            }
        }
        else
        {
            // 如果选择错误，显示游戏结束界面
            StartCoroutine(ShowGameOverDelayed(delay));
        }
    }

    private IEnumerator ShowNextDialogueDelayed(int dialogueIndex, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);
            
        ShowDialogue(dialogueIndex);
    }

    private IEnumerator ShowGameOverDelayed(float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);
            
        if (gameOverUI != null)
        {
            ShowUI(gameOverUI);
        }
        else
        {
            Debug.LogWarning("UIManager: 未分配 Game Over UI！");
        }
    }

    /// <summary>
    /// 在正确的时间调用此方法显示对应的UI对话
    /// </summary>
    public void ShowDialogue(int dialogueIndex)
    {
        float optionDelay = 1f + (animationType == UIAnimationType.None ? 0f : animationDuration);

        if (dialogueIndex == 1 && dialogue1 != null)
        {
            ShowUI(dialogue1);
            // 每次重新显示对话1时，选项先瞬间隐藏，再走延迟动画逻辑
            SetGameObjectActive(dialogue1OptionCorrect, false, true);
            SetGameObjectActive(dialogue1OptionIncorrect, false, true);
            StartCoroutine(ShowOptionsDelayed(1, optionDelay));
        }
        else if (dialogueIndex == 2 && dialogue2 != null)
        {
            ShowUI(dialogue2);
            // 显示对话2时，选项先瞬间隐藏，再延迟出现
            SetGameObjectActive(dialogue2OptionCorrect, false, true);
            SetGameObjectActive(dialogue2OptionIncorrect, false, true);
            StartCoroutine(ShowOptionsDelayed(2, optionDelay));
        }
    }

    /// <summary>
    /// 隐藏指定编号的对话
    /// </summary>
    public void HideDialogue(int dialogueIndex)
    {
        if (dialogueIndex == 1 && dialogue1 != null)
        {
            HideUI(dialogue1);
        }
        else if (dialogueIndex == 2 && dialogue2 != null)
        {
            HideUI(dialogue2);
        }
    }

    /// <summary>
    /// 隐藏所有对话
    /// </summary>
    public void HideAllDialogues()
    {
        HideUI(dialogue1);
        HideUI(dialogue2);
        SetGameObjectActive(dialogue1OptionCorrect, false);
        SetGameObjectActive(dialogue1OptionIncorrect, false);
        SetGameObjectActive(dialogue2OptionCorrect, false);
        SetGameObjectActive(dialogue2OptionIncorrect, false);
        HideUI(gameOverUI);
    }

    #region DOTween Animation Helpers

    private void ShowUI(GameObject uiObject, bool instant = false)
    {
        if (uiObject == null) return;
        
        // 杀掉该物体上的所有Tween，防止动画冲突
        uiObject.transform.DOKill();
        CanvasGroup cg = uiObject.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();

        uiObject.SetActive(true);

        // 确保显示时能够交互
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        if (instant || animationType == UIAnimationType.None)
        {
            if (cg != null) cg.alpha = 1f;
            uiObject.transform.localScale = Vector3.one;
            return;
        }

        switch (animationType)
        {
            case UIAnimationType.Fade:
                if (cg == null) cg = uiObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = true;
                cg.interactable = true;
                cg.alpha = 0f;
                cg.DOFade(1f, animationDuration).SetEase(showEase);
                break;
            case UIAnimationType.Scale:
                uiObject.transform.localScale = Vector3.zero;
                uiObject.transform.DOScale(Vector3.one, animationDuration).SetEase(showEase);
                break;
        }
    }

    private void HideUI(GameObject uiObject, bool instant = false)
    {
        if (uiObject == null || !uiObject.activeSelf) return;

        // 杀掉该物体上的所有Tween，防止动画冲突
        uiObject.transform.DOKill();
        CanvasGroup cg = uiObject.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();

        // 隐藏时立即禁用交互
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if (instant || animationType == UIAnimationType.None)
        {
            uiObject.SetActive(false);
            return;
        }

        switch (animationType)
        {
            case UIAnimationType.Fade:
                if (cg == null) cg = uiObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
                cg.DOFade(0f, animationDuration).SetEase(hideEase).OnComplete(() => uiObject.SetActive(false));
                break;
            case UIAnimationType.Scale:
                uiObject.transform.DOScale(Vector3.zero, animationDuration).SetEase(hideEase).OnComplete(() => uiObject.SetActive(false));
                break;
        }
    }

    #endregion
}
