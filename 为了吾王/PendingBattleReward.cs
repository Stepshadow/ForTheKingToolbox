namespace ForTheKingToolbox
{
    internal static class PendingBattleReward
    {
        public static string ItemId { get; private set; } = "";
        public static string ItemName { get; private set; } = "";

        public static bool HasPendingItem
        {
            get
            {
                return ItemId.Length > 0;
            }
        }

        public static void Mark(string itemId, string itemName)
        {
            ItemId = itemId;
            ItemName = itemName;
        }

        public static void Clear()
        {
            ItemId = "";
            ItemName = "";
        }
    }
}