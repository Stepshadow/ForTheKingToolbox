using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ForTheKingToolbox
{
    [BepInPlugin(
        "com.wuyipeng.fortheking.toolbox",
        "For The King Toolbox",
        "0.4.1"
    )]
    public class ForTheKingToolboxPlugin : BaseUnityPlugin
    {
        private bool panelVisible = false;
        private Rect windowRect = new Rect(0, 0, 800, 640);
        private Vector2 scrollPosition = Vector2.zero;

        private string searchText = "";
        private string statusText = "进入冒险后按 G 打开工具箱。";

        private List<ItemEntry> allItems = new List<ItemEntry>();

        private bool searchedChineseColumn = false;
        private FieldInfo chineseNameField = null;
        private PropertyInfo chineseNameProperty = null;

        private void Awake()
        {
            Harmony harmony = new Harmony(
                "com.wuyipeng.fortheking.toolbox"
            );

            harmony.PatchAll();

            Logger.LogInfo("For The King Toolbox loaded.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                panelVisible = !panelVisible;

                if (panelVisible)
                {
                    CenterWindow();
                    LoadItems();
                }
            }
        }

        private void OnGUI()
        {
            if (!panelVisible)
            {
                return;
            }

            windowRect = GUI.Window(
                20260624,
                windowRect,
                DrawToolboxWindow,
                "For The King Toolbox"
            );
        }

        private void CenterWindow()
        {
            windowRect.x = (Screen.width - windowRect.width) / 2f;
            windowRect.y = (Screen.height - windowRect.height) / 2f;
        }

        private void DrawToolboxWindow(int windowId)
        {
            GUI.Label(
                new Rect(15, 32, 650, 22),
                "单击物品即可标记；下一次战斗奖励将包含该物品。"
            );

            GUI.Label(new Rect(15, 58, 45, 22), "搜索：");

            searchText = GUI.TextField(
                new Rect(60, 55, 430, 25),
                searchText
            );

            if (GUI.Button(new Rect(500, 55, 70, 25), "清空"))
            {
                searchText = "";
            }

            if (GUI.Button(new Rect(580, 55, 70, 25), "刷新"))
            {
                LoadItems();
            }

            if (GUI.Button(new Rect(660, 55, 120, 25), "关闭"))
            {
                panelVisible = false;
            }

            GUI.Label(new Rect(15, 87, 760, 22), statusText);

            List<ItemEntry> visibleItems = GetVisibleItems();
            float contentHeight = visibleItems.Count * 38 + 5;

            scrollPosition = GUI.BeginScrollView(
                new Rect(15, 115, 770, 470),
                scrollPosition,
                new Rect(0, 0, 745, contentHeight)
            );

            float y = 5;

            for (int i = 0; i < visibleItems.Count; i++)
            {
                ItemEntry item = visibleItems[i];

                if (GUI.Button(
                    new Rect(5, y, 480, 32),
                    item.DisplayName
                ))
                {
                    MarkItem(item);
                }

                GUI.Label(
                    new Rect(495, y + 7, 240, 22),
                    item.InternalName
                );

                y += 38;
            }

            GUI.EndScrollView();

            GUI.Label(
                new Rect(15, 595, 350, 22),
                "显示 " + visibleItems.Count + " / " + allItems.Count + " 个物品"
            );

            if (PendingBattleReward.HasPendingItem)
            {
                GUI.Label(
                    new Rect(365, 595, 280, 22),
                    "已标记：" + PendingBattleReward.ItemName
                );
            }
            else
            {
                GUI.Label(
                    new Rect(365, 595, 280, 22),
                    "当前未标记物品"
                );
            }

            if (GUI.Button(new Rect(660, 592, 120, 25), "取消标记"))
            {
                PendingBattleReward.Clear();
                statusText = "已取消战斗奖励标记。";
            }

            GUI.DragWindow(new Rect(0, 0, 800, 28));
        }

        private void LoadItems()
        {
            allItems.Clear();

            if (Google2u.TextItems.Instance == null)
            {
                statusText = "物品表还未加载，请先进入一局冒险。";
                return;
            }

            FindChineseNameColumn();

            int index = 0;

            foreach (Google2u.TextItemsRow row in Google2u.TextItems.Instance.Rows)
            {
                if (index >= Google2u.TextItems.Instance.rowNames.Length)
                {
                    break;
                }

                string displayName = GetDisplayName(row);
                string englishName = GetText(row._en);
                string rowName = Google2u.TextItems.Instance.rowNames[index];

                if (!string.IsNullOrEmpty(displayName)
                    && !string.IsNullOrEmpty(rowName)
                    && rowName.Length > 4)
                {
                    string internalName = rowName.Substring(4);

                    allItems.Add(
                        new ItemEntry(
                            displayName,
                            englishName,
                            internalName
                        )
                    );
                }

                index++;
            }

            if (PendingBattleReward.HasPendingItem)
            {
                statusText =
                    "已读取 " + allItems.Count
                    + " 个物品。当前标记："
                    + PendingBattleReward.ItemName;
            }
            else
            {
                statusText = "已读取 " + allItems.Count + " 个物品。";
            }

            Logger.LogInfo("Loaded " + allItems.Count + " items.");
        }

        private void FindChineseNameColumn()
        {
            if (searchedChineseColumn)
            {
                return;
            }

            searchedChineseColumn = true;

            Type rowType = typeof(Google2u.TextItemsRow);

            string[] commonChineseNames = new string[]
            {
                "_zh_cn",
                "_zhcn",
                "_zh",
                "_cn",
                "_ch",
                "_chs",
                "_sc",
                "_chinese",
                "_simplifiedchinese"
            };

            FieldInfo[] fields = rowType.GetFields(
                BindingFlags.Public | BindingFlags.Instance
            );

            for (int i = 0; i < commonChineseNames.Length; i++)
            {
                for (int j = 0; j < fields.Length; j++)
                {
                    if (string.Equals(
                        fields[j].Name,
                        commonChineseNames[i],
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        chineseNameField = fields[j];

                        Logger.LogInfo(
                            "Chinese item column: " + fields[j].Name
                        );

                        return;
                    }
                }
            }

            PropertyInfo[] properties = rowType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            );

            for (int i = 0; i < commonChineseNames.Length; i++)
            {
                for (int j = 0; j < properties.Length; j++)
                {
                    if (properties[j].GetIndexParameters().Length == 0
                        && string.Equals(
                            properties[j].Name,
                            commonChineseNames[i],
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        chineseNameProperty = properties[j];

                        Logger.LogInfo(
                            "Chinese item column: " + properties[j].Name
                        );

                        return;
                    }
                }
            }

            foreach (Google2u.TextItemsRow row in Google2u.TextItems.Instance.Rows)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    string text = GetText(fields[i].GetValue(row));

                    if (ContainsChinese(text))
                    {
                        chineseNameField = fields[i];

                        Logger.LogInfo(
                            "Chinese item column found: " + fields[i].Name
                        );

                        return;
                    }
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    string text = GetText(properties[i].GetValue(row, null));

                    if (ContainsChinese(text))
                    {
                        chineseNameProperty = properties[i];

                        Logger.LogInfo(
                            "Chinese item column found: " + properties[i].Name
                        );

                        return;
                    }
                }
            }

            Logger.LogWarning(
                "Could not find a Chinese item column. English will be used."
            );
        }

        private string GetDisplayName(Google2u.TextItemsRow row)
        {
            string chineseName = "";

            if (chineseNameField != null)
            {
                chineseName = GetText(chineseNameField.GetValue(row));
            }

            if (string.IsNullOrEmpty(chineseName)
                && chineseNameProperty != null)
            {
                chineseName = GetText(
                    chineseNameProperty.GetValue(row, null)
                );
            }

            if (!string.IsNullOrEmpty(chineseName))
            {
                return chineseName;
            }

            return GetText(row._en);
        }

        private bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                if (character >= 0x4E00 && character <= 0x9FFF)
                {
                    return true;
                }
            }

            return false;
        }

        private string GetText(object value)
        {
            if (value == null)
            {
                return "";
            }

            return value.ToString();
        }

        private List<ItemEntry> GetVisibleItems()
        {
            List<ItemEntry> result = new List<ItemEntry>();

            for (int i = 0; i < allItems.Count; i++)
            {
                ItemEntry item = allItems[i];

                if (string.IsNullOrEmpty(searchText)
                    || item.DisplayName.IndexOf(
                        searchText,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                    || item.EnglishName.IndexOf(
                        searchText,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                    || item.InternalName.IndexOf(
                        searchText,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private void MarkItem(ItemEntry item)
        {
            PendingBattleReward.Mark(
                item.InternalName,
                item.DisplayName
            );

            statusText =
                "已标记：" + item.DisplayName
                + "。下一次战斗奖励将尝试出现它。";

            Logger.LogInfo(
                "Marked battle reward item: "
                + item.DisplayName
                + " (" + item.InternalName + ")"
            );
        }

        private class ItemEntry
        {
            public string DisplayName;
            public string EnglishName;
            public string InternalName;

            public ItemEntry(
                string displayName,
                string englishName,
                string internalName
            )
            {
                DisplayName = displayName;
                EnglishName = englishName;
                InternalName = internalName;
            }
        }
    }
}