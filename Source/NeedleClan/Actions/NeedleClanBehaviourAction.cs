using System;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public abstract class NeedleClanBehaviourAction : KingBehaviourAction
{
    protected override bool MatchesSkin(BattleUnitView view) => NativeKingSkin.NeedleId(view) != 0;

    // 追加投掷复用既有骰子 Index，结算日志中仍有独立的记录。
    internal static bool IsRepeat(BattleCardBehaviourResult first, BattleCardBehaviourResult second)
        => first != null && second != null && ReferenceEquals(first.playingCard, second.playingCard)
            && first.behaviourIdx == second.behaviourIdx
            && first.behaviourRawData?.ActionScript == "NeedleClan_S3"
            && second.behaviourRawData?.ActionScript == "NeedleClan_S3";

    protected override List<RencounterManager.MovingAction> BuildDeflection(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        => NeedleClanShortClashes.Build(ref self, ref opponent);

    protected static bool IsRepeatRoll(ref RencounterManager.ActionAfterBehaviour self)
    {
        var result = self.behaviourResultData;
        var log = self.view.model.battleCardResultLog.BehaviourResultList;
        int i = log.IndexOf(result);
        return i > 0 && IsRepeat(log[i - 1], result);
    }
}
