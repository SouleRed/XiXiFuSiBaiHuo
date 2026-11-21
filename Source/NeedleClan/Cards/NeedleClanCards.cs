using System;
using LOR_DiceSystem;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_4Liu1WLi : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时]若目标至少带有4层[流血]则使本书页所有骰子威力+1";

        public override void OnUseCard()
        {
            var target = card?.target;
            if (target == null) return;

            int bleed = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding)?.stack ?? 0;
            if (bleed >= 4)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 1 });
            }
        }
    }

    public class DiceCardSelfAbility_CX_XiaoDaZhao : DiceCardSelfAbilityBase
    {
        public static string Desc = "[使用时]目标每带有4层[流血]则使本书页所有骰子威力+1(至多+2)";

        public override void OnUseCard()
        {
            var target = card?.target;
            if (target == null) return;

            int bleed = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding)?.stack ?? 0;
            if (Math.Min(bleed / 4, 2) > 0)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 1 });
            }
        }
    }
}
