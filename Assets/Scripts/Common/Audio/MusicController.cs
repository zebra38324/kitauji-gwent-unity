using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicController : MonoBehaviour
{
    private static string TAG = "MusicController";

    public GameObject background;

    public Slider volumeBGMSlider;

    public TextMeshProUGUI volumeBGMText;

    public Slider volumeSFXSlider;

    public TextMeshProUGUI volumeSFXText;

    public TextMeshProUGUI bgmNameText;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Init());
    }

    // Update is called once per frame
    void Update()
    {
        bgmNameText.text = AudioManager.Instance.GetBGMName();
    }

    public void OnMusicButtonClick()
    {
        if (background.activeSelf) {
            background.SetActive(false);
        } else {
            background.SetActive(true);
        }
    }

    public void UpdateBGMVolume()
    {
        AudioManager.Instance.SetBGMVolume(volumeBGMSlider.value);
        UpdateVolumeText(volumeBGMText, volumeBGMSlider.value);
    }

    public void UpdateSFXVolume()
    {
        AudioManager.Instance.SetSFXVolume(volumeSFXSlider.value);
        UpdateVolumeText(volumeSFXText, volumeSFXSlider.value);
    }

    public void SwitchNextBGM()
    {
        KLog.I(TAG, "SwitchNextBGM");
        AudioManager.Instance.SwitchNextBGM();
    }

    private IEnumerator Init()
    {
        yield return null; // 停一帧，确保AudioManager已构建
        float defaultBGMVolume = AudioManager.Instance.GetBGMVolume();
        volumeBGMSlider.value = defaultBGMVolume;
        UpdateVolumeText(volumeBGMText, defaultBGMVolume);
        float defaultSFXVolume = AudioManager.Instance.GetSFXVolume();
        volumeSFXSlider.value = defaultSFXVolume;
        UpdateVolumeText(volumeSFXText, defaultSFXVolume);
    }

    private void UpdateVolumeText(TextMeshProUGUI text, float value)
    {
        text.text = ((int)(value * 100)).ToString();
    }
}
