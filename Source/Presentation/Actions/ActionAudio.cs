using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class ActionAudio
{
    internal static BattleEffectSound Play(string key, float volume = 1f)
    {
        if (!YinXiao.TryGetAudioClip(key, out var clip)) return null;
        var manager = SingletonBehavior<BattleSoundManager>.Instance;
        if (!clip || !manager || !manager.effectSoundPrefab) return null;
        var sound = Object.Instantiate(manager.effectSoundPrefab, manager.transform);
        sound.Init(clip, volume * manager.VolumeFX);
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
