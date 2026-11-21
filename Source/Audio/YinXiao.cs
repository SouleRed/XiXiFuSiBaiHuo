/* 
计划（伪代码，详细）：
1. 在类中添加一个全局音量控制属性：MasterVolume（0.0 到 1.0），用于控制所有播放音效的总体音量。
2. 提供设置/获取 MasterVolume 的静态方法或属性，内部做边界裁剪（Clamp）。
3. 添加播放音效的方法 `Play(string name, float volume = 1f, bool loop = false)`： 
   - 查找 `AudioClips` 字典中是否存在对应 `AudioClip`。
   - 创建一个临时的 `GameObject`（或复用父对象下的子对象），添加 `AudioSource` 组件。
   - 将 `AudioClip` 赋值给 `AudioSource.clip`，并把 `AudioSource.volume` 设置为 `Clamp01(MasterVolume * volume)`。
   - 设置 `loop`，调用 `Play()`。
   - 如果不循环，启动协程或使用 `Object.Destroy` 在 `clip.length` 后销毁该 `GameObject`。
4. 添加停止方法：
   - `Stop(string name)`：停止当前正在播放且与给定名字匹配的 `AudioSource`。
   - `StopAll()`：停止并销毁所有由本工具创建的临时 `AudioSource`。
5. 保持 `LoadAuidioCllip` 的音频加载逻辑不变（只在播放时控制音量），并修正拼写/命名的可读性（尽量不破坏原有签名）。
6. 在文件顶部用注释记录主要变更，确保代码与 Unity 的运行时 API 一致（使用 `UnityEngine`）。
7. 提供 `TryGetAudioClip(string name, out AudioClip clip)` 便于外部查询音效。

下面是修改后的完整文件，实现了上述功能。
*/

using BaseMod;
using NAudio.Wave;
using Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace 裁缝之王
{
    public class YinXiao : ModInitializer
    {
        // 全局音量（0..1）
        private static float _masterVolume = 1f;
        public static float MasterVolume
        {
            get { return _masterVolume; }
            set { _masterVolume = Mathf.Clamp01(value); }
        }

        // 用于承载临时播放对象，避免场景混乱
        private static GameObject _audioParent;

        public override void OnInitializeMod()
        {
            YinXiao.LoadAuidioCllip(new DirectoryInfo(YinXiao.path + "/BGM"));
            EnsureAudioParent();
            // 不在此处播放任何音效，播放由 Play 方法控制
        }

        private static void EnsureAudioParent()
        {
            if (_audioParent == null)
            {
                _audioParent = new GameObject("YinXiao_AudioParent");
                UnityEngine.Object.DontDestroyOnLoad(_audioParent);
            }
        }

        public static string path
        {
            get
            {
                bool flag = YinXiao._path == null;
                if (flag)
                {
                    YinXiao._path = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                }
                return YinXiao._path;
            }
        }

        public static void LoadAuidioCllip(DirectoryInfo dir)
        {
            bool flag = dir.GetDirectories().Length != 0;
            if (flag)
            {
                DirectoryInfo[] directories = dir.GetDirectories();
                for (int i = 0; i < directories.Length; i++)
                {
                    YinXiao.LoadAuidioCllip(directories[i]);
                }
            }
            foreach (FileInfo fileInfo in dir.GetFiles())
            {
                AudioClip audioClip = null;
                bool flag2 = fileInfo.Extension == ".mp3";
                if (flag2)
                {
                    using (Mp3FileReader sourceProvider = new Mp3FileReader(fileInfo.FullName))
                    {
                        string tmpWav = fileInfo.FullName + ".wav";
                        WaveFileWriter.CreateWaveFile(tmpWav, sourceProvider);
                        YinXiao.WAV wav = new YinXiao.WAV(File.ReadAllBytes(tmpWav));
                        AudioClip audioClip2 = AudioClip.Create(Path.GetFileNameWithoutExtension(fileInfo.FullName), wav.SampleCount, 1, wav.Frequency, false);
                        audioClip2.SetData(wav.LeftChannel, 0);
                        try
                        {
                            File.Delete(tmpWav);
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                        audioClip = audioClip2;
                    }
                }
                bool flag3 = fileInfo.Extension == ".wav";
                if (flag3)
                {
                    YinXiao.WAV wav2 = new YinXiao.WAV(File.ReadAllBytes(fileInfo.FullName));
                    AudioClip audioClip3 = AudioClip.Create(Path.GetFileNameWithoutExtension(fileInfo.FullName), wav2.SampleCount, 1, wav2.Frequency, false);
                    audioClip3.SetData(wav2.LeftChannel, 0);
                    audioClip = audioClip3;
                }
                bool flag4 = !YinXiao.AudioClips.ContainsKey(Path.GetFileNameWithoutExtension(fileInfo.FullName)) && audioClip != null;
                if (flag4)
                {
                    YinXiao.AudioClips[Path.GetFileNameWithoutExtension(fileInfo.FullName)] = audioClip;
                }
            }
        }

        public static Dictionary<string, AudioClip> AudioClips = new Dictionary<string, AudioClip>();

        protected static string _path = null;
        public class WAV
        {
            private static float ByRAihonDonfannfight_Lyra_ItilizeroFloat(byte firstByte, byte secondByte)
            {
                return (float)((short)((int)secondByte << 8 | (int)firstByte)) / 32768f;
            }

            private static int ByRAihonDonfannfight_Lyra_Itilizerolnt(byte[] bytes, int offset = 0)
            {
                int num = 0;
                for (int i = 0; i < 4; i++)
                {
                    num |= (int)bytes[offset + i] << i * 8;
                }
                return num;
            }

            private static byte[] GetBytes(string filename)
            {
                return File.ReadAllBytes(filename);
            }

            public float[] LeftChannel { get; internal set; }

            public float[] RightChannel { get; internal set; }

            public int ChannelCount { get; internal set; }

            public int SampleCount { get; internal set; }

            public int Frequency { get; internal set; }

            public WAV(string filename) : this(YinXiao.WAV.GetBytes(filename))
            {
            }

            public WAV(byte[] wav)
            {
                this.ChannelCount = (int)wav[22];
                this.Frequency = YinXiao.WAV.ByRAihonDonfannfight_Lyra_Itilizerolnt(wav, 24);
                int i = 12;
                while (wav[i] != 100 || wav[i + 1] != 97 || wav[i + 2] != 116 || wav[i + 3] != 97)
                {
                    i += 4;
                    int num = (int)wav[i] + (int)wav[i + 1] * 256 + (int)wav[i + 2] * 65536 + (int)wav[i + 3] * 16777216;
                    i += 4 + num;
                }
                i += 8;
                this.SampleCount = (wav.Length - i) / 2;
                bool flag = this.ChannelCount == 2;
                bool flag2 = flag;
                if (flag2)
                {
                    this.SampleCount /= 2;
                }
                this.LeftChannel = new float[this.SampleCount];
                bool flag3 = this.ChannelCount == 2;
                bool flag4 = flag3;
                if (flag4)
                {
                    this.RightChannel = new float[this.SampleCount];
                }
                else
                {
                    this.RightChannel = null;
                }
                int num2 = 0;
                while (i < wav.Length)
                {
                    this.LeftChannel[num2] = YinXiao.WAV.ByRAihonDonfannfight_Lyra_ItilizeroFloat(wav[i], wav[i + 1]);
                    i += 2;
                    bool flag5 = this.ChannelCount == 2;
                    bool flag6 = flag5;
                    if (flag6)
                    {
                        this.RightChannel[num2] = YinXiao.WAV.ByRAihonDonfannfight_Lyra_ItilizeroFloat(wav[i], wav[i + 1]);
                        i += 2;
                    }
                    num2++;
                }
            }
        }

        // 通过名字播放音效：name 为字典中的键（不带扩展名），volume 为相对音量（0..1），loop 是否循环
        public static AudioSource Play(string name, float volume = 1f, bool loop = false)
        {
            EnsureAudioParent();
            AudioClip clip;
            if (!AudioClips.TryGetValue(name, out clip) || clip == null)
            {
                Debug.LogWarning($"YinXiao: AudioClip '{name}' not found.");
                return null;
            }

            GameObject go = new GameObject($"YinXiao_{name}");
            go.transform.SetParent(_audioParent.transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = Mathf.Clamp01(MasterVolume * volume);
            src.Play();

            if (!loop)
            {
                // 销毁 GameObject，在主线程中通过延时销毁（使用 Unity 的 Destroy）
                UnityEngine.Object.Destroy(go, clip.length + 0.1f);
            }

            return src;
        }

        // 新增：直接以 AudioClip 播放，签名兼容 SoundEffectManager.PlayClip(clip, loop, volume, userdata)
        public static AudioSource Play(AudioClip clip, bool loop = false, float volume = 1f, object userdata = null)
        {
            EnsureAudioParent();
            if (clip == null)
            {
                Debug.LogWarning("YinXiao: AudioClip is null.");
                return null;
            }

            string name = clip.name;
            GameObject go = new GameObject($"YinXiao_{name}");
            go.transform.SetParent(_audioParent.transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = Mathf.Clamp01(MasterVolume * volume);
            src.Play();

            if (!loop)
            {
                UnityEngine.Object.Destroy(go, clip.length + 0.1f);
            }

            return src;
        }

        // 新增：提供 PlayClip 别名以完全匹配类似接口
        public static AudioSource PlayClip(AudioClip clip, bool loop, float volume, object userdata)
        {
            return Play(clip, loop, volume, userdata);
        }

        // 停止并销毁指定名字的所有播放实例
        public static void Stop(string name)
        {
            if (_audioParent == null) return;
            var children = _audioParent.GetComponentsInChildren<AudioSource>(true);
            foreach (var src in children)
            {
                if (src != null && src.clip != null && src.clip.name == name)
                {
                    src.Stop();
                    UnityEngine.Object.Destroy(src.gameObject);
                }
            }
        }

        // 停止并销毁所有由本类创建的音效
        public static void StopAll()
        {
            if (_audioParent == null) return;
            UnityEngine.Object.Destroy(_audioParent);
            _audioParent = null;
        }

        // 尝试取出 AudioClip
        public static bool TryGetAudioClip(string name, out AudioClip clip)
        {
            return AudioClips.TryGetValue(name, out clip);
        }

        // 播放音效并在其播放完毕后接着循环播放另一个音效
        public static void PlayThenLoop(string firstName, string loopName, float volume = 1f)
        {
            AudioClip firstClip;
            // 如果找不到第一个音频，直接播放循环音频
            if (!TryGetAudioClip(firstName, out firstClip) || firstClip == null)
            {
                Play(loopName, volume, true);
                return;
            }

            // 先播放第一个（非循环）
            Play(firstName, volume, false);
            // 用协程在第一个 clip 长度后启动循环音频
            CoroutineRunner.StartCoroutine(PlayAfterDelayCoroutine(firstClip.length, loopName, volume));
        }

        private static System.Collections.IEnumerator PlayAfterDelayCoroutine(float delay, string loopName, float volume)
        {
            yield return new WaitForSeconds(delay);
            Play(loopName, volume, true);
        }

        public void SetVolume(float volume)
        {
            foreach (KeyValuePair<int, SoundEffectPlayer> keyValuePair in this._management)
            {
                SoundEffectPlayer value = keyValuePair.Value;
                if (value != null)
                {
                    value.SetVolume(volume);
                }
            }
        }

        // Token: 0x06006192 RID: 24978 RVA: 0x001FF34C File Offset: 0x001FD54C
        public void RemovePlayer(int id)
        {
            this._management.Remove(id);
        }

        public GameObject ReferencePlayer;

        private int _id;

        private Dictionary<int, SoundEffectPlayer> _management = new Dictionary<int, SoundEffectPlayer>();

        private List<SoundEffectPlayer> _loopSounds = new List<SoundEffectPlayer>();

        // 新增方法：在播放完成后回调执行代码
        // 说明：将下面方法插入到类 `YinXiao` 中（与现有 Play / CoroutineRunner 配合使用）

        // 伪代码（详细计划）：
        // 1. 提供两个重载：按名字播放并回调；按 AudioClip 播放并回调。
        // 2. 在方法内部：
        //    a. 确保音频父对象存在（EnsureAudioParent）。
        //    b. 通过名字从 AudioClips 获取 AudioClip（或直接使用传入的 AudioClip）。
        //    c. 若找不到音频则打印警告并返回 null。
        //    d. 调用现有的 Play(clip, loop, volume, userdata) 创建并开始播放，获得返回的 AudioSource。
        //    e. 如果 loop == false 且回调不为 null，则使用 CoroutineRunner 启动一个协程：等待 clip.length 秒后执行回调。
        //    f. 协程中包裹 try/catch 捕获并记录异常，确保不会因回调异常中断其他逻辑。
        //    g. 返回创建的 AudioSource 以便调用方进一步操作（例如提前停止）。
        // 3. 提供一个私有静态协程方法负责延时等待并安全调用回调（使用 WaitForSeconds）。
        // 4. 注意：若 loop == true，则不自动调用回调（因为循环不会结束）；若需要在第一次播放后切换到循环播放，请使用已有的 PlayThenLoop 或自定义逻辑。


        public static AudioSource PlayWithCallback(string name, Action onComplete, float volume = 1f, bool loop = false)
        {
            EnsureAudioParent();
            AudioClip clip;
            if (!AudioClips.TryGetValue(name, out clip) || clip == null)
            {
                Debug.LogWarning($"YinXiao: AudioClip '{name}' not found.");
                return null;
            }

            AudioSource src = Play(clip, loop, volume, null);
            if (!loop && onComplete != null)
            {
                CoroutineRunner.StartCoroutine(InvokeAfterDelayCoroutine(clip.length, onComplete));
            }

            return src;
        }

        public static AudioSource PlayWithCallback(AudioClip clip, Action onComplete, float volume = 1f, bool loop = false)
        {
            EnsureAudioParent();
            if (clip == null)
            {
                Debug.LogWarning("YinXiao: AudioClip is null.");
                return null;
            }

            AudioSource src = Play(clip, loop, volume, null);
            if (!loop && onComplete != null)
            {
                CoroutineRunner.StartCoroutine(InvokeAfterDelayCoroutine(clip.length, onComplete));
            }

            return src;
        }

        private static IEnumerator InvokeAfterDelayCoroutine(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
