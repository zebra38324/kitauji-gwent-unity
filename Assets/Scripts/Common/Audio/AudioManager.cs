using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioManager : MonoBehaviour
{
    private static string TAG = "AudioManager";

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
        Awarding, // 颁奖仪式
    }

    private Dictionary<SFXType, AudioClip> sfxCache;

    private List<AudioClip> bgmList = new List<AudioClip>();

    private bool switchNextBGM = false;

    private bool pauseBGM = false;

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

    public void PlaySFX(SFXType type, bool loop = false)
    {
        sfxPlayer.loop = loop;
        sfxPlayer.clip = sfxCache[type];
        if (sfxPlayer.clip != null) {
            sfxPlayer.Play();
        }
    }

    public void StopSFX()
    {
        sfxPlayer.loop = false;
        if (sfxPlayer.isPlaying) {
            sfxPlayer.Stop();
        }
    }

    public string GetBGMName()
    {
        if (bgmPlayer.clip == null) {
            return "BGM加载中";
        }
        return bgmPlayer.clip.name;
    }

    public void SwitchNextBGM()
    {
        KLog.I(TAG, "SwitchNextBGM");
        switchNextBGM = true;
    }

    public void PauseBGM()
    {
        KLog.I(TAG, "PauseBGM");
        pauseBGM = true;
    }

    public void ResumeBGM()
    {
        KLog.I(TAG, "ResumeBGM");
        pauseBGM = false;
    }

    private IEnumerator PlayBGM()
    {
        bgmPlayer.volume = 0.5f; // 初始默认音量
        yield return LoadBGM();
        if (bgmList.Count == 0) {
            yield break;
        }
        int lastIndex = 0;
        while (!isAbort) {
            if (pauseBGM) {
                if (bgmPlayer.isPlaying) {
                    bgmPlayer.Pause();
                }
                yield return null;
                continue;
            } else if (!bgmPlayer.isPlaying) {
                bgmPlayer.UnPause();
            }
            if (bgmPlayer.isPlaying && !switchNextBGM) {
                yield return null;
                continue;
            }
            int randomIndex;
            do {
                randomIndex = Random.Range(0, bgmList.Count);
            } while (randomIndex == lastIndex);
            var clip = bgmList[randomIndex];
            bgmPlayer.clip = clip;
            lastIndex = randomIndex;
            switchNextBGM = false;
            KLog.I(TAG, $"PlayBGM: {clip.name} ({clip.length}秒)");
            bgmPlayer.Play();
        }
    }

    private IEnumerator LoadBGM()
    {
        KLog.I(TAG, "LoadBGM: will load");
        AsyncOperationHandle<IList<AudioClip>> handle = Addressables.LoadAssetsAsync<AudioClip>("bgm", null);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded) {
            foreach (AudioClip clip in handle.Result) {
                bgmList.Add(clip);
                KLog.I(TAG, $"LoadBGM: {clip.name} ({clip.length}秒)");
            }
            KLog.I(TAG, "LoadBGM: BGM assets loaded successfully: " + handle.Result.Count);
        } else {
            KLog.W(TAG, "LoadBGM: Failed to load BGM assets");
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
        sfxCache[SFXType.Awarding] = KResources.Instance.LoadLocal<AudioClip>(@"Audio/sfx/awarding");
    }
}
