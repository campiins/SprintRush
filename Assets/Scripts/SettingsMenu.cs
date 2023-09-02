using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] GameObject optionsMenu;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] TMP_Text volumeText;

    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Dropdown qualityDropdown;

    void Start()
    {
        LoadSettings();
    }

    public void SetVolume (float volume) // min = 0, MAX = 100
    {
        // Convertir valor del slider a decibelios
        float db;
        db = (volume / 100) * (0 - (-80)) - 80;

        audioMixer.SetFloat("volume", db);
        volumeText.text = volume.ToString();
        volumeSlider.value = volume;

        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    public void SetQuality (int qualityIndex) // 0 = LOW, 1 = MEDIUM, 2 = HIGH
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        qualityDropdown.value = qualityIndex;

        PlayerPrefs.SetInt("Quality", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen (bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    void LoadSettings()
    {
        float volumeValue = PlayerPrefs.GetFloat("Volume", 100); // Si no existe, su valor por defecto es 100 
        int qualityValue = PlayerPrefs.GetInt("Quality", 2); // Si no existe, su valor por defecto es 2 (HIGH)

        SetVolume(volumeValue);
        SetQuality(qualityValue);
    }
}