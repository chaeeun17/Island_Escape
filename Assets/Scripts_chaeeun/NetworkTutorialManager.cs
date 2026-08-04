using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using Fusion;

[System.Serializable] 
public class TutorialStep1
{
    public string stepName;        
    [TextArea(3, 5)] 
    public string instructionText;     
    [TextArea(2, 3)] 
    public string successText;       
}

public class NetworkTutorialManager : NetworkBehaviour
{
    public static NetworkTutorialManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText; 
    
    [Header("튜토리얼 단계 리스트")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    
    private int currentStepIndex = -1;
    private bool isWaitingForNextStep = false;
    private bool isTutorialStarted = false; // 중복 시작 방지 플래그

    [Networked] public NetworkBool IsTutorialCompleted { get; set; }

    public event Action OnTutorialCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 1. 멀티플레이로 씬이 스폰되었을 때 실행
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsTutorialCompleted = false;
        }

        StartTutorialIfNotStarted();
    }

    // 🌟 2. 혹시 네트워크 스폰을 안 타더라도 유니티 기본 Start에서 안전하게 실행!
    private void Start()
    {
        StartTutorialIfNotStarted();
    }

    private void StartTutorialIfNotStarted()
    {
        if (!isTutorialStarted)
        {
            isTutorialStarted = true;
            Debug.Log("🎓 [TutorialManager] 튜토리얼을 0단계부터 시작합니다!");
            SetStep(0); 
        }
    }

    public void SetStep(int index)
    {
        currentStepIndex = index;

        if (currentStepIndex >= tutorialSteps.Count)
        {
            OnTutorialComplete();
            return;
        }

        isWaitingForNextStep = false;
        
        // 🌟 UI 패널 켜기
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("🚨 [TutorialManager] tutorialUIPanel이 연결되어 있지 않습니다!");
        }

        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        if (tutorialText != null) 
        {
            tutorialText.text = currentStep.instructionText;
        }
    }

    public void CompleteCurrentStep()
    {
        isWaitingForNextStep = true; 
        
        if (tutorialUIPanel != null) tutorialUIPanel.SetActive(true);

        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        if (tutorialText != null) tutorialText.text = currentStep.successText; 
        
        Debug.Log($"✅ {currentStep.stepName} 완료!");
    }

    public void OnClickConfirm()
    {
        if (isWaitingForNextStep)
        {
            if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
            SetStep(currentStepIndex + 1);
        }
        else
        {
            if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
        }
    }

    public void NextStep()
    {
        SetStep(currentStepIndex + 1);
    }

    private void OnTutorialComplete()
    {
        Debug.Log("🎉 모든 튜토리얼 완료!");

        if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);

        if (Object != null && HasStateAuthority)
        {
            IsTutorialCompleted = true;
            RPC_NotifyTutorialCompleted();
        }
        else if (Object != null)
        {
            RPC_RequestCompleteTutorial();
        }
        else
        {
            // 싱글플레이 로컬 테스트용
            OnTutorialCompleted?.Invoke();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCompleteTutorial()
    {
        IsTutorialCompleted = true;
        RPC_NotifyTutorialCompleted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyTutorialCompleted()
    {
        OnTutorialCompleted?.Invoke();
    }
}