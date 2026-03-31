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

    [Header("UI Images / 对话面板")]
    public GameObject dialogue1;
    public GameObject dialogue2;

    [Header("对话1 选项 (普通 Image)")]
    public Image dialogue1CorrectOption;
    public Image dialogue1IncorrectOption;

    [Header("对话2 选项 (如果有的话)")]
    public Image dialogue2CorrectOption;
    public Image dialogue2IncorrectOption;

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
        AddClickListenerToImage(dialogue1CorrectOption, 1, true);
        AddClickListenerToImage(dialogue1IncorrectOption, 1, false);
        
        AddClickListenerToImage(dialogue2CorrectOption, 2, true);
        AddClickListenerToImage(dialogue2IncorrectOption, 2, false);

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
        // 隐藏所有对话2相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue2, true);
        SetImageActive(dialogue2CorrectOption, false, true);
        SetImageActive(dialogue2IncorrectOption, false, true);

        // 隐藏对话1的选项 (瞬间隐藏，不要动画，作为初始状态)
        SetImageActive(dialogue1CorrectOption, false, true);
        SetImageActive(dialogue1IncorrectOption, false, true);

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
            SetImageActive(dialogue1CorrectOption, true);
            SetImageActive(dialogue1IncorrectOption, true);
        }
        else if (dialogueIndex == 2)
        {
            SetImageActive(dialogue2CorrectOption, true);
            SetImageActive(dialogue2IncorrectOption, true);
        }
    }

    /// <summary>
    /// 辅助方法：安全地设置Image的激活状态
    /// </summary>
    private void SetImageActive(Image img, bool isActive, bool instant = false)
    {
        if (img != null && img.gameObject != null)
        {
            if (isActive)
            {
                ShowUI(img.gameObject, instant);
            }
            else
            {
                HideUI(img.gameObject, instant);
            }
        }
    }

    /// <summary>
    /// 为指定的 Image 添加 EventTrigger 以实现点击监听
    /// </summary>
    private void AddClickListenerToImage(Image img, int dialogueIndex, bool isCorrect)
    {
        if (img == null) return;

        // 确保 Image 可以接收射线检测
        img.raycastTarget = true;

        // 获取或添加 EventTrigger 组件
        EventTrigger trigger = img.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = img.gameObject.AddComponent<EventTrigger>();
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
        // 1. 广播玩家点击了哪个对话的正确/错误选项
        OnDialogueOptionClicked?.Invoke(dialogueIndex, isCorrect);

        // 2. 隐藏当前对话框及其选项
        HideDialogue(dialogueIndex);
        if (dialogueIndex == 1)
        {
            SetImageActive(dialogue1CorrectOption, false);
            SetImageActive(dialogue1IncorrectOption, false);
            
            // 3. 规则：如果点击了对话1的【正确】选项，则显示对话2
            if (isCorrect)
            {
                // 延迟等待隐藏动画播完再显示下一个，或者直接显示
                float delay = animationType == UIAnimationType.None ? 0f : animationDuration;
                StartCoroutine(ShowNextDialogueDelayed(2, delay));
            }
        }
        else if (dialogueIndex == 2)
        {
            SetImageActive(dialogue2CorrectOption, false);
            SetImageActive(dialogue2IncorrectOption, false);
        }
    }

    private IEnumerator ShowNextDialogueDelayed(int dialogueIndex, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);
            
        ShowDialogue(dialogueIndex);
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
            SetImageActive(dialogue1CorrectOption, false, true);
            SetImageActive(dialogue1IncorrectOption, false, true);
            StartCoroutine(ShowOptionsDelayed(1, optionDelay));
        }
        else if (dialogueIndex == 2 && dialogue2 != null)
        {
            ShowUI(dialogue2);
            // 显示对话2时，选项先瞬间隐藏，再延迟出现
            SetImageActive(dialogue2CorrectOption, false, true);
            SetImageActive(dialogue2IncorrectOption, false, true);
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
        SetImageActive(dialogue1CorrectOption, false);
        SetImageActive(dialogue1IncorrectOption, false);
        SetImageActive(dialogue2CorrectOption, false);
        SetImageActive(dialogue2IncorrectOption, false);
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

        if (instant || animationType == UIAnimationType.None)
        {
            uiObject.SetActive(false);
            return;
        }

        switch (animationType)
        {
            case UIAnimationType.Fade:
                if (cg == null) cg = uiObject.AddComponent<CanvasGroup>();
                cg.DOFade(0f, animationDuration).SetEase(hideEase).OnComplete(() => uiObject.SetActive(false));
                break;
            case UIAnimationType.Scale:
                uiObject.transform.DOScale(Vector3.zero, animationDuration).SetEase(hideEase).OnComplete(() => uiObject.SetActive(false));
                break;
        }
    }

    #endregion
}
