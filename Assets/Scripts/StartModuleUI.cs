using Fusion;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartModuleUI : MonoBehaviour
{
    [SerializeField] TMP_InputField sessionNameInput;
    [SerializeField] TMP_InputField sessionPasswordInput;
    [SerializeField] TextMeshProUGUI yesOrNoMessage;
    [SerializeField] TextMeshProUGUI okMessage;

    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;
    [SerializeField] Button okButton;
    [SerializeField] Button logOutButton;
    [SerializeField] Button roomSetButton;
    [SerializeField] Button createRoomButton;
    [SerializeField] Button joinRoomButton;

    [SerializeField] GameObject GameStartPanel;
    [SerializeField] GameObject InitPanel;
    [SerializeField] GameObject RoomSetPanel;

    Action OnYes;
    Action OnNo;
    Action OnOK;

    public string sessionName { get; private set; }
    public string sessionPassword { get; private set; }

    GameObject currentPanel;
    GameObject previousPanel;

    public void InitializeAllButtons(Action<GameMode> roomAction, Action logOutAction)
    {
        //room game start-->GameManager?
        //room quit
        logOutButton.onClick.AddListener(() => logOutAction.Invoke());
        //app turn off-->GameManager?
        //일단은 게임종료

        //begin RoomSetting
        roomSetButton.onClick.AddListener(() =>
        {
            InitPanel.SetActive(false);
            RoomSetPanel.SetActive(true);
            currentPanel = RoomSetPanel;
            previousPanel = InitPanel;
        });
        //room create
        createRoomButton.onClick.AddListener(() => roomAction(GameMode.Host));
        //room join
        joinRoomButton.onClick.AddListener(() => roomAction(GameMode.Client));
        yesButton.onClick.AddListener(PlayYesAction);
        noButton.onClick.AddListener(PlayNoAction);
        okButton.onClick.AddListener(PlayOKAction);
    }
    private void Awake()
    {
        sessionNameInput.onValueChanged.AddListener((s) => { sessionName = s; });
        sessionPasswordInput.onValueChanged.AddListener((s) => { sessionPassword = s; });
        InitPanel.SetActive(true);
        RoomSetPanel.SetActive(false);
        GameStartPanel.SetActive(false);
        
    }
    public void ShowOKMessage(string message, Action okAction)
    {
        okMessage.text = message;
        OnOK = okAction;

        if(currentPanel != okMessage.rectTransform.parent.gameObject)
        {
            previousPanel = currentPanel;
            currentPanel = okMessage.rectTransform.parent.gameObject;
            previousPanel.SetActive(false);
            currentPanel.SetActive(true);
        }

    }
    public void ShowYesOrNoMessage(string message, Action yesAction, Action noAction)
    {
        yesOrNoMessage.text = message;
        OnYes = yesAction;
        OnNo = noAction;

        previousPanel = currentPanel;
        currentPanel = yesOrNoMessage.rectTransform.parent.gameObject;
        previousPanel.SetActive(false);
        currentPanel.SetActive(true);
    }
    public void ShowPreviousPanel()
    {
        previousPanel.SetActive(true);
        currentPanel.SetActive(false);

        (previousPanel, currentPanel) = (currentPanel, previousPanel);
    }
    public void ShowGameStartPanel()
    {
        currentPanel.SetActive(false);
        GameStartPanel.SetActive(true);
    }
    void PlayOKAction()
    {
        OnOK?.Invoke();
    }
    void PlayYesAction()
    {
        OnYes?.Invoke();
    }
    void PlayNoAction()
    {
        OnNo?.Invoke();
    }
}
