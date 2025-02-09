using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource bgmPlayer;

    public AudioSource sfxPlayer;

    public enum SFXType
    {
        Scorch = 0, // 因卡牌技能被移出
    }

    private Dictionary<SFXType, AudioClip> sfxCache;

    // Start is called before the first frame update
    void Start()
    {
        sfxCache = new Dictionary<SFXType, AudioClip>();
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(PlayBGM());
        InitSFX();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetBGMVolume(float value)
    {
        bgmPlayer.volume = value;
    }

    public float GetBGMVolume()
    {
        return bgmPlayer.volume;
    }

    public void SetSFXVolume(float value)
    {
        sfxPlayer.volume = value;
    }

    public float GetSFXVolume()
    {
        return sfxPlayer.volume;
    }

    public void PlaySFX(SFXType type)
    {
        sfxPlayer.clip = sfxCache[type];
        if (sfxPlayer.clip != null) {
            sfxPlayer.Play();
        }
    }

    private IEnumerator PlayBGM()
    {
        bgmPlayer.volume = 0.5f; // 初始默认音量
        KResources.Instance.Load<AudioClip>(bgmPlayer, @"Audio/bgm/dream_solister.mp3");
        while (bgmPlayer.clip == null) {
            yield return null; // TODO: 加载失败死循环？
        }
        bgmPlayer.Play();
    }

    private void InitSFX()
    {
        sfxPlayer.volume = 0.5f; // 初始默认音量
        sfxCache[SFXType.Scorch] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/scorch");
    }
}
