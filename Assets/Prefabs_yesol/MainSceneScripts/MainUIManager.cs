using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class MainUIManager : MonoBehaviour
{
    [SerializeField] private ConfigUIManager configUIManager;

    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject hostUI;
    [SerializeField] private GameObject joinUI;
    [SerializeField] private GameObject configUI;
    [SerializeField] private GameObject quitUI;

    [SerializeField] private bool hostOn = false;
    [SerializeField] private bool joinOn = false;
    [SerializeField] private bool configOn = false;
    [SerializeField] private bool quitOn = false;

    [SerializeField] private GameObject soundUI;
    [SerializeField] private GameObject gameplayUI;
    [SerializeField] private GameObject controlsUI;
    [SerializeField] private GameObject displayUI;

    [SerializeField] private bool soundOn = true;
    [SerializeField] private bool gameplayOn = false;
    [SerializeField] private bool controlsOn = false;
    [SerializeField] private bool displayOn = false;

    #region MainUI
    public void HostBtnClicked()
    {
        hostOn = !hostOn;
        hostUI.SetActive(hostOn);
        mainUI.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = !hostOn;
    }
    public void JoinBtnClicked()
    {
        joinOn = !joinOn;
        joinUI.SetActive(joinOn);
        mainUI.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = !joinOn;
    }
    public void ConfigBtnClicked()
    {
        configOn = !configOn;
        if (configOn)
        {
            configUI.SetActive(configOn);
            configUI.GetComponent<Canvas>().enabled = false;

            soundUI.SetActive(true);
            gameplayUI.SetActive(true);
            controlsUI.SetActive(true);
            displayUI.SetActive(true);

            configUIManager.LoadSavedConfiguration();

            soundUI.SetActive(soundOn);
            gameplayUI.SetActive(gameplayOn);
            controlsUI.SetActive(controlsOn);
            displayUI.SetActive(displayOn);

            configUI.GetComponent<Canvas>().enabled = true;
        }
        else
        {
            configUIManager.SaveConfiguration();
            configUI.SetActive(configOn);
        }
            mainUI.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = !configOn;
    }
    public void QuitBtnClicked()
    {
        quitOn = !quitOn;
        quitUI.SetActive(quitOn);
        mainUI.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = !quitOn;
    }
    #endregion

    #region HostUI
    #endregion

    #region JoinUI
    #endregion

    #region ConfigUI
    public void SoundToggleClicked()
    {
        soundOn = !soundOn;
        if (!soundOn) return;
        else SetConfigToggles(true, false, false, false);
    }
    public void GameplayToggleClicked()
    {
        gameplayOn = !gameplayOn;
        if (!gameplayOn) return;
        else SetConfigToggles(false, true, false, false);
    }
    public void ControlToggleClicked()
    {
        controlsOn = !controlsOn;
        if (!controlsOn) return;
        else SetConfigToggles(false, false, true, false);
    }
    public void DisplayToggleClicked()
    {
        displayOn = !displayOn;
        if (!displayOn) return;
        else SetConfigToggles(false, false, false, true);
    }

    private void SetConfigToggles(bool sound, bool gameplay, bool controls, bool display)
    {
        soundUI.SetActive(sound);
        gameplayUI.SetActive(gameplay);
        controlsUI.SetActive(controls);
        displayUI.SetActive(display);
    }
    #endregion

    #region QuitUI
    #endregion
}
