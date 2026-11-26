using UnityEngine;
using 裁缝之王.Shared.Interop;

namespace 裁缝之王
{
    public class EnemyTeamStageManager_CaiFengKing : EnemyTeamStageManager
    {
        private const int WarmLightPassiveId = 9;

        public override void OnWaveStart()
        {
            base.OnWaveStart();
            if (!LeRougeInterop.Available) return;

            var librarians = BattleObjectManager.instance?.GetAliveList(Faction.Player);
            if (librarians == null) return;

            LorId passiveId = CaiFengKing_Initializer.GetLorId(WarmLightPassiveId);
            foreach (BattleUnitModel unit in librarians)
            {
                if (!LeRougeInterop.IsHeathcliffKing(unit)) continue;
                if (LeRougeInterop.HasWarmLightPassive(unit)
                    || unit.passiveDetail.HasPassiveInReady<PassiveAbility_8LRH_WarmLight_CFK>())
                    continue;

                if (unit.passiveDetail.AddPassive(passiveId) != null)
                {
                    unit.passiveDetail.OnCreated();
                    Debug.Log($"[裁缝之王][红与黑联动] 接待舞台已自动为司书 {unit.UnitData?.unitData?.name} 赋予联动被动(ID={WarmLightPassiveId})。");
                }
            }
        }
    }
}
