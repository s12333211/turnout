using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    static private SoundManager instance = null;

    public AudioMixer audioMixer;
    private AudioSource[] audioSources;                     //OutputはBGMやSEに予め設定すること。BGMはクロスフェードのために、BGMのAudioSourceは最低2つを用意する必要があります
    public static float VolumeMaster { get; private set; }  //全体ボリューム
    public static float VolumeBGM { get; private set; }     //BGMボリューム
    public static float VolumeSE { get; private set; }      //SEボリューム

    private const string volumeSettingKey = "_volume_setting_";
    private const string audioMixerVolumeMasterName = "VolumeMaster";
    private const string audioMixerVolumeBGMName = "VolumeBGM";
    private const string audioMixerVolumeSEName = "VolumeSE";
    private const float lowerVolumeDecibelBound = -80.0f;

    void Start()
    {
        if (instance != null)
        {
            GameObject.Destroy(this);
            return;
        }
        /* Do not destroy the SoundManger when scenes are switched. */
        GameObject.DontDestroyOnLoad(this.gameObject);
        instance = this;

        // AudioSource取得
        audioSources = gameObject.GetComponents<AudioSource>();

        // 設定読み込み
        string volumeSetting = PlayerPrefs.GetString(volumeSettingKey, "1,1,1");
        string[] volumeSettings = volumeSetting.Split(',');
        if (volumeSettings.Length >= 1 && float.TryParse(volumeSettings[0], out float volumeMaster))
            VolumeMaster = volumeMaster;
        else
            VolumeMaster = 0;
        if (volumeSettings.Length >= 2 && float.TryParse(volumeSettings[1], out float volumeBGM))
            VolumeBGM = volumeBGM;
        else
            VolumeBGM = 0;
        if (volumeSettings.Length >= 3 && float.TryParse(volumeSettings[2], out float volumeSE))
            VolumeSE = volumeSE;
        else
            VolumeSE = 0;
        // 音量設定
        instance.audioMixer.SetFloat(audioMixerVolumeMasterName, VolumeToDecibel(VolumeMaster));
        instance.audioMixer.SetFloat(audioMixerVolumeBGMName, VolumeToDecibel(VolumeBGM));
        instance.audioMixer.SetFloat(audioMixerVolumeSEName, VolumeToDecibel(VolumeSE));
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// SEを再生する
    /// </summary>
    /// <param name="audioClip">SEのAudioClip</param>
    /// <param name="volume">音量</param>
    /// <param name="pitch">ピッチ</param>
    public static void PlaySE(AudioClip audioClip, float volume = 1f, float pitch = 1f)
    {
        // AudioClipを確認
        if (audioClip == null)
        {
            Debug.LogWarning("音楽クリップがありません！");
            return;
        }
        // インスタンスを確認
        if (instance == null)
        {
            Debug.LogWarning("SoundManagerがありません！");
            return;
        }
        // 各AudioSourceを確認、合うAudioMixerGroupで再生
        for (int i = 0; i < instance.audioSources.Length; i++)
        {
            if (instance.audioSources[i].isPlaying != true && instance.audioSources[i].outputAudioMixerGroup.name == "SE")
            {
                // パラメター設定
                instance.audioSources[i].clip = audioClip;
                instance.audioSources[i].volume = volume;
                instance.audioSources[i].loop = false;
                instance.audioSources[i].pitch = pitch;
                // 再生
                instance.audioSources[i].Play();
                return;
            }
        }
        Debug.Log("SE再生為のチャンネルが足りない。:" + audioClip.name);
        return;
    }

    /// <summary>
    /// 主音量を設定し、PlayerPrefsに音量設定を保存する
    /// </summary>
    /// <param name="volumeBGM">主音量のボリューム値。1は元々の音量、0はミュート</param>
    public static void SetMasterVolume(float volumeMaster)
    {
        VolumeMaster = volumeMaster;
        // 設定保存
        PlayerPrefs.SetString(volumeSettingKey, VolumeMaster.ToString("0.00") + "," + VolumeBGM.ToString("0.00") + "," + VolumeSE.ToString("0.00"));
        // インスタンスを確認
        if (instance == null)
        {
            Debug.LogWarning("SoundManagerがありません！");
            return;
        }
        // 音量設定
        float decibel = VolumeToDecibel(VolumeMaster);
        instance.audioMixer.SetFloat(audioMixerVolumeMasterName, decibel);
    }
    /// <summary>
    /// BGM音量を設定し、PlayerPrefsに音量設定を保存する
    /// </summary>
    /// <param name="volumeBGM">BGMのボリューム値。1は元々の音量、0はミュート</param>
    public static void SetBGMVolume(float volumeBGM)
    {
        VolumeBGM = volumeBGM;
        // 設定保存
        PlayerPrefs.SetString(volumeSettingKey, VolumeMaster.ToString("0.00") + "," + VolumeBGM.ToString("0.00") + "," + VolumeSE.ToString("0.00"));
        // インスタンスを確認
        if (instance == null)
        {
            Debug.LogWarning("SoundManagerがありません！");
            return;
        }
        // 音量設定
        float decibel = VolumeToDecibel(VolumeBGM);
        instance.audioMixer.SetFloat(audioMixerVolumeBGMName, decibel);
    }

    /// <summary>
    /// SE音量を設定し、PlayerPrefsに音量設定を保存する
    /// </summary>
    /// <param name="volumeSE">SEのボリューム値。1は元々の音量、0はミュート</param>
    public static void SetSEVolume(float volumeSE)
    {
        VolumeSE = volumeSE;
        // 設定保存
        PlayerPrefs.SetString(volumeSettingKey, VolumeMaster.ToString("0.00") + "," + VolumeBGM.ToString("0.00") + "," + VolumeSE.ToString("0.00"));
        // インスタンスを確認
        if (instance == null)
        {
            Debug.LogWarning("SoundManagerがありません！");
            return;
        }
        // 音量設定
        float decibel = VolumeToDecibel(VolumeSE);
        instance.audioMixer.SetFloat(audioMixerVolumeSEName, decibel);
    }

    /// <summary>
    /// ボリュームからデシベルに変換
    /// </summary>
    private static float VolumeToDecibel(float volume)
    {
        if (volume == 0)
            return lowerVolumeDecibelBound;
        return Mathf.Log10(volume) * 20;
    }
}
