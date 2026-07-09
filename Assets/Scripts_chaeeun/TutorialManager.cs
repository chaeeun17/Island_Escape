using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;


[System.Serializable] 
public class TutorialStep
{
    public string stepName;       // 단계 이름         
    [TextArea(3, 5)] 
    public string instructionText;     // UI에 띄울 안내 문구
    [TextArea(2, 3)] 
    public string successText;       // 성공 시 문구
    //public GameObject highlightArrow;     // 안내용 화살표 오브젝트
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject tutorialUIPanel;
    [SerializeField] private TextMeshProUGUI tutorialText; 
    
    [Header("튜토리얼 단계 리스트")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    
    private int currentStepIndex = -1;
    private bool isWaitingForNextStep = false;
    public event Action OnTutorialCompleted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetStep(0); 
    }

    public void SetStep(int index)
    {
        /*
        if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
        {
            if (tutorialSteps[currentStepIndex].highlightArrow != null)
                tutorialSteps[currentStepIndex].highlightArrow.SetActive(false);
        }*/

        currentStepIndex = index;

        if (currentStepIndex >= tutorialSteps.Count)
        {
            OnTutorialComplete();
            return;
        }

        isWaitingForNextStep = false;
        if (tutorialUIPanel != null)
        {
            tutorialUIPanel.SetActive(true);
        }

        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        if (tutorialText != null) tutorialText.text = currentStep.instructionText;

        //if (currentStep.highlightArrow != null) currentStep.highlightArrow.SetActive(true);
    }

    public void CompleteCurrentStep()
    {
        isWaitingForNextStep = true; 
        
        if (tutorialUIPanel != null) tutorialUIPanel.SetActive(true); // 껐던 UI 다시 켜기

        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        if (tutorialText != null) tutorialText.text = currentStep.successText; 
        
        Debug.Log($"✅ {currentStep.stepName} 완료!");
    }

    public void OnClickConfirm()
    {
        if (isWaitingForNextStep)
        {
            // "완료!" 창에서 확인을 눌렀다면 -> 다음 단계 미션으로
            if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
            SetStep(currentStepIndex + 1);
        }
        else
        {
            // "안내" 창에서 확인을 눌렀다면 -> UI 끄고 미션 수행하러 가기
            if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
        }
    }

    public void NextStep()
    {
        SetStep(currentStepIndex + 1);
    }

    private void OnTutorialComplete()
    {
        Debug.Log("🎉 모든 튜토리얼 완료");

        if (OnTutorialCompleted != null)
        {
            OnTutorialCompleted.Invoke();
        }
    }
}
