using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
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
    public GameObject dialogue3;
    public GameObject dialogue4;
    public GameObject dialogue5;
    public GameObject dialogue6;

    [Header("对话1 选项 (点击发送广播)")]
    public GameObject dialogue1OptionCorrect;
    public GameObject dialogue1OptionIncorrect;

    [Header("对话2 选项 (点击发送广播)")]
    public GameObject dialogue2OptionCorrect;
    public GameObject dialogue2OptionIncorrect;

    [Header("对话3 选项 (点击发送广播)")]
    public GameObject dialogue3OptionCorrect;
    public GameObject dialogue3OptionIncorrect;

    [Header("对话6 选项 (点击发送广播)")]
    public GameObject dialogue6OptionCorrect;
    public GameObject dialogue6OptionIncorrect;

    [Header("UI Images / 对话7及选项")]
    public GameObject dialogue7;
    public GameObject dialogue7OptionCorrect;
    public GameObject dialogue7OptionIncorrect;

    [Header("UI Images / 对话8")]
    public GameObject dialogue8;

    [Header("UI Images / 对话9")]
    public GameObject dialogue9;

    [Header("Raw Image (对话3出现时打开)")]
    public RawImage dialogue3RawImage;

    [Header("Videos / 视频 (对话3出现时打开)")]
    public VideoPlayer video1;

    [Header("Game Over UI / 游戏结束界面")]
    public GameObject gameOverUI;

    // 广播事件：参数1为对话编号（如 1 代表对话1），参数2为是否正确（true为正确，false为错误）
    public event Action<int, bool> OnDialogueOptionClicked;
    
    // 广播事件：参数为对话编号
    public event Action<int> OnDialogueShown;

    // 广播事件：参数为对话编号（用于单纯点击对话面板，不区分正误）
    public event Action<int> OnDialogueClicked;

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

        AddClickListenerToGameObject(dialogue3OptionCorrect, 3, true);
        AddClickListenerToGameObject(dialogue3OptionIncorrect, 3, false);

        AddClickListenerToGameObject(dialogue6OptionCorrect, 6, true);
        AddClickListenerToGameObject(dialogue6OptionIncorrect, 6, false);

        AddClickListenerToGameObject(dialogue7OptionCorrect, 7, true);
        AddClickListenerToGameObject(dialogue7OptionIncorrect, 7, false);

        // 为对话5本身添加点击事件（不发送广播，仅用于推进流程）
        AddDialogueClickListener(dialogue5, () => 
        {
            if (dialogue5 != null && dialogue5.activeSelf)
            {
                HideDialogue(4); // 点击对话5时，同时隐藏对话4
                HideDialogue(5); // 隐藏对话5本身
                float delay = animationType == UIAnimationType.None ? 0f : animationDuration;
                StartCoroutine(ShowNextDialogueDelayed(6, delay));
            }
        });

        // 为对话8本身添加点击事件
        AddDialogueClickListener(dialogue8, () => 
        {
            if (dialogue8 != null && dialogue8.activeSelf)
            {
                HideDialogue(8); // 隐藏对话8本身
                // 广播对话被点击事件，让摄像机移动
                try
                {
                    OnDialogueClicked?.Invoke(8);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Broadcast OnDialogueClicked failed: {e.Message}\n{e.StackTrace}");
                }
            }
        });

        // 为对话9本身添加点击事件（如有需要，用于推进后续流程）
        AddDialogueClickListener(dialogue9, () => 
        {
            if (dialogue9 != null && dialogue9.activeSelf)
            {
                HideDialogue(9);
            }
        });

        // 初始化UI状态
        InitializeUI();
    }

    /// <summary>
    /// 为对话框本身添加点击事件（不发送广播，仅用于流程推进）
    /// </summary>
    private void AddDialogueClickListener(GameObject obj, Action onClickAction)
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
        entry.callback.AddListener((data) => { onClickAction?.Invoke(); });
        
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 初始化UI显示状态，根据规则：
    /// Dialogue1默认出现，对话1选项延迟1秒出现
    /// Dialogue2、3及选项默认隐藏
    /// </summary>
    private void InitializeUI()
    {
        // 隐藏游戏结束界面
        HideUI(gameOverUI, true);

        // 隐藏所有对话2相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue2, true);
        SetGameObjectActive(dialogue2OptionCorrect, false, true);
        SetGameObjectActive(dialogue2OptionIncorrect, false, true);

        // 隐藏所有对话3相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue3, true);
        SetGameObjectActive(dialogue3OptionCorrect, false, true);
        SetGameObjectActive(dialogue3OptionIncorrect, false, true);

        // 隐藏所有对话4相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue4, true);

        // 隐藏所有对话5相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue5, true);

        // 隐藏所有对话6相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue6, true);
        SetGameObjectActive(dialogue6OptionCorrect, false, true);
        SetGameObjectActive(dialogue6OptionIncorrect, false, true);

        // 隐藏所有对话7相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue7, true);
        SetGameObjectActive(dialogue7OptionCorrect, false, true);
        SetGameObjectActive(dialogue7OptionIncorrect, false, true);

        // 隐藏所有对话8相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue8, true);

        // 隐藏所有对话9相关的UI (瞬间隐藏，不要动画，作为初始状态)
        HideUI(dialogue9, true);

        // 显示视频 (始终打开)
        if (video1 != null) video1.gameObject.SetActive(true);

        // 显示对话3对应的 RawImage (始终打开)
        if (dialogue3RawImage != null) dialogue3RawImage.gameObject.SetActive(true);

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
        else if (dialogueIndex == 3)
        {
            if (dialogue3 != null && dialogue3.activeSelf)
            {
                SetGameObjectActive(dialogue3OptionCorrect, true);
                SetGameObjectActive(dialogue3OptionIncorrect, true);
            }
        }
        else if (dialogueIndex == 6)
        {
            if (dialogue6 != null && dialogue6.activeSelf)
            {
                SetGameObjectActive(dialogue6OptionCorrect, true);
                SetGameObjectActive(dialogue6OptionIncorrect, true);
            }
        }
        else if (dialogueIndex == 7)
        {
            if (dialogue7 != null && dialogue7.activeSelf)
            {
                SetGameObjectActive(dialogue7OptionCorrect, true);
                SetGameObjectActive(dialogue7OptionIncorrect, true);
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
        else if (dialogueIndex == 3)
        {
            SetGameObjectActive(dialogue3OptionCorrect, false);
            SetGameObjectActive(dialogue3OptionIncorrect, false);
        }
        else if (dialogueIndex == 6)
        {
            SetGameObjectActive(dialogue6OptionCorrect, false);
            SetGameObjectActive(dialogue6OptionIncorrect, false);
        }
        else if (dialogueIndex == 7)
        {
            SetGameObjectActive(dialogue7OptionCorrect, false);
            SetGameObjectActive(dialogue7OptionIncorrect, false);
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
            if (dialogueIndex == 1)
            {
                // 延迟等待隐藏动画播完再显示下一个，或者直接显示
                StartCoroutine(ShowNextDialogueDelayed(2, delay));
            }
            else if (dialogueIndex == 2)
            {
                // 显示对话3 (在这个过程中会先播放视频，再显示选项)
                StartCoroutine(ShowNextDialogueDelayed(3, delay));
            }
            else if (dialogueIndex == 3)
            {
                // 对话3已经全部完成（包含选项点击），可以推进到后续逻辑
                Debug.Log("对话3正确选项被点击。");
                // 推进到对话4
                StartCoroutine(ShowNextDialogueDelayed(4, delay));
            }
            else if (dialogueIndex == 6)
            {
                Debug.Log("对话6正确选项被点击。");
                // 推进到对话7
                StartCoroutine(ShowNextDialogueDelayed(7, delay));
            }
            else if (dialogueIndex == 7)
            {
                Debug.Log("对话7正确选项被点击。");
                // 推进到对话8
                StartCoroutine(ShowNextDialogueDelayed(8, delay));
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

    private IEnumerator PlayVideoAndShowOptions(float initialDelay)
    {
        // 1. 等待主对话框动画播放完毕
        if (initialDelay > 0)
            yield return new WaitForSeconds(initialDelay);

        // 2. 播放视频
        if (video1 != null)
        {
            video1.Play();
            
            // 等待视频准备好
            while (!video1.isPrepared)
            {
                yield return null;
            }

            // 等待视频播放完成
            while (video1.isPlaying || (ulong)video1.frame < video1.frameCount - 1)
            {
                yield return null;
            }
        }
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
        else if (dialogueIndex == 3 && dialogue3 != null)
        {
            ShowUI(dialogue3);
            if (dialogue3RawImage != null) dialogue3RawImage.gameObject.SetActive(true);
            SetGameObjectActive(dialogue3OptionCorrect, false, true);
            SetGameObjectActive(dialogue3OptionIncorrect, false, true);
            
            // 出现对话3时，播放视频1
            StartCoroutine(PlayVideoAndShowOptions(optionDelay));
            
            // 延迟11秒后显示对话3的选项
            StartCoroutine(ShowOptionsDelayed(3, 11f));
        }
        else if (dialogueIndex == 4 && dialogue4 != null)
        {
            ShowUI(dialogue4);
            // 对话4没有选项，停留2秒后自动推进到对话5
            StartCoroutine(ShowNextDialogueDelayed(5, 2f));
        }
        else if (dialogueIndex == 5 && dialogue5 != null)
        {
            ShowUI(dialogue5);
        }
        else if (dialogueIndex == 6 && dialogue6 != null)
        {
            ShowUI(dialogue6);
            SetGameObjectActive(dialogue6OptionCorrect, false, true);
            SetGameObjectActive(dialogue6OptionIncorrect, false, true);
            StartCoroutine(ShowOptionsDelayed(6, optionDelay));
        }
        else if (dialogueIndex == 7 && dialogue7 != null)
        {
            ShowUI(dialogue7);
            SetGameObjectActive(dialogue7OptionCorrect, false, true);
            SetGameObjectActive(dialogue7OptionIncorrect, false, true);
            StartCoroutine(ShowOptionsDelayed(7, optionDelay));
        }
        else if (dialogueIndex == 8 && dialogue8 != null)
        {
            ShowUI(dialogue8);
        }
        else if (dialogueIndex == 9 && dialogue9 != null)
        {
            ShowUI(dialogue9);
        }

        // 触发对话显示的事件
        try
        {
            OnDialogueShown?.Invoke(dialogueIndex);
        }
        catch (Exception e)
        {
            Debug.LogError($"Broadcast OnDialogueShown failed: {e.Message}\n{e.StackTrace}");
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
        else if (dialogueIndex == 3 && dialogue3 != null)
        {
            HideUI(dialogue3);
            if (dialogue3RawImage != null) dialogue3RawImage.gameObject.SetActive(false);
        }
        else if (dialogueIndex == 4 && dialogue4 != null)
        {
            HideUI(dialogue4);
        }
        else if (dialogueIndex == 5 && dialogue5 != null)
        {
            HideUI(dialogue5);
        }
        else if (dialogueIndex == 6 && dialogue6 != null)
        {
            HideUI(dialogue6);
        }
        else if (dialogueIndex == 7 && dialogue7 != null)
        {
            HideUI(dialogue7);
        }
        else if (dialogueIndex == 8 && dialogue8 != null)
        {
            HideUI(dialogue8);
        }
        else if (dialogueIndex == 9 && dialogue9 != null)
        {
            HideUI(dialogue9);
        }
    }

    /// <summary>
    /// 隐藏所有对话
    /// </summary>
    public void HideAllDialogues()
    {
        HideUI(dialogue1);
        HideUI(dialogue2);
        HideUI(dialogue3);
        HideUI(dialogue4);
        HideUI(dialogue5);
        HideUI(dialogue6);
        HideUI(dialogue7);
        HideUI(dialogue8);
        HideUI(dialogue9);
        SetGameObjectActive(dialogue1OptionCorrect, false);
        SetGameObjectActive(dialogue1OptionIncorrect, false);
        SetGameObjectActive(dialogue2OptionCorrect, false);
        SetGameObjectActive(dialogue2OptionIncorrect, false);
        SetGameObjectActive(dialogue3OptionCorrect, false);
        SetGameObjectActive(dialogue3OptionIncorrect, false);
        SetGameObjectActive(dialogue6OptionCorrect, false);
        SetGameObjectActive(dialogue6OptionIncorrect, false);
        SetGameObjectActive(dialogue7OptionCorrect, false);
        SetGameObjectActive(dialogue7OptionIncorrect, false);
        if (dialogue3RawImage != null) dialogue3RawImage.gameObject.SetActive(false);
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
