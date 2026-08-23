using Fusion;
using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartSceneUI : MonoBehaviour
{
    //Host Join Config Quit
    //Start
    [SerializeField] TMP_InputField sessionNameInput;
    [SerializeField] TMP_InputField sessionPasswordInput;
    [SerializeField] TextMeshProUGUI notificationText;
    [SerializeField] TextMeshProUGUI roomSetText;

    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;
    [SerializeField] Button okButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button configButton;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button startButton;

    [SerializeField] GameObject ConfigPanel;
    [SerializeField] GameObject RoomSetPanel;
    [SerializeField] GameObject NotificationPanel;

    Action OnYes;
    Action OnNo;
    Action OnOK;

    public string sessionName { get; private set; }
    public string sessionPassword { get; private set; }

    private GameMode gameMode;

    public void InitializeAllButtons(Action<GameMode> roomAction)
    {
        quitButton.onClick.AddListener(() =>
        {
            ShowYesOrNoMessage("Are You Sure to QUIT?",//정말 나갈거니?
                () => { Application.Quit(); },//응 나갈게(YesButton 누르면)
                () => { NotificationPanel.SetActive(false);//아냐(NoButton 누르면)
                });
        });

        configButton.onClick.AddListener(() =>
        {
            if (RoomSetPanel.activeInHierarchy)
                RoomSetPanel.SetActive(false);
            ConfigPanel.SetActive(true);
        });
        startButton.onClick.AddListener(() => roomAction(gameMode));
        //room host
        hostButton.onClick.AddListener(() => SetRoomSettingPanel(GameMode.Host));
        //room join
        joinButton.onClick.AddListener(() => SetRoomSettingPanel(GameMode.Client));
        yesButton.onClick.AddListener(PlayYesAction);
        noButton.onClick.AddListener(PlayNoAction);
        okButton.onClick.AddListener(PlayOKAction);
    }
    private void SetRoomSettingPanel(GameMode gameMode)
    {
        this.gameMode = gameMode;
        string roomSet = gameMode == GameMode.Host ? "Hosting" : "Joining";
        roomSetText.text = roomSet;
        if (ConfigPanel.activeInHierarchy)
            ConfigPanel.SetActive(false);
        RoomSetPanel.SetActive(true);
    }
    private void Awake()
    {
        sessionNameInput.onValueChanged.AddListener((s) =>  OnRoomNameChanged(ChangeText(s)));
        sessionPasswordInput.onValueChanged.AddListener((s) => OnRoomPasswordChanged(ChangeText(s)));
        NotificationPanel.SetActive(false);
        RoomSetPanel.SetActive(false);
        ConfigPanel.SetActive(false);

    }
    private void OnRoomNameChanged(string roomName)
    { 
        sessionName = roomName;
        sessionNameInput.text = roomName;
    }
    private void OnRoomPasswordChanged(string roomPassword)
    {
        sessionPassword = roomPassword;
        sessionPasswordInput.text = roomPassword;
    }
    private string ChangeText(string s)
    {
        string nameword = Regex.Replace(s, @"[^0-9A-Z]", "");
        return nameword;
    }
    public void ShowOKMessage(string message, Action okAction)
    {
        notificationText.text = message;
        OnOK = okAction;
        okButton.gameObject.SetActive(true);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);

        NotificationPanel.SetActive(true);
    }
    public void ShowYesOrNoMessage(string message, Action yesAction, Action noAction)
    {
        notificationText.text = message;
        OnYes = yesAction;
        OnNo = noAction;
        okButton.gameObject.SetActive(false);
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        NotificationPanel.SetActive(true);
    }
    void PlayOKAction()
    {
        OnOK?.Invoke();
        NotificationPanel.SetActive(false);
    }
    void PlayYesAction()
    {
        OnYes?.Invoke();
        NotificationPanel.SetActive(false);
    }
    void PlayNoAction()
    {
        OnNo?.Invoke();
        NotificationPanel.SetActive(false);
    }
}
