using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_CFK_BGM : DiceCardSelfAbilityBase//裁缝之王独立 BGM 覆盖（任意地图生效）
    {
        public static string Desc = "[装备时]改变背景音乐";

        internal static AudioClip ResolveKingBgm()
        {
            return NativeStageMap.TryGetDefaultBgm(out AudioClip nativeClip) ? nativeClip : null;
        }

        internal static void ApplyTo(BattleUnitModel unit)
        {
            if (unit == null) return;
            AudioClip clip = ResolveKingBgm();
            if (clip == null) return;

            var existing = unit.bufListDetail.GetActivatedBufList()
                .OfType<ChangeBGM>()
                .FirstOrDefault();
            if (existing != null)
            {
                existing.ApplyTheme();
                return;
            }
            unit.bufListDetail.AddBuf(new ChangeBGM(clip));
        }

        public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
            ApplyTo(unit);
        }

        /// <summary>
        /// 参照原版殷红迷雾（PassiveAbility_250422）的原生切曲机制：
        /// 通过固定敌方情感 BGM 判定端口 + SetEnemyTheme + ChangeEnemyTheme(0) 覆盖当前战斗 BGM，
        /// 不篡改原地图自身的 mapBgm 字段，可在任意地图中稳定生效。
        /// </summary>
        public class ChangeBGM : BattleUnitBuf
        {
            public ChangeBGM(AudioClip clip)
            {
                this.array[0] = clip;
                this.array[1] = clip;
                this.array[2] = clip;
            }

            internal AudioClip[] Theme => this.array;

            internal void ApplyTheme()
            {
                NativeStageMap.LockEnemyThemePort();

                BattleSoundManager sound = SingletonBehavior<BattleSoundManager>.Instance;
                if (sound == null || this.array[0] == null) return;

                // 已经在放同一首曲子时不要重切：重切会硬停当前音轨，开局会听到爆音
                AudioSource current = sound.CurrentPlayingTheme;
                if (current != null && current.clip == this.array[0])
                {
                    if (current.isPlaying) return;
                }

                // 开场瞬间 CurrentPlayingTheme 仍为空（原版还没 StartBgm），此时切换会打断开场曲，
                // 交给 OnRoundStart 再做，避免开局爆音
                if (current == null) return;

                sound.SetEnemyTheme(this.array);
                sound.ChangeEnemyTheme(0);
            }

            private static void ApplyOnRoundStartIfNeeded(AudioClip[] theme)
            {
                BattleSoundManager sound = SingletonBehavior<BattleSoundManager>.Instance;
                if (sound == null || theme[0] == null) return;
                AudioSource current = sound.CurrentPlayingTheme;
                if (current != null && current.clip == theme[0]) return;
                sound.SetEnemyTheme(theme);
                sound.ChangeEnemyTheme(0);
            }

            public override void Init(BattleUnitModel owner)
            {
                base.Init(owner);
                NativeStageMap.LockEnemyThemePort();
                ApplyTheme();
            }

            public override void OnRoundStart()
            {
                NativeStageMap.LockEnemyThemePort();
                ApplyOnRoundStartIfNeeded(this.array);
            }

            private readonly AudioClip[] array = new AudioClip[3];

            /// <summary>
            /// 防止在其他有专属 Boss 地图的接待中触发 ChangeToSpecialMap 时，
            /// 原版 SetEnemyTheme(对方地图.mapBgm) 将已激活的裁缝之王独立 BGM 覆盖回去。
            /// </summary>
            [HarmonyLib.HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.ChangeToSpecialMap))]
            public static class BattleSceneRoot_ChangeToSpecialMap_Patch
            {
                [HarmonyLib.HarmonyPostfix]
                private static void Postfix()
                {
                    try
                    {
                        BattleObjectManager bom = BattleObjectManager.instance;
                        BattleSoundManager sound = SingletonBehavior<BattleSoundManager>.Instance;
                        if (bom == null || sound == null) return;

                        foreach (BattleUnitModel alive in bom.GetAliveList())
                        {
                            ChangeBGM active = alive?.bufListDetail?.GetActivatedBufList()
                                ?.OfType<ChangeBGM>()
                                .FirstOrDefault();
                            if (active != null && active.Theme[0] != null)
                            {
                                sound.SetEnemyTheme(active.Theme);
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[裁缝之王][BGM] 切图后保持独立 BGM 失败：" + e.Message);
                    }
                }
            }
        }
    }


    public class DiceCardSelfAbility_CX_4Liu20Shang : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时]若目标至少带有4层[流血]则使本书页造成的伤害+20%";

        public override void OnUseCard()
        {
            var target = card?.target;
            if (target == null) return;

            int bleed = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding)?.stack ?? 0;
            if (bleed >= 4)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { dmgRate = 20 });
            }
        }
    }

    public class DiceCardSelfAbility_CX_6Liu20Shang : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时]若目标至少带有6层[流血]则使本书页造成的伤害+20%";

        public override void OnUseCard()
        {
            var target = card?.target;
            if (target == null) return;

            int bleed = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding)?.stack ?? 0;
            if (bleed >= 6)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { dmgRate = 20 });
            }
        }
    }

    public class DiceCardSelfAbility_CX_ZhaoHuan : DiceCardSelfAbilityBase
    {
        public static string Desc = "[战斗开始]这一幕结束时召唤3名友方单位";

        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_Zhaohuan, 1, owner);
            base.OnStartBattle();
            Presentation.Actions.KingSummonPresentation.Register(card);
        }
    }

    public class DiceCardSelfAbility_CX_ShouGongFengZhi : DiceCardSelfAbilityBase
    {
        public override bool IsValidTarget(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            return targetUnit != null && targetUnit.faction == unit.faction;
        }

        public override bool IsOnlyAllyUnit()
        {
            return true;
        }

        public static string Desc = "[优先指定友方单位] [不对友方单位造成伤害与混乱伤害] [战斗开始]若目标为友方单位则使其这一幕与下一幕获得1层[强壮]";

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = card?.target;
            if (target == null || target.faction != owner.faction) return;

            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = -9999, breakRate = -9999 });
        }

        public override void OnStartBattle()
        {
            BattleUnitModel target = card.target;
            if (target == null) return;

            if (target.faction == owner.faction)
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, owner);
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, owner);
            }
            base.OnStartBattle();
        }
    }

    public class DiceCardSelfAbility_CX_NiuKouTaoTai : DiceCardSelfAbilityBase
    {
        public static string Desc = "[拼点失败]使目标的[针钉]层数-1";

        public override void OnLoseParrying()
        {
            BattleUnitModel target = card?.target;
            if (target?.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) is BattleUnitBuf_BF_ZhenDing zhenDing && zhenDing.stack > 0)
            {
                zhenDing.XiaoStack(1);
            }
            base.OnLoseParrying();
        }
    }
}
