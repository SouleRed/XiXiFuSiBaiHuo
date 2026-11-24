using System.Collections.Generic;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_1ZhenLiu : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加1层[流血]与[针钉]";

        public override void OnSucceedAttack()
        {
            BattleUnitModel target = card.target;
            if (target == null) return;

            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Bleeding, 1, owner);
            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 1, owner);
        }
    }

    public class DiceCardAbility_TX_2ZhenLiuQ : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加2层[流血]与[针钉]";

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (target == null) return;

            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Bleeding, 2, owner);
            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 2, owner);
        }
    }

    public class DiceCardAbility_TX_2ZhenLiu : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加2层[流血]与[针钉]";

        public override void OnSucceedAttack()
        {
            BattleUnitModel target = card.target;
            if (target == null) return;

            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Bleeding, 2, owner);
            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 2, owner);
        }
    }

    public class DiceCardAbility_TX_1Zhen : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加1层[针钉]";

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (target == null) return;
            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 1, owner);
        }
    }

    public class DiceCardAbility_TX_ShouGongFengZhi : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标为友方单位则使其对敌方单位使用一张费用最高的书页";

        public override void OnSucceedAttack()
        {
            BattleUnitModel target = card.target;
            if (target == null || target.faction != owner.faction) return;

            int maxCost = 0;
            foreach (BattleDiceCardModel cardModel in target.allyCardDetail.GetAllDeck())
            {
                if (cardModel.GetCost() > maxCost) maxCost = cardModel.GetCost();
            }

            List<BattleDiceCardModel> candidates = target.allyCardDetail.GetAllDeck().FindAll(x => x.GetCost() == maxCost);
            if (candidates.Count == 0) return;

            BattleDiceCardModel chosen = RandomUtil.SelectOne(candidates);
            if (chosen == null) return;

            foreach (BattleUnitModel enemy in BattleObjectManager.instance.GetAliveList_random(owner.faction == Faction.Enemy ? Faction.Player : Faction.Enemy, 1))
            {
                BattleTools.AddCardAttack(target, enemy, chosen.GetID());
            }
        }
    }

    public class DiceCardAbility_TX_ShuangZhenTaoTai : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标带有[标本化]则造成的伤害+100%";

        public override void BeforeRollDice()
        {
            BattleUnitModel target = card?.target;
            if (target == null) return;

            BattleUnitBuf buf = target.bufListDetail.GetActivatedBufList().Find(x => x.bufType == KeywordHao.BF_BiaoBenHua);
            if (buf != null)
            {
                behavior?.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 100 });
            }
        }
    }

    public class DiceCardAbility_TX_NiuKouTaoTai : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]追加目标[针钉]层数的伤害";

        public override void OnSucceedAttack()
        {
            BattleUnitModel target = card.target;
            if (target == null) return;

            BattleUnitBuf activatedBuf = target.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing);
            if (activatedBuf != null && activatedBuf.stack > 0)
            {
                target.TakeDamage(activatedBuf.stack, DamageType.Card_Ability, owner, KeywordBuf.None);
            }
        }
    }

    public class DiceCardAbility_TX_JiaoJiaoJiao : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标带有[标本化]则追加目标体力上限120%的伤害自身恢复体力上限8%的体力";//即死代码在斗蛐蛐时可能会无视对面锁血，因此修改为高额追伤

        public override void OnSucceedAttack()
        {
            
            BattleUnitModel target = card.target;
            if (target == null) return;
            BattleUnitBuf buf = target.bufListDetail.GetActivatedBufList().Find(x => x.bufType == KeywordHao.BF_BiaoBenHua);
            if (buf != null)
            {
                target.TakeDamage((int)(target.MaxHp * 1.2f), DamageType.ETC, target, KeywordBuf.None);
                owner.RecoverHP((int)(owner.MaxHp * 0.08f));
                if ((int)target.hp <= (int)target.MaxHp / 2)
                {
                    //target.Die(target, true);
                }
            }
        }
    }

    public class DiceCardAbility_TX_BiaoBenHua : DiceCardAbilityBase
{
    public static string Desc = "[命中时]若目标[针钉]层数不低于5则对其施加2层[标本化]";

    private int _zhenDingBeforeAttack;

    public override void BeforeRollDice()
    {
        BattleUnitModel target = card?.target;
        _zhenDingBeforeAttack =
            target?.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing) ?? 0;
    }

    public override void OnSucceedAttack(BattleUnitModel target)
    {
        if (target == null) return;

        if (_zhenDingBeforeAttack >= 5)
        {
            target.bufListDetail.AddKeywordBufThisRoundByCard(
                KeywordHao.BF_BiaoBenHua, 2, owner);
        }

        base.OnSucceedAttack(target);
    }
}
}
