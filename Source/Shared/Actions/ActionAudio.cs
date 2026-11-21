using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class ActionAudio
{
    internal static BattleEffectSound Play(string key, float volume = 1f)
    {
        var manager = SingletonBehavior<BattleSoundManager>.Instance;
        return manager ? PlayWithChannel(manager, key, volume * manager.VolumeFX) : null;
    }

    internal static BattleEffectSound PlayVoice(string key, float volume = 1f)
    {
        var manager = SingletonBehavior<BattleSoundManager>.Instance;
        if (!manager) return null;
        float voiceVolume = GlobalGameManager.Instance?.CurrentOption?.GetVolumeVoice() ?? manager.VolumeFX;
        return PlayWithChannel(manager, key, volume * voiceVolume);
    }

    private static BattleEffectSound PlayWithChannel(BattleSoundManager manager, string key, float finalVolume)
    {
        if (!CaiFengKing_Initializer.AudioClips.TryGetValue(key, out var clip)) return null;
        if (!clip || !manager.effectSoundPrefab) return null;
        var sound = Object.Instantiate(manager.effectSoundPrefab, manager.transform);
        sound.Init(clip, finalVolume);
        sound.enabled = false;
        sound.gameObject.AddComponent<KingAudioLifetime>().Source = sound.GetComponent<AudioSource>();
        return sound;
    }
}

public sealed class KingAudioLifetime : MonoBehaviour
{
    internal AudioSource Source;
    private void Update()
    {
        if (!AudioListener.pause && (!Source || !Source.isPlaying)) Destroy(gameObject);
    }
}
