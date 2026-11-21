namespace 裁缝之王
{
    public class CaiFengZhiWangManager : CreatureMapManager
    {
        public void Start()
        {
            if (!isEgo) SendMessage("SetMapBgm", "BGM1.wav");
        }

        public override void EnableMap(bool b)
        {
            isEnabled = b;
            gameObject.SetActive(b);
        }

        public override void OnRoundStart()
        {
            if (isEgo) return;
            SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(mapBgm);
            int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
            Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
        }
    }
}
