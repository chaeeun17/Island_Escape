using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigUIManager : MonoBehaviour
{
    private ConfigInfo configInfo = new ConfigInfo();

    [SerializeField] private Slider masterVol;
    [SerializeField] private Slider seVol;
    [SerializeField] private Slider ambVol;
    [SerializeField] private Slider uiVol;
    [SerializeField] private Slider vcVol;

    [SerializeField] private TextMeshProUGUI masterText;
    [SerializeField] private TextMeshProUGUI seText;
    [SerializeField] private TextMeshProUGUI ambText;
    [SerializeField] private TextMeshProUGUI uiText;
    [SerializeField] private TextMeshProUGUI vcText;

    [SerializeField] private List<Toggle> turnMode = new List<Toggle>();
    [SerializeField] private Slider smoothTurnSpeed;
    [SerializeField] private Slider snapTurnAngle;
    [SerializeField] private Toggle tunnelingVignette;
    [SerializeField] private List<Toggle> dominentHand = new List<Toggle>();
    [SerializeField] private List<Toggle> playPosition = new List<Toggle>();

    [SerializeField] private TextMeshProUGUI smoothTurnText;
    [SerializeField] private TextMeshProUGUI snapTurnText;

    [SerializeField] private List<Toggle> refreshRate = new List<Toggle>();
    [SerializeField] private List<Toggle> graphicsQuality = new List<Toggle>();
    [SerializeField] private List<Toggle> subtitles = new List<Toggle>();

    #region Sound
    public void MasterSlider()
    {
        configInfo.MasterVolume = (int)(masterVol.value * 100);
        masterText.text = configInfo.MasterVolume.ToString();
    }
    public void SESlider()
    {
        configInfo.SoundEffects = (int)(seVol.value * 100);
        seText.text = configInfo.SoundEffects.ToString();
    }
    public void AmbienceSlider()
    {
        configInfo.Ambience = (int)(ambVol.value * 100);
        ambText.text = configInfo.Ambience.ToString();
    }
    public void UISlider()
    {
        configInfo.UI = (int)(uiVol.value * 100);
        uiText.text = configInfo.UI.ToString();
    }
    public void VCSlider()
    {
        configInfo.VoiceChat = (int)(vcVol.value * 100);
        vcText.text = configInfo.VoiceChat.ToString();
    }
    #endregion

    #region Gameplay
    public void SmoothTurnClicked()
    {
        configInfo.TurnMode = 0;
    }

    public void SnapTurnClicked()
    {
        configInfo.TurnMode = 1;
    }

    public void SetSmoothTurnSpeed()
    {
        configInfo.SmoothTurnSpeed = (int)(smoothTurnSpeed.value * 100);
        smoothTurnText.text = configInfo.SmoothTurnSpeed.ToString();
    }
    public void SetSnapTurnAngle()
    {
        configInfo.SnapTurnAngle = (int)(snapTurnAngle.value * 100);
        snapTurnText.text = configInfo.SnapTurnAngle.ToString();
    }

    public void TunnelingVignetteClicked()
    {
        configInfo.TunnelingVignette = tunnelingVignette.isOn;
    }

    public void RightHandClicked()
    {
        configInfo.DominentHand = 0;
    }
    public void LeftHandClicked()
    {
        configInfo.DominentHand = 1;
    }
    public void StandingClicked()
    {
        configInfo.PlayPosition = 0;
    }
    public void SeatedClicked()
    {
        configInfo.PlayPosition = 1;
    }
    #endregion

    #region Display
    public void Rate72HzClicked()
    {
        configInfo.RefreshRate = 0;
    }

    public void Rate90HzClicked()
    {
        configInfo.RefreshRate = 1;
    }

    public void Rate120HzClicked()
    {
        configInfo.RefreshRate = 2;
    }

    public void LowClicked()
    {
        configInfo.GrapicsQuality = 0;
    }
    public void MiddleClicked()
    {
        configInfo.GrapicsQuality = 1;
    }
    public void HighClicked()
    {
        configInfo.GrapicsQuality = 2;
    }

    public void HeadLockedClicked()
    {
        configInfo.Subtitles = 0;
    }
    public void WorldLockedClicked()
    {
        configInfo.Subtitles = 1;
    }
    #endregion

    #region JSON
    public void LoadSavedConfiguration()
    {
        string filePath = Application.persistentDataPath + "/config.json";
        if (!File.Exists(filePath))
        {
            SaveConfiguration();
        }
        string jsonString = File.ReadAllText(filePath);
        configInfo = JsonConvert.DeserializeObject<ConfigInfo>(jsonString);

        masterVol.value = (float)configInfo.MasterVolume / 100;
        seVol.value = (float)configInfo.SoundEffects / 100;
        ambVol.value = (float)configInfo.Ambience / 100;
        uiVol.value = (float)configInfo.UI / 100;
        vcVol.value = (float)configInfo.VoiceChat / 100;

        masterText.text = configInfo.MasterVolume.ToString();
        seText.text = configInfo.SoundEffects.ToString();
        ambText.text = configInfo.Ambience.ToString();
        uiText.text = configInfo.UI.ToString();
        vcText.text = configInfo.VoiceChat.ToString();

        turnMode[configInfo.TurnMode].isOn = true;
        smoothTurnSpeed.value = (float)configInfo.SmoothTurnSpeed / 100;
        snapTurnAngle.value = (float)configInfo.SnapTurnAngle / 100;
        tunnelingVignette.isOn = configInfo.TunnelingVignette;
        dominentHand[configInfo.DominentHand].isOn = true;
        playPosition[configInfo.PlayPosition].isOn = true;

        smoothTurnText.text = configInfo.SmoothTurnSpeed.ToString();
        snapTurnText.text = configInfo.SnapTurnAngle.ToString();

        refreshRate[configInfo.RefreshRate].isOn = true;
        graphicsQuality[configInfo.GrapicsQuality].isOn = true;
        subtitles[configInfo.Subtitles].isOn = true;
    }

    public void SaveConfiguration()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "config.json");
        string jsonString = JsonConvert.SerializeObject(configInfo, Formatting.Indented);
        File.WriteAllText(filePath, jsonString);
    }
    #endregion
}
