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
        "0.7.0"
    )]
    public class ForTheKingToolboxPlugin : BaseUnityPlugin
    {
        private const float WindowWidth = 900f;
        private const float WindowHeight = 700f;
        private const float HoverCardDelay = 0.25f;

        private static ForTheKingToolboxPlugin instance;

        private bool panelVisible = false;

        private Rect windowRect =
            new Rect(0f, 0f, WindowWidth, WindowHeight);

        private Vector2 scrollPosition = Vector2.zero;

        private string searchText = "";

        private string statusText =
            "进入冒险后按 G 打开工具箱。";

        private List<ItemEntry> allItems =
            new List<ItemEntry>();

        private Dictionary<string, Sprite> iconCache =
            new Dictionary<string, Sprite>();

        private bool searchedChineseColumn = false;

        private FieldInfo chineseNameField = null;

        private PropertyInfo chineseNameProperty = null;

        private ItemEntry hoverCandidateItem = null;

        private float hoverCandidateStartTime = 0f;

        private GridEditor.FTK_itembase.ID shownCardItem =
            GridEditor.FTK_itembase.ID.None;

        private bool nativeCardShown = false;

        private bool rtsCameraMouseBlocked = false;

        private bool rtsCameraMouseWasEnabled = false;

        private bool stylesReady = false;

        private Texture2D pixelTexture = null;
        private Texture2D windowTexture = null;
        private Texture2D headerTexture = null;
        private Texture2D listTexture = null;
        private Texture2D rowTexture = null;
        private Texture2D rowHoverTexture = null;
        private Texture2D rowActiveTexture = null;
        private Texture2D buttonTexture = null;
        private Texture2D buttonHoverTexture = null;
        private Texture2D buttonActiveTexture = null;
        private Texture2D searchTexture = null;
        private Texture2D searchFocusedTexture = null;

        private GUIStyle windowStyle = null;
        private GUIStyle titleStyle = null;
        private GUIStyle subtitleStyle = null;
        private GUIStyle normalLabelStyle = null;
        private GUIStyle smallLabelStyle = null;
        private GUIStyle statusLabelStyle = null;
        private GUIStyle searchFieldStyle = null;
        private GUIStyle buttonStyle = null;
        private GUIStyle rowStyle = null;
        private GUIStyle itemNameStyle = null;
        private GUIStyle itemIdStyle = null;
        private GUIStyle itemActionStyle = null;
        private GUIStyle iconFallbackStyle = null;

        private void Awake()
        {
            instance = this;

            Harmony harmony = new Harmony(
                "com.wuyipeng.fortheking.toolbox"
            );

            harmony.PatchAll();

            Logger.LogInfo("For The King Toolbox loaded.");
        }

        private void OnDisable()
        {
            CloseNativeItemCard();

            Behaviour rtsCameraMouse =
                GetRtsCameraMouseBehaviour();

            if (rtsCameraMouse != null
                && rtsCameraMouseBlocked)
            {
                rtsCameraMouse.enabled =
                    rtsCameraMouseWasEnabled;
            }

            rtsCameraMouseBlocked = false;

            if (instance == this)
            {
                instance = null;
            }
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
                else
                {
                    CloseNativeItemCard();
                }
            }
        }

        private void LateUpdate()
        {
            bool shouldBlockCameraMouse =
                panelVisible
                && IsMouseOverWindow();

            Behaviour rtsCameraMouse =
                GetRtsCameraMouseBehaviour();

            if (rtsCameraMouse == null)
            {
                return;
            }

            if (shouldBlockCameraMouse)
            {
                if (!rtsCameraMouseBlocked)
                {
                    rtsCameraMouseWasEnabled =
                        rtsCameraMouse.enabled;

                    rtsCameraMouseBlocked = true;
                }

                rtsCameraMouse.enabled = false;
            }
            else if (rtsCameraMouseBlocked)
            {
                rtsCameraMouse.enabled =
                    rtsCameraMouseWasEnabled;

                rtsCameraMouseBlocked = false;
            }
        }

        private Behaviour GetRtsCameraMouseBehaviour()
        {
            if (FTKHub.Instance == null)
            {
                return null;
            }

            object cameraMouse =
                FTKHub.Instance.m_RtsCameraMouse;

            return cameraMouse as Behaviour;
        }

        private void OnGUI()
        {
            if (!panelVisible)
            {
                return;
            }

            EnsureStyles();

            windowRect = GUI.Window(
                20260624,
                windowRect,
                DrawToolboxWindow,
                GUIContent.none,
                windowStyle
            );

            if (IsMouseOverWindow()
                && Event.current != null
                && IsMouseEvent(Event.current.type))
            {
                Event.current.Use();
            }
        }

        public static bool IsMouseOverToolbox()
        {
            if (instance == null)
            {
                return false;
            }

            if (!instance.panelVisible)
            {
                return false;
            }

            return instance.IsMouseOverWindow();
        }

        private bool IsMouseOverWindow()
        {
            Vector2 mousePosition = new Vector2(
                Input.mousePosition.x,
                Screen.height - Input.mousePosition.y
            );

            return windowRect.Contains(mousePosition);
        }

        private bool IsMouseEvent(EventType eventType)
        {
            return eventType == EventType.MouseDown
                || eventType == EventType.MouseUp
                || eventType == EventType.MouseDrag
                || eventType == EventType.ScrollWheel
                || eventType == EventType.ContextClick;
        }

        private void CenterWindow()
        {
            windowRect.x = 18f;
            windowRect.y = 18f;
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            pixelTexture = CreateTexture(Color.white);

            windowTexture = CreateTexture(
                new Color(0.045f, 0.058f, 0.070f, 0.985f)
            );

            headerTexture = CreateTexture(
                new Color(0.090f, 0.112f, 0.126f, 1f)
            );

            listTexture = CreateTexture(
                new Color(0.025f, 0.034f, 0.043f, 0.96f)
            );

            rowTexture = CreateTexture(
                new Color(0.085f, 0.105f, 0.118f, 0.98f)
            );

            rowHoverTexture = CreateTexture(
                new Color(0.145f, 0.170f, 0.180f, 1f)
            );

            rowActiveTexture = CreateTexture(
                new Color(0.190f, 0.155f, 0.090f, 1f)
            );

            buttonTexture = CreateTexture(
                new Color(0.190f, 0.150f, 0.075f, 1f)
            );

            buttonHoverTexture = CreateTexture(
                new Color(0.290f, 0.225f, 0.105f, 1f)
            );

            buttonActiveTexture = CreateTexture(
                new Color(0.120f, 0.090f, 0.040f, 1f)
            );

            searchTexture = CreateTexture(
                new Color(0.020f, 0.028f, 0.036f, 1f)
            );

            searchFocusedTexture = CreateTexture(
                new Color(0.055f, 0.073f, 0.087f, 1f)
            );

            windowStyle = new GUIStyle();

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.font = GUI.skin.font;
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            titleStyle.normal.textColor =
                new Color(0.93f, 0.78f, 0.38f, 1f);

            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.font = GUI.skin.font;
            subtitleStyle.fontSize = 12;
            subtitleStyle.alignment = TextAnchor.MiddleLeft;
            subtitleStyle.normal.textColor =
                new Color(0.68f, 0.73f, 0.73f, 1f);

            normalLabelStyle = new GUIStyle(GUI.skin.label);
            normalLabelStyle.font = GUI.skin.font;
            normalLabelStyle.fontSize = 14;
            normalLabelStyle.alignment = TextAnchor.MiddleLeft;
            normalLabelStyle.normal.textColor =
                new Color(0.88f, 0.89f, 0.85f, 1f);

            smallLabelStyle = new GUIStyle(GUI.skin.label);
            smallLabelStyle.font = GUI.skin.font;
            smallLabelStyle.fontSize = 12;
            smallLabelStyle.alignment = TextAnchor.MiddleLeft;
            smallLabelStyle.normal.textColor =
                new Color(0.58f, 0.65f, 0.67f, 1f);

            statusLabelStyle = new GUIStyle(GUI.skin.label);
            statusLabelStyle.font = GUI.skin.font;
            statusLabelStyle.fontSize = 13;
            statusLabelStyle.alignment = TextAnchor.MiddleLeft;
            statusLabelStyle.normal.textColor =
                new Color(0.77f, 0.82f, 0.77f, 1f);

            searchFieldStyle = new GUIStyle(GUI.skin.textField);
            searchFieldStyle.font = GUI.skin.font;
            searchFieldStyle.fontSize = 14;
            searchFieldStyle.alignment = TextAnchor.MiddleLeft;
            searchFieldStyle.padding =
                new RectOffset(10, 10, 5, 5);
            searchFieldStyle.normal.background = searchTexture;
            searchFieldStyle.focused.background =
                searchFocusedTexture;
            searchFieldStyle.normal.textColor =
                new Color(0.92f, 0.92f, 0.88f, 1f);
            searchFieldStyle.focused.textColor =
                Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.font = GUI.skin.font;
            buttonStyle.fontSize = 13;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.padding =
                new RectOffset(6, 6, 2, 2);
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.hover.background = buttonHoverTexture;
            buttonStyle.active.background = buttonActiveTexture;
            buttonStyle.normal.textColor =
                new Color(0.96f, 0.91f, 0.74f, 1f);
            buttonStyle.hover.textColor =
                Color.white;
            buttonStyle.active.textColor =
                new Color(0.80f, 0.76f, 0.64f, 1f);

            rowStyle = new GUIStyle(GUI.skin.button);
            rowStyle.padding =
                new RectOffset(0, 0, 0, 0);
            rowStyle.normal.background = rowTexture;
            rowStyle.hover.background = rowHoverTexture;
            rowStyle.active.background = rowActiveTexture;

            itemNameStyle = new GUIStyle(GUI.skin.label);
            itemNameStyle.font = GUI.skin.font;
            itemNameStyle.fontSize = 15;
            itemNameStyle.fontStyle = FontStyle.Bold;
            itemNameStyle.alignment = TextAnchor.MiddleLeft;
            itemNameStyle.normal.textColor =
                new Color(0.93f, 0.90f, 0.79f, 1f);

            itemIdStyle = new GUIStyle(GUI.skin.label);
            itemIdStyle.font = GUI.skin.font;
            itemIdStyle.fontSize = 11;
            itemIdStyle.alignment = TextAnchor.MiddleLeft;
            itemIdStyle.normal.textColor =
                new Color(0.57f, 0.65f, 0.69f, 1f);

            itemActionStyle = new GUIStyle(GUI.skin.label);
            itemActionStyle.font = GUI.skin.font;
            itemActionStyle.fontSize = 12;
            itemActionStyle.fontStyle = FontStyle.Bold;
            itemActionStyle.alignment = TextAnchor.MiddleCenter;
            itemActionStyle.normal.textColor =
                new Color(0.92f, 0.72f, 0.30f, 1f);

            iconFallbackStyle = new GUIStyle(GUI.skin.label);
            iconFallbackStyle.font = GUI.skin.font;
            iconFallbackStyle.fontSize = 22;
            iconFallbackStyle.fontStyle = FontStyle.Bold;
            iconFallbackStyle.alignment = TextAnchor.MiddleCenter;
            iconFallbackStyle.normal.textColor =
                new Color(0.75f, 0.60f, 0.28f, 1f);

            stylesReady = true;
        }

        private Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false
            );

            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        private void DrawToolboxWindow(int windowId)
        {
            DrawWindowFrame();

            GUI.Label(
                new Rect(24, 9, 550, 28),
                "FOR THE KING  ·  TOOLBOX",
                titleStyle
            );

            GUI.Label(
                new Rect(25, 34, 650, 18),
                "物品补给台 · 悬停查看属性 · 点击加入当前角色背包",
                subtitleStyle
            );

            GUI.Label(
                new Rect(24, 67, 48, 24),
                "搜索",
                normalLabelStyle
            );

            searchText = GUI.TextField(
                new Rect(72, 65, 480, 28),
                searchText,
                searchFieldStyle
            );

            if (GUI.Button(
                new Rect(565, 65, 82, 28),
                "清空",
                buttonStyle
            ))
            {
                searchText = "";
            }

            if (GUI.Button(
                new Rect(657, 65, 102, 28),
                "刷新物品",
                buttonStyle
            ))
            {
                LoadItems();
            }

            if (GUI.Button(
                new Rect(769, 65, 106, 28),
                "关闭  G",
                buttonStyle
            ))
            {
                panelVisible = false;
                CloseNativeItemCard();
                return;
            }

            GUI.Label(
                new Rect(24, 101, 850, 22),
                statusText,
                statusLabelStyle
            );

            List<ItemEntry> visibleItems =
                GetVisibleItems();

            float contentHeight = Mathf.Max(
                visibleItems.Count * 56f + 8f,
                494f
            );

            ItemEntry hoveredItem = null;

            scrollPosition = GUI.BeginScrollView(
                new Rect(20, 135, 860, 500),
                scrollPosition,
                new Rect(0, 0, 835, contentHeight),
                false,
                true
            );

            float y = 5f;

            for (int i = 0; i < visibleItems.Count; i++)
            {
                ItemEntry item = visibleItems[i];

                Rect rowRect =
                    new Rect(5, y, 812, 50);

                if (Event.current != null
                    && rowRect.Contains(
                        Event.current.mousePosition
                    ))
                {
                    hoveredItem = item;
                }

                if (GUI.Button(
                    rowRect,
                    GUIContent.none,
                    rowStyle
                ))
                {
                    GiveItemToLocalPlayer(item);
                }

                DrawItemIcon(
                    new Rect(
                        rowRect.x + 6,
                        rowRect.y + 5,
                        40,
                        40
                    ),
                    item
                );

                GUI.Label(
                    new Rect(
                        rowRect.x + 58,
                        rowRect.y + 6,
                        590,
                        21
                    ),
                    item.DisplayName,
                    itemNameStyle
                );

                GUI.Label(
                    new Rect(
                        rowRect.x + 58,
                        rowRect.y + 28,
                        590,
                        16
                    ),
                    item.InternalName,
                    itemIdStyle
                );

                GUI.Label(
                    new Rect(
                        rowRect.x + 680,
                        rowRect.y + 14,
                        110,
                        22
                    ),
                    "点击获取",
                    itemActionStyle
                );

                y += 56f;
            }

            GUI.EndScrollView();

            if (Event.current != null
                && Event.current.type == EventType.Repaint)
            {
                UpdateNativeCardHover(hoveredItem);
            }

            GUI.Label(
                new Rect(24, 651, 360, 22),
                "显示 "
                + visibleItems.Count
                + " / "
                + allItems.Count
                + " 个物品",
                smallLabelStyle
            );

            GUI.Label(
                new Rect(385, 651, 490, 22),
                "鼠标位于窗口内时，地图缩放与点击会被拦截",
                smallLabelStyle
            );

            GUI.DragWindow(
                new Rect(0, 0, WindowWidth, 54)
            );
        }

        private void UpdateNativeCardHover(
            ItemEntry hoveredItem
        )
        {
            if (hoveredItem == null)
            {
                hoverCandidateItem = null;

                CloseNativeItemCard();

                return;
            }

            if (hoveredItem.ItemId
                == GridEditor.FTK_itembase.ID.None)
            {
                hoverCandidateItem = null;

                CloseNativeItemCard();

                return;
            }

            if (hoverCandidateItem != hoveredItem)
            {
                hoverCandidateItem = hoveredItem;

                hoverCandidateStartTime =
                    Time.realtimeSinceStartup;

                if (nativeCardShown
                    && shownCardItem != hoveredItem.ItemId)
                {
                    CloseNativeItemCard();
                }

                return;
            }

            if (nativeCardShown
                && shownCardItem == hoveredItem.ItemId)
            {
                return;
            }

            if (Time.realtimeSinceStartup
                - hoverCandidateStartTime
                < HoverCardDelay)
            {
                return;
            }

            ShowNativeItemCard(hoveredItem);
        }

        private void ShowNativeItemCard(ItemEntry item)
        {
            CharacterOverworld localCharacter;

            if (!TryGetLocalCharacter(
                out localCharacter
            ))
            {
                return;
            }

            if (FTKUI.Instance == null)
            {
                return;
            }

            if (FTKUI.Instance.m_ItemCardDisplay == null)
            {
                return;
            }

            try
            {
                FTKUI.Instance.m_ItemCardDisplay.Show(
                    item.ItemId,
                    null,
                    localCharacter,
                    uiItemDetail.Mode.ItemDisplay,
                    "Item",
                    null,
                    true,
                    0,
                    null,
                    false,
                    true
                );

                shownCardItem = item.ItemId;
                nativeCardShown = true;
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    "Could not show native item card: "
                    + exception
                );
            }
        }

        private void CloseNativeItemCard()
        {
            if (!nativeCardShown)
            {
                return;
            }

            try
            {
                if (FTKUI.Instance != null
                    && FTKUI.Instance.m_ItemCardDisplay != null)
                {
                    FTKUI.Instance.m_ItemCardDisplay.Close(
                        uiItemDetail.Mode.ItemDisplay
                    );
                }
            }
            catch
            {
            }

            nativeCardShown = false;

            shownCardItem =
                GridEditor.FTK_itembase.ID.None;
        }

        private void DrawWindowFrame()
        {
            Rect window =
                new Rect(0f, 0f, WindowWidth, WindowHeight);

            Rect header =
                new Rect(0f, 0f, WindowWidth, 54f);

            Rect list =
                new Rect(18f, 133f, 864f, 504f);

            DrawSolidRect(window, windowTexture);

            DrawBorder(
                window,
                new Color(0.70f, 0.53f, 0.22f, 1f),
                2f
            );

            DrawSolidRect(header, headerTexture);

            DrawSolidRect(
                new Rect(0f, 52f, WindowWidth, 2f),
                new Color(0.72f, 0.55f, 0.22f, 1f)
            );

            DrawSolidRect(list, listTexture);

            DrawBorder(
                list,
                new Color(0.30f, 0.36f, 0.37f, 1f),
                1f
            );

            DrawSolidRect(
                new Rect(18f, 637f, 864f, 1f),
                new Color(0.30f, 0.36f, 0.37f, 1f)
            );
        }

        private void DrawItemIcon(
            Rect rect,
            ItemEntry item
        )
        {
            DrawSolidRect(
                new Rect(
                    rect.x - 1,
                    rect.y - 1,
                    rect.width + 2,
                    rect.height + 2
                ),
                new Color(0.52f, 0.40f, 0.17f, 1f)
            );

            DrawSolidRect(
                rect,
                new Color(0.020f, 0.026f, 0.031f, 1f)
            );

            Sprite icon = GetItemIcon(item);

            if (icon != null
                && icon.texture != null)
            {
                Rect source = icon.textureRect;

                Rect uv = new Rect(
                    source.x / icon.texture.width,
                    source.y / icon.texture.height,
                    source.width / icon.texture.width,
                    source.height / icon.texture.height
                );

                GUI.DrawTextureWithTexCoords(
                    rect,
                    icon.texture,
                    uv
                );
            }
            else
            {
                GUI.Label(
                    rect,
                    "?",
                    iconFallbackStyle
                );
            }
        }

        private Sprite GetItemIcon(ItemEntry item)
        {
            Sprite cachedIcon = null;

            if (iconCache.TryGetValue(
                item.InternalName,
                out cachedIcon
            ))
            {
                return cachedIcon;
            }

            Sprite icon = null;

            try
            {
                if (item.ItemId
                    != GridEditor.FTK_itembase.ID.None)
                {
                    icon = global::GameCache.Cache.Items.GetIcon(
                        item.ItemId
                    );
                }
            }
            catch (Exception exception)
            {
                Logger.LogWarning(
                    "Could not load item icon: "
                    + exception.Message
                );
            }

            iconCache[item.InternalName] = icon;

            return icon;
        }

        private void DrawSolidRect(
            Rect rect,
            Texture texture
        )
        {
            GUI.DrawTexture(rect, texture);
        }

        private void DrawSolidRect(
            Rect rect,
            Color color
        )
        {
            Color oldColor = GUI.color;

            GUI.color = color;

            GUI.DrawTexture(
                rect,
                pixelTexture
            );

            GUI.color = oldColor;
        }

        private void DrawBorder(
            Rect rect,
            Color color,
            float thickness
        )
        {
            Color oldColor = GUI.color;

            GUI.color = color;

            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    thickness
                ),
                pixelTexture
            );

            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.yMax - thickness,
                    rect.width,
                    thickness
                ),
                pixelTexture
            );

            GUI.DrawTexture(
                new Rect(
                    rect.x,
                    rect.y,
                    thickness,
                    rect.height
                ),
                pixelTexture
            );

            GUI.DrawTexture(
                new Rect(
                    rect.xMax - thickness,
                    rect.y,
                    thickness,
                    rect.height
                ),
                pixelTexture
            );

            GUI.color = oldColor;
        }

        private void GiveItemToLocalPlayer(ItemEntry item)
        {
            CharacterOverworld localCharacter;

            if (!TryGetLocalCharacter(
                out localCharacter
            ))
            {
                statusText =
                    "找不到本机角色，请先进入一局冒险。";

                return;
            }

            if (item.ItemId
                == GridEditor.FTK_itembase.ID.None)
            {
                statusText =
                    "无法识别物品编号："
                    + item.InternalName;

                return;
            }

            try
            {
                localCharacter.AddItemToBackpack(
                    item.ItemId,
                    true
                );

                statusText =
                    "已加入背包："
                    + item.DisplayName;
            }
            catch (Exception exception)
            {
                statusText =
                    "添加失败，请查看 BepInEx 日志。";

                Logger.LogError(
                    "Failed to add item: "
                    + exception
                );
            }
        }

        private bool TryGetLocalCharacter(
            out CharacterOverworld localCharacter
        )
        {
            localCharacter = null;

            if (FTKHub.Instance == null)
            {
                return false;
            }

            if (FTKHub.Instance.m_CharacterOverworlds
                == null)
            {
                return false;
            }

            List<CharacterOverworld> characters =
                FTKHub.Instance.m_CharacterOverworlds;

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterOverworld character =
                    characters[i];

                if (character == null)
                {
                    continue;
                }

                if (character.m_PhotonView == null)
                {
                    continue;
                }

                if (character.m_PhotonView.isMine)
                {
                    localCharacter = character;

                    return true;
                }
            }

            return false;
        }

        private void LoadItems()
        {
            allItems.Clear();
            iconCache.Clear();

            if (Google2u.TextItems.Instance == null)
            {
                statusText =
                    "物品表尚未加载，请先进入一局冒险。";

                return;
            }

            FindChineseNameColumn();

            int index = 0;

            foreach (
                Google2u.TextItemsRow row
                in Google2u.TextItems.Instance.Rows
            )
            {
                if (index
                    >= Google2u.TextItems.Instance.rowNames.Length)
                {
                    break;
                }

                string displayName = GetDisplayName(row);

                string englishName = GetText(row._en);

                string rowName =
                    Google2u.TextItems.Instance.rowNames[index];

                if (!string.IsNullOrEmpty(displayName)
                    && !string.IsNullOrEmpty(rowName)
                    && rowName.Length > 4)
                {
                    string internalName =
                        rowName.Substring(4);

                    GridEditor.FTK_itembase.ID itemId =
                        GridEditor.FTK_itembase.GetEnum(
                            internalName
                        );

                    allItems.Add(
                        new ItemEntry(
                            displayName,
                            englishName,
                            internalName,
                            itemId
                        )
                    );
                }

                index++;
            }

            statusText =
                "已读取 "
                + allItems.Count
                + " 个物品。";
        }

        private void FindChineseNameColumn()
        {
            if (searchedChineseColumn)
            {
                return;
            }

            searchedChineseColumn = true;

            Type rowType =
                typeof(Google2u.TextItemsRow);

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
                BindingFlags.Public
                | BindingFlags.Instance
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
                        return;
                    }
                }
            }

            PropertyInfo[] properties =
                rowType.GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance
                );

            for (int i = 0; i < commonChineseNames.Length; i++)
            {
                for (int j = 0; j < properties.Length; j++)
                {
                    if (properties[j]
                        .GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    if (string.Equals(
                        properties[j].Name,
                        commonChineseNames[i],
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        chineseNameProperty = properties[j];
                        return;
                    }
                }
            }

            foreach (
                Google2u.TextItemsRow row
                in Google2u.TextItems.Instance.Rows
            )
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    string text =
                        GetText(fields[i].GetValue(row));

                    if (ContainsChinese(text))
                    {
                        chineseNameField = fields[i];
                        return;
                    }
                }

                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i]
                        .GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    string text = GetText(
                        properties[i].GetValue(
                            row,
                            null
                        )
                    );

                    if (ContainsChinese(text))
                    {
                        chineseNameProperty = properties[i];
                        return;
                    }
                }
            }
        }

        private string GetDisplayName(
            Google2u.TextItemsRow row
        )
        {
            string chineseName = "";

            if (chineseNameField != null)
            {
                chineseName = GetText(
                    chineseNameField.GetValue(row)
                );
            }

            if (string.IsNullOrEmpty(chineseName)
                && chineseNameProperty != null)
            {
                chineseName = GetText(
                    chineseNameProperty.GetValue(
                        row,
                        null
                    )
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

                if (character >= 0x4E00
                    && character <= 0x9FFF)
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
            List<ItemEntry> result =
                new List<ItemEntry>();

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

        private class ItemEntry
        {
            public string DisplayName;
            public string EnglishName;
            public string InternalName;

            public GridEditor.FTK_itembase.ID ItemId;

            public ItemEntry(
                string displayName,
                string englishName,
                string internalName,
                GridEditor.FTK_itembase.ID itemId
            )
            {
                DisplayName = displayName;
                EnglishName = englishName;
                InternalName = internalName;
                ItemId = itemId;
            }
        }
    }

    [HarmonyPatch(typeof(FTKUI), "IsMouseOverUI")]
    internal static class ToolboxMouseBlockPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (ForTheKingToolboxPlugin.IsMouseOverToolbox())
            {
                __result = true;
            }
        }
    }
}