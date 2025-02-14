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
        Attack, // 攻击卡牌
        Tunning, // 调音
        SetFinish, // 单局结束
        Win, // 获得胜利
    }

    private Dictionary<SFXType, AudioClip> sfxCache;

    private static string[] bgmNameList = { "dream_solister.mp3", "普罗旺斯之风.mp3" };

    private int bgmIndex;

    private bool isAbort = false;

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

    private void OnApplicationQuit()
    {
        isAbort = true;
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
        string filePrefix = @"Audio/bgm/";
        bgmIndex = 0;
        while (!isAbort) {
            if (bgmPlayer.isPlaying) {
                yield return null;
                continue;
            }
            bgmPlayer.clip = null;
            KResources.Instance.Load<AudioClip>(bgmPlayer, filePrefix + bgmNameList[bgmIndex]);
            bgmIndex = (bgmIndex + 1) % bgmNameList.Length;
            while (bgmPlayer.clip == null && !isAbort) {
                yield return null;
            }
            bgmPlayer.Play();
        }
    }

    private void InitSFX()
    {
        sfxPlayer.volume = 0.5f; // 初始默认音量
        sfxCache[SFXType.Scorch] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/scorch");
        sfxCache[SFXType.Attack] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/attack");
        sfxCache[SFXType.Tunning] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/tunning");
        sfxCache[SFXType.SetFinish] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/set_finish");
        sfxCache[SFXType.Win] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/win");
    }
}
