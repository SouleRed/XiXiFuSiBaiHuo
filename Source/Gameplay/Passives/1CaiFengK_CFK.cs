using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LOR_DiceSystem;

namespace 裁缝之王
{
    public class PassiveAbility_1CaiFengK_CFK : PassiveAbilityBase
    {

        public override bool IsImmune(KeywordBuf buf)
        {
            return buf == KeywordBuf.Stun || buf == KeywordBuf.SealKeyword || buf == KeywordBuf.Seal;
        }
        public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
        {
            if (type == DamageType.Buf && !this.owner.IsBreakLifeZero())
            {
                return 0.5f;
            }
            return base.DmgFactor(dmg, type, keyword);
        }
        public override double ChangeDamage(BattleUnitModel attacker, double dmg)
        {
            double result;
            if (dmg > 20 && !this.owner.IsBreakLifeZero())
            {
                result = 30 + ((dmg - 20) * 0.4);
            }
            else
            {
                result = base.ChangeDamage(attacker, dmg);
            }
            return result;
        }

        public override void OnRoundStart()
        {
            KangXingSet();
            base.OnRoundStart();
        }
        private void KangXingSet()
        {
            this.owner.bufListDetail.RemoveBufAll(typeof(EmotionCardAbility_freischutz3.BattleUnitBuf_Emotion_Fruischutz_Flame));
            this.owner.Book.SetResistHP(BehaviourDetail.Slash, AtkResist.Endure);
            this.owner.Book.SetResistHP(BehaviourDetail.Penetrate, AtkResist.Normal);
            this.owner.Book.SetResistHP(BehaviourDetail.Hit, AtkResist.Resist);
            this.owner.Book.SetResistBP(BehaviourDetail.Slash, AtkResist.Resist);
            this.owner.Book.SetResistBP(BehaviourDetail.Penetrate, AtkResist.Endure);
            this.owner.Book.SetResistBP(BehaviourDetail.Hit, AtkResist.Normal);
        }

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            KangXingSet();
            if (BattleObjectManager.instance.GetAliveList(this.owner.faction).Exists((BattleUnitModel x) => x != this.owner))
            {
                dmg = dmg/2;
            }          
            return base.BeforeTakeDamage(attacker, dmg);
        }

    }
}
