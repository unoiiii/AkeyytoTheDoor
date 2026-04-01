using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;

public class UIManager2 : MonoBehaviour
{
    public static UIManager2 Instance { get; private set; }

    [Header("UI Animations")]
    public float animationDuration = 0.5f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;

    [Header("Videos")]
    public float videoDelay = 2.0f;
    public GameObject video1;
    public GameObject video2;

    [Header("Dialogues (No Broadcast)")]
    public GameObject dialogue1;
    public GameObject dialogue2;
    public GameObject dialogue3;

    [Header("Dialogue 1 Options")]
    public GameObject dialogue1OptionCorrect;
    public GameObject dialogue1OptionWrong;

    [Header("Dialogue 2 Options")]
    public GameObject dialogue2OptionCorrect;
    public GameObject dialogue2OptionWrong;

    [Header("Dialogue 3 Options")]
    public GameObject dialogue3OptionCorrect;
    public GameObject dialogue3OptionWrong;

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
        // 绑定选项点击事件
        BindOptionEvent(dialogue1OptionCorrect, 1, true);
        BindOptionEvent(dialogue1OptionWrong, 1, false);

        BindOptionEvent(dialogue2OptionCorrect, 2, true);
        BindOptionEvent(dialogue2OptionWrong, 2, false);

        BindOptionEvent(dialogue3OptionCorrect, 3, true);
        BindOptionEvent(dialogue3OptionWrong, 3, false);

        // 初始化时隐藏所有UI
        HideAllInstant();
        
        // 默认显示对话1（如果有默认显示的逻辑）
        // ShowDialogue(1);
    }

    private void BindOptionEvent(GameObject optionObj, int dialogueIndex, bool isCorrect)
    {
        if (optionObj == null) return;

        // 确保图片能够接收射线点击
        Image img = optionObj.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        // 优先使用 Button 组件处理点击，如果没有则添加
        Button btn = optionObj.GetComponent<Button>();
        if (btn == null)
        {
            btn = optionObj.AddComponent<Button>();
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            HandleOptionClick(dialogueIndex, isCorrect);
        });
    }

    private void HandleOptionClick(int dialogueIndex, bool isCorrect)
    {
        // 1. 隐藏当前对话及其选项
        HideDialogue(dialogueIndex);

        // 2. 发送广播
        OnDialogueOptionClicked?.Invoke(dialogueIndex, isCorrect);
        
        // 可以在这里根据对错自动显示下一个对话，也可以由其他控制器(如GameManager)监听广播后调用ShowDialogue
    }

    /// <summary>
    /// 在正确的时间调用此方法显示对应的UI对话及选项
    /// </summary>
    public void ShowDialogue(int dialogueIndex)
    {
        GameObject dialogue = null;
        GameObject optionCorrect = null;
        GameObject optionWrong = null;

        switch (dialogueIndex)
        {
            case 1:
                dialogue = dialogue1;
                optionCorrect = dialogue1OptionCorrect;
                optionWrong = dialogue1OptionWrong;
                break;
            case 2:
                dialogue = dialogue2;
                optionCorrect = dialogue2OptionCorrect;
                optionWrong = dialogue2OptionWrong;
                break;
            case 3:
                dialogue = dialogue3;
                optionCorrect = dialogue3OptionCorrect;
                optionWrong = dialogue3OptionWrong;
                break;
            default:
                Debug.LogWarning($"UIManager2: 未配置对话 {dialogueIndex} 的UI。");
                return;
        }

        ShowUI(dialogue);
        // 选项延迟出现
        StartCoroutine(ShowOptionsDelayed(optionCorrect, optionWrong, 1.0f));
    }

    private IEnumerator ShowOptionsDelayed(GameObject optionCorrect, GameObject optionWrong, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowUI(optionCorrect);
        ShowUI(optionWrong);
    }

    /// <summary>
    /// 在正确的时机调用此方法：延迟指定时间后，先后显示视频1和视频2
    /// </summary>
    public void ShowVideos()
    {
        StartCoroutine(ShowVideosRoutine());
    }

    private IEnumerator ShowVideosRoutine()
    {
        // 延迟设定好的时间（默认2秒）
        yield return new WaitForSeconds(videoDelay);
        
        // 先出现视频1
        ShowUI(video1);
        
        // 此处可根据需要调节视频1和视频2之间的间隔时间
        yield return new WaitForSeconds(1.0f);
        
        // 后出现视频2
        ShowUI(video2);
    }

    /// <summary>
    /// 隐藏指定编号的对话及其选项
    /// </summary>
    public void HideDialogue(int dialogueIndex)
    {
        switch (dialogueIndex)
        {
            case 1:
                HideUI(dialogue1);
                HideUI(dialogue1OptionCorrect);
                HideUI(dialogue1OptionWrong);
                break;
            case 2:
                HideUI(dialogue2);
                HideUI(dialogue2OptionCorrect);
                HideUI(dialogue2OptionWrong);
                break;
            case 3:
                HideUI(dialogue3);
                HideUI(dialogue3OptionCorrect);
                HideUI(dialogue3OptionWrong);
                break;
        }
    }

    private void HideAllInstant()
    {
        SetInstantHide(dialogue1);
        SetInstantHide(dialogue1OptionCorrect);
        SetInstantHide(dialogue1OptionWrong);

        SetInstantHide(dialogue2);
        SetInstantHide(dialogue2OptionCorrect);
        SetInstantHide(dialogue2OptionWrong);

        SetInstantHide(dialogue3);
        SetInstantHide(dialogue3OptionCorrect);
        SetInstantHide(dialogue3OptionWrong);

        SetInstantHide(video1);
        SetInstantHide(video2);
    }

    private void SetInstantHide(GameObject uiObject)
    {
        if (uiObject == null) return;
        uiObject.SetActive(false);
    }

    private void ShowUI(GameObject uiObject)
    {
        if (uiObject == null) return;
        
        uiObject.transform.DOKill();
        CanvasGroup cg = uiObject.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();
        else cg = uiObject.AddComponent<CanvasGroup>();

        uiObject.SetActive(true);
        cg.blocksRaycasts = true;
        cg.interactable = true;
        
        cg.alpha = 0f;
        cg.DOFade(1f, animationDuration).SetEase(showEase);
    }

    private void HideUI(GameObject uiObject)
    {
        if (uiObject == null || !uiObject.activeSelf) return;
        
        uiObject.transform.DOKill();
        CanvasGroup cg = uiObject.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();
        else cg = uiObject.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false;
        cg.interactable = false;
        
        cg.DOFade(0f, animationDuration).SetEase(hideEase).OnComplete(() => uiObject.SetActive(false));
    }
}
