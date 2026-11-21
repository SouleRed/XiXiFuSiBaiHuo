using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;

namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_ShouGongFengZhi : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[优先指定友方单位] [不对友方单位造成伤害与混乱伤害] [战斗开始]若目标为友方单位则使其这一幕与下一幕获得1层[强壮]";
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattlePlayingCardDataInUnitModel card = this.card;
            BattleUnitModel battleUnitModel = (card != null) ? card.target : null;
            if (battleUnitModel == null)
            {
                return;
            }
            if (battleUnitModel.faction == base.owner.faction)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = -9999,
                    breakRate = -9999
                });
            }
        }


        public override void OnStartBattle()
        {
            BattleUnitModel target = base.card.target;
            if (target == null)
            {
                return;
            }
            if (target.faction == base.owner.faction)
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, base.owner);
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, base.owner);
            }
            base.OnStartBattle();
        }
    }

    }

