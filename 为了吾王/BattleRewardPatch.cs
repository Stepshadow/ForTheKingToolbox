using System.Collections.Generic;
using HarmonyLib;

namespace ForTheKingToolbox
{
    [HarmonyPatch(
        typeof(GridEditor.FTK_enemyCombat.ItemDrops),
        "GetLootItems"
    )]
    internal static class BattleRewardPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            List<GridEditor.FTK_itembase.ID> __result
        )
        {
            if (!PendingBattleReward.HasPendingItem)
            {
                return;
            }

            if (__result == null)
            {
                return;
            }

            GridEditor.FTK_itembase.ID itemId =
                GridEditor.FTK_itembase.GetEnum(
                    PendingBattleReward.ItemId
                );

            if (itemId == GridEditor.FTK_itembase.ID.None)
            {
                return;
            }

            if (!__result.Contains(itemId))
            {
                __result.Add(itemId);
            }

            PendingBattleReward.Clear();
        }
    }
}