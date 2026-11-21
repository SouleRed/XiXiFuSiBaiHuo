using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace 裁缝之王
{
    public class KaoLengMianMapManager : CreatureMapManager
    {
        public void Start()
        {
            if (!this.isEgo)
            {
                this.SendMessage("SetMapBgm", "BGM1.wav");
                //SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject.SendMessage("SetMapBgm", "BGM文件名");
                //SingletonBehavior<BattleSoundManager>.Instance.EndBgm();
                //SingletonBehavior<BattleSoundManager>.Instance.StartBgm();
            }
        }

        public override void EnableMap(bool b)
        {          
            this.isEnabled = b;
            this.gameObject.SetActive(b);
        }

        public override void OnRoundStart()
        {
            if (!this.isEgo)
            {
                SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(this.mapBgm);
                int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
                Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
            }
        }
    }
}
