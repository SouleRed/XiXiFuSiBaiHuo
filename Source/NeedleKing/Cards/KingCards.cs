using LOR_DiceSystem;
using RedDice;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_CFK_BGM : DiceCardSelfAbilityBase//展开拉取的bgm
    {
        public static string Desc = "[装备时]改变背景音乐";
        public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
            AudioClip clip;
            bool stageData = BattleTools.GetStageData<AudioClip>("CFK_BGM", out clip);
            if (stageData)
            {
                unit.bufListDetail.AddBuf(new DiceCardSelfAbility_CX_CFK_BGM.ChangeBGM(clip));
            }
        }

        public class ChangeBGM : BattleUnitBuf
        {
            public ChangeBGM(AudioClip clip)
            {
                this.array[0] = clip;
                this.array[1] = clip;
                this.array[2] = clip;
            }

            public override void Init(BattleUnitModel owner)
            {
                base.Init(owner);
                int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
                Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 3;
                SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(this.array);
                SingletonBehavior<BattleSoundManager>.Instance.ChangeEnemyTheme(0);
            }

            public override void OnRoundStart()
            {
                int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
                Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 3;
            }

            private readonly AudioClip[] array = new AudioClip[3];
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

    public class DiceCardSelfAbility_CX_WeiShaJiu : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量2] [使用时]目标每带有1层[针钉]使本书页造成的伤害+5%(至多25%)";

        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 5, 25) });
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

    public class DiceCardSelfAbility_CX_20YiShang : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [使用时]目标每带有1层[针钉]使本书页造成的伤害+10%(至多50%)";

        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 10, 50) });
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

    public class DiceCardSelfAbility_CX_ShuangZhenTaoTai : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [使用时]除自身外每有一名友方角色则使本书页所有骰子造成的伤害+50%且威力+4 若场上不存在友方单位则摧毁本书页所有骰子 [攻击结束]移除所有针族并使自身下一幕获得移除的友方单位数的[强壮]";

        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }

        private List<BattleUnitModel> AlivePins(List<BattleUnitModel> aliveList)
            => aliveList.Where(unit =>
                unit != null &&
                unit != owner &&
                unit.passiveDetail.HasPassive<PassiveAbility_7XiaoErJian_CFK>()).ToList();

        public override void OnUseCard()
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction);
            int allies = aliveList.Count(unit => unit != owner);
            if (allies == 0)
            {
                card.RemoveAllDice();
                return;
            }

            Presentation.Actions.KingAllyPresentation.Capture(card, AlivePins(aliveList).ToArray());
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 4 * allies,
                dmgRate = 50 * allies,
            });
        }

        public override void OnEndBattle()
        {
            if (owner == null)
            {
                base.OnEndBattle();
                return;
            }

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction);
            int aliveCount = aliveList.Count(unit => unit != null && unit != owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, aliveCount, owner);

            foreach (BattleUnitModel target in AlivePins(aliveList))
            {
                target.Die(null, true);
            }

            base.OnEndBattle();
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

    public class DiceCardSelfAbility_CX_JiaoJiaoJiao : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[优先指定标本化的目标] [钉层数低于5的单位无法改变本书页的攻击目标] [使用时]目标每带有1层[针钉]使本书页造成的伤害+10%(至多50%)";

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 10, 50) });
            }
        }

        public override bool IsTargetChangable(BattleUnitModel attacker)
        {
            BattleUnitModel target = card?.target;
            return target != null && target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing) >= 5;
        }
    }

    public class DiceCardSelfAbility_CX_BiaoBenHua : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [本书页不造成伤害与混乱伤害] [使用时]摧毁目标当前书页所有骰子";

        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;

            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = -9999, breakRate = -9999 });
        }

        public override void OnUseCard()
        {
            card.target?.currentDiceAction?.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
        }
    }
}
