using AutoKeywordUtil;
using BaseMod;
using HarmonyLib;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Y_LimbusBuff.Method;

namespace 裁缝之王
{
    public class CaiFengKing_Initializer : ModInitializer
    {
        public const string ModPackage = "CaiFengKing";

        public static string PackageId
        {
            get
            {
                return CaiFengKing_Initializer._packageId;
            }
        }
        public static LorId GetLorId(int id)
        {
            return new LorId(CaiFengKing_Initializer._packageId, id);
        }
        public override void OnInitializeMod()
        {
            Harmony harmony = new Harmony(CaiFengKing_Initializer._packageId);
            NativeKingSkin.Register(harmony, path);
            Presentation.CustomCombat.CombatHostPatches.Install(harmony);
            Presentation.CustomCombat.CustomCombatRuntime.Register();
            this.LoadAudioClip(Path.Combine(CaiFengKing_Initializer.path, "BGM"));
            YInitializer.ActivateMods.Add("CaiFengKing");
            AutoKeywordUtils.RegisterKeywordsFromAssembly(Assembly.GetExecutingAssembly());
        }
        public static string GetBattleDlg(LOR_XML.DialogType dlgType, string character)
        {
            string result = string.Empty;
            string groupID = "CaiFengKing";
            Dictionary<LOR_XML.DialogType, List<LOR_XML.BattleDialog>> dictionary = new Dictionary<LOR_XML.DialogType, List<LOR_XML.BattleDialog>>();
            LOR_XML.BattleDialogCharacter characterData = Singleton<BattleDialogXmlList>.Instance.GetCharacterData(groupID, character);
            if (characterData != null)
            {
                foreach (LOR_XML.BattleDialogType battleDialogType in characterData.dialogTypeList)
                {
                    dictionary.Add(battleDialogType.dialogType, battleDialogType.dialogList);
                }
            }
            if (dictionary != null && dictionary.ContainsKey(dlgType))
            {
                List<LOR_XML.BattleDialog> list = dictionary[dlgType];
                if (list.Count > 0)
                {
                    result = list[UnityEngine.Random.Range(0, list.Count)].dialogContent;
                }
            }
            return result;
        }

        private void LoadAudioClip(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                foreach (DirectoryInfo directoryInfo2 in directoryInfo.GetDirectories())
                {
                    this.LoadAudioClip(directoryInfo2.FullName);
                }
                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    AudioClip audioClip;
                    bool flag2 = ExtraTools.TryGetAudioClip(fileInfo.FullName, out audioClip);
                    if (flag2)
                    {
                        CaiFengKing_Initializer.AudioClip[audioClip.name] = audioClip;
                    }
                }
            }
        }
        public static readonly Dictionary<string, AudioClip> AudioClip = new Dictionary<string, AudioClip>();

        public static string packageId = "CaiFengKing";

        public static string _packageId = "CaiFengKing";

        public static string currentID = "CaiFengKing";

        public static string path = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

        public static Dictionary<string, AudioClip> CustomSound;
    }
}
