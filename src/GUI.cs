using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Atlyss_DPSUI {

    public enum UIMode {
        Auto,
        Boss,
        Party
    }

    public class DPSUI_GUI : Boolable {

        internal class PartyMemberBar {
            internal GameObject self;

            private Text memberInfo;
            private Text damageText;

            private RectTransform background;
            private RectTransform fillBar;

            private Image classImg;
            public Color fillColor;
            private float dataUpdateTime;

            internal PartyMemberBar(GameObject self) {
                this.self = self;
                fillColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                AddBackground(self);

                Texture2D val = new Texture2D(1, 1);
                val.SetPixel(0, 0, Color.white);

                GameObject val2 = AddBackground(self, val);
                val2.name = "Fillbar";
                fillBar = val2.GetComponent<RectTransform>();
                fillBar.sizeDelta = new Vector2(-3f, -2f);
                fillBar.GetComponent<Image>().color = fillColor;

                GameObject val3 = new GameObject("ClassImage", typeof(RectTransform), typeof(Image));
                classImg = val3.GetComponent<Image>();
                setParent(val3, self);
                self.GetComponent<RectTransform>();
                RectTransform component = val3.GetComponent<RectTransform>();
                component.anchorMin = new Vector2(0.01f, 0.3f);
                component.anchorMax = new Vector2(0.09f, 0.7f);
                component.sizeDelta = Vector2.zero;
                component.pivot = component.anchorMin;
                component.anchoredPosition = Vector2.zero;

                int num = 0;
                while (classImages != null && num < classImages.Length) {
                    if (classImages[num] && classImages[num].name == "_clsIco_novice") {
                        classImg.sprite = Sprite.Create(classImages[num], new Rect(0f, 0f, (float)((Texture)classImages[num]).width, (float)((Texture)classImages[num]).height), new Vector2(0.5f, 0.5f));
                        break;
                    }
                    num++;
                }

                GameObject val4 = new GameObject("MemberName", typeof(Text));
                setupRectTransform(val4, new Vector2(0.16f, 0.5f), self, ignoreParentRect: true);
                memberInfo = setupText(val4, 20);
                val4.AddComponent<Shadow>();
                memberInfo.alignment = TextAnchor.MiddleLeft;
                memberInfo.text = "Dumbass";

                val4 = new GameObject("DamageText", typeof(Text));
                setupRectTransform(val4, new Vector2(0.97f, 0.5f), self, ignoreParentRect: true);
                damageText = setupText(val4, 20);
                val4.AddComponent<Shadow>();
                damageText.alignment = TextAnchor.MiddleRight;
                damageText.text = "Fuck all";
            }

            internal void UpdateInfo(DPSValues info, float barFillPercent) {
                memberInfo.text = info.nickname;
                if (info.steamID == Player._mainPlayer.Network_steamID)
                    memberInfo.text += " (You)";

                string text = info.totalDamage.ToString("n1");
                if (text.EndsWith(".0"))
                    text = text.Substring(0, text.Length - 2);

                damageText.text = text;
                barFillPercent = Mathf.Max(barFillPercent, 1E-05f);
                fillBar.anchorMax = new Vector2(barFillPercent, 1f);
                self.SetActive(true);

                if (info.classIcon != classImg.sprite.texture.name) {
                    for (int i = 0; i < classImages.Length; i++) {
                        if (classImages[i] && classImages[i].name == info.classIcon) {
                            classImg.sprite = Sprite.Create(classImages[i], new Rect(0f, 0f, classImages[i].width, classImages[i].height), new Vector2(0.5f, 0.5f));
                            break;
                        }
                    }
                }

                try {
                    uint num = Convert.ToUInt32(info.color, 16);
                    if (num != 0) {
                        Color32 fillColor = new Color32 {
                            r = (byte)((num >> 24) & 0xFF),
                            g = (byte)((num >> 16) & 0xFF),
                            b = (byte)((num >> 8) & 0xFF),
                            a = byte.MaxValue
                        };

                        fillBar.GetComponent<Image>().color = fillColor;
                    }
                } catch { }
            }
        }

        public static DPSUI_GUI _UI;
        internal static InGameUI gameUI;
        public static UIMode _UIMode;

        public static bool showUI;
        private static bool _userShowPartyUI;
        private static bool _userShowLocalUI;
        private static bool _showPartyUI;
        private static bool _showLocalUI = true;

        private static float partyAnimPos;

        public static Font UI_font;

        public static Vector2 DPS_Pos = new Vector2(0.61f, 0.14f);
        public static Vector2 PartyDPS_MinPos = new Vector2(0.85f, 0.35f);
        public static Vector2 PartyDPS_MaxPos = new Vector2(1f, 0.65f);
        public static Vector2 PartyDPS_TotalPos = new Vector2(0.05f, 0.95f);
        public static Vector2 PartyDPS_DPSPos = new Vector2(0.95f, 0.95f);

        public static int MaxShownPartyMembers = 5;
        public static float topTextSpace = 0.13f;
        public static float edgePadding = 0.03f;
        public static float memberSpacing = 0.01f;
        public static int topTextSize = 20;

        internal static GameObject partyDpsContainer;
        internal static GameObject localDpsContainer;

        internal static RectTransform partyDpsTransform;
        internal static RectTransform localDpsTransform;

        internal static AnimationCurve partyAnimCurve;

        internal static GameObject partyDpsLabelRoot;

        internal Canvas rootCanvas;

        internal Text localDpsText;
        internal Text partyTotalText;
        internal Text partyDpsText;
        internal Text partyDpsLabelText;

        internal static Texture2D[] classImages;
        internal PartyMemberBar[] memberBars;

        internal static bool createdUI;


        public static bool userShowPartyUI {
            get { return _userShowPartyUI; }
            set {
                _userShowPartyUI = value;
                _UI?.Update();
            }
        }

        public static bool userShowLocalUI {
            get { return _userShowLocalUI; }
            set {
                _userShowLocalUI = value;
                _UI?.UpdateVisibility();
            }
        }

        public static bool showPartyUI {
            get { return _showPartyUI; }
            set {
                if (!_showPartyUI && value)
                    partyAnimPos = 0f;

                _showPartyUI = value;
                _UI?.Update();
            }
        }

        public static bool showLocalUI {
            get { return _showLocalUI; }
            set {
                _showLocalUI = value;
                if ((bool)_UI) {
                    _UI.UpdateVisibility();
                }
            }
        }

        public static void setParent(GameObject child, GameObject parent) {
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
        }

        public static void resetPartyAnim() {
            partyDpsContainer.SetActive(false);
            partyAnimPos = 0f;
        }

        public static RectTransform setupRectTransform(GameObject obj, Vector2 anchor, GameObject parent = null, bool ignoreParentRect = false) {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.pivot = anchor;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;

            if (parent) {
                setParent(obj, parent);
                if (!ignoreParentRect) {
                    RectTransform parentRect = parent.GetComponent<RectTransform>();
                    if (parentRect)
                        rect.sizeDelta = parentRect.sizeDelta;
                }
            }

            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        public static Text setupText(GameObject textObj, int fontSize = 10) {
            Text component = textObj.GetComponent<Text>();
            if (component == null)
                return null;

            RectTransform component2 = textObj.transform.parent.GetComponent<RectTransform>();
            if (component2 && component2.sizeDelta != Vector2.zero) {
                textObj.GetComponent<RectTransform>().sizeDelta = component2.sizeDelta;
            }
            component.gameObject.layer = LayerMask.NameToLayer("UI");
            component.font = UI_font;
            component.font.material.mainTexture.filterMode = (FilterMode)0;
            component.horizontalOverflow = (HorizontalWrapMode)1;
            component.alignment = (TextAnchor)8;
            component.fontSize = fontSize;
            component.transform.localPosition = Vector3.zero;
            RectTransform component3 = component.GetComponent<RectTransform>();
            if (component3) {
                component3.anchoredPosition = Vector2.zero;
            }
            return component;
        }

        public static GameObject AddBackground(GameObject uiObjt, Texture2D image = null, Sprite sprite = null) {
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            setParent(background, uiObjt);

            RectTransform rectTrans = background.GetComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.offsetMin = Vector2.zero;
            rectTrans.offsetMax = Vector2.zero;
            background.gameObject.layer = LayerMask.NameToLayer("UI");

            Image imgComp = background.GetComponent<Image>();
            if (image) {
                imgComp.sprite = Sprite.Create(image, new Rect(0f, 0f, image.width, image.height), new Vector2(0.5f, 0.5f));
                imgComp.type = Image.Type.Sliced;
            } else if (sprite) {
                imgComp.sprite = sprite;
                imgComp.type = Image.Type.Sliced;
            } else {
                imgComp.color = Color.black;
            }
            return background;
        }

        internal DPSUI_GUI() {
            if (classImages == null || classImages.Length == 0) {
                Texture2D[] images = Resources.LoadAll<Texture2D>("_graphic/_ui/_classicons/");

                Texture2D nullImg = Resources.Load<Texture2D>("_graphic/_ui/_ico_caution_lv");
                nullImg.name = "Null";

                classImages = new Texture2D[images.Length + 1];
                classImages[0] = nullImg;
                Array.Copy(images, 0, classImages, 1, images.Length);

                Plugin.logger.LogDebug($"Loaded {classImages.Length} class icons");
                foreach (Texture2D img in classImages) {
                    Plugin.logger.LogDebug("Loaded class " + img.name);
                }
            }

            if (InGameUI._current) {
                UI_font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                GameObject rootCanvas = new GameObject("DPSUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                setupMainCanvas(rootCanvas);
                Plugin.logger.LogInfo($"root {rootCanvas}");
                GameObject localDPSContainer = new GameObject("localDPS", typeof(RectTransform));
                RectTransform obj = setupRectTransform(localDPSContainer, DPS_Pos, rootCanvas);
                obj.sizeDelta = new Vector2(150f, 50f);
                obj.anchoredPosition = Vector2.zero;
                localDpsTransform = obj;

                GameObject localDPSText = new GameObject("DpsText", typeof(Text));
                setupRectTransform(localDPSText, Vector2.right, localDPSContainer);
                localDpsText = setupText(localDPSText, 25);
                localDpsText.text = "0 DPS";

                Texture2D backgroundImage = null;
                Sprite backgroundSprite = null;

                try {
                    UnityEngine.Object bgValue = Resources.Load<Sprite>(DPSUI_Config.backgroundImage.Value);
                    if (bgValue != null) {
                        backgroundSprite = bgValue as Sprite;
                    } else
                        backgroundImage = Resources.Load(DPSUI_Config.backgroundImage.Value) as Texture2D;
                } catch {
                    backgroundSprite = Resources.Load("_graphic/_ui/bk_06") as Sprite;
                }

                if (backgroundImage != null)
                    backgroundImage.filterMode = FilterMode.Point;

                GameObject partyContainer = new GameObject("partyDPS", typeof(RectTransform));
                RectTransform partyTransform = setupRectTransform(partyContainer, Vector2.zero, rootCanvas, true);
                partyTransform.anchorMin = PartyDPS_MinPos;
                partyTransform.anchorMax = PartyDPS_MaxPos;
                partyTransform.sizeDelta = Vector2.zero;
                partyDpsTransform = partyTransform;
                if (backgroundSprite != null)
                    AddBackground(partyContainer, sprite: backgroundSprite);
                else if (backgroundImage != null)
                    AddBackground(partyContainer, image: backgroundImage);
                partyContainer.GetComponentInChildren<Image>().color = new Color(0.9f, 0.9f, 0.9f);

                partyAnimCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 1f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );

                GameObject partyLabelContainer = new GameObject("DPS Label", typeof(RectTransform));
                RectTransform obj6 = setupRectTransform(partyLabelContainer, new Vector2(0f, 1f), rootCanvas, true);
                obj6.anchorMin = new Vector2(PartyDPS_MinPos.x, PartyDPS_MaxPos.y);
                obj6.anchorMax = new Vector2(PartyDPS_MaxPos.x, PartyDPS_MaxPos.y + 0.04f);
                obj6.sizeDelta = Vector2.zero;

                backgroundImage = Resources.Load("_graphic/_ui/bk_04") as Texture2D;
                AddBackground(partyLabelContainer, backgroundImage).GetComponent<Image>().color = new Color32(133, 94, 83, 255);
                partyDpsLabelRoot = partyLabelContainer;

                GameObject partyLabelText = new GameObject("PartyLabelText", typeof(Text));
                setupRectTransform(partyLabelText, new Vector2(0.5f, 0.5f), partyLabelContainer, true);
                partyDpsLabelText = setupText(partyLabelText, 20);
                partyDpsLabelText.alignment = TextAnchor.MiddleCenter;

                GameObject val9 = new GameObject("PartyTotalText", typeof(Text));
                RectTransform obj8 = setupRectTransform(val9, PartyDPS_TotalPos, partyContainer, true);
                partyTotalText = setupText(val9, topTextSize);
                val9.AddComponent<Shadow>();
                partyTotalText.alignment = TextAnchor.UpperLeft;
                obj8.anchoredPosition = Vector2.zero;
                partyTotalText.text = "Total: 8000";

                GameObject partyDPSText = new GameObject("PartyDPSText", typeof(Text));
                RectTransform partyDPSTextTrans = setupRectTransform(partyDPSText, PartyDPS_DPSPos, partyContainer, true);
                partyDpsText = setupText(partyDPSText, topTextSize);
                partyDPSText.AddComponent<Shadow>();
                partyDpsText.alignment = TextAnchor.UpperRight;
                partyDPSTextTrans.anchoredPosition = Vector2.zero;
                partyDpsText.text = "8000 DPS";

                float memberBarHeight = (1f - topTextSpace - (MaxShownPartyMembers - 1) * memberSpacing - edgePadding) / (float)MaxShownPartyMembers;
                memberBars = new PartyMemberBar[MaxShownPartyMembers];

                Vector2 anchorMin, anchorMax;
                for (int i = MaxShownPartyMembers - 1; i >= 0; i--) {
                    float yMin = edgePadding + memberSpacing * i + memberBarHeight * i;
                    float yMax = yMin + memberBarHeight;

                    anchorMin = new Vector2(edgePadding, yMin);
                    anchorMax = new Vector2(1 - edgePadding, yMax);

                    GameObject memberBarContainer = new GameObject($"member{i}", typeof(RectTransform));
                    RectTransform obj10 = setupRectTransform(memberBarContainer, anchorMin, partyContainer);
                    obj10.anchorMin = anchorMin;
                    obj10.anchorMax = anchorMax;
                    obj10.pivot = Vector2.zero;
                    obj10.anchoredPosition = Vector2.zero;

                    memberBars[MaxShownPartyMembers - 1 - i] = new PartyMemberBar(memberBarContainer);
                }

                partyDpsContainer = partyContainer;
                localDpsContainer = localDPSContainer;
                createdUI = true;
                gameUI = InGameUI._current;

                partyDpsContainer.SetActive(false);
                localDpsContainer.SetActive(false);

                UpdateVisibility();
                Plugin.logger.LogInfo("Created dps ui!");
            }

            void setupMainCanvas(GameObject canvasObj) {
                setupRectTransform(canvasObj, Vector2.zero, ((Component)InGameUI._current).gameObject, ignoreParentRect: true);
                Canvas component = canvasObj.GetComponent<Canvas>();
                component.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                rootCanvas = component;
            }
        }

        internal void UpdateVisibility() {
            if (!Plugin._AmHeadless && createdUI) {
                localDpsContainer.SetActive(showLocalUI && _userShowLocalUI && showUI);
                partyDpsLabelRoot.SetActive(false);
            }
        }

        public void UpdateDPS(float dps) {
            if (createdUI) {
                string text = dps.ToString("n1");
                if (text.EndsWith(".0"))
                    text = text.Substring(0, text.IndexOf('.'));

                string text2 = text + " DPS";
                localDpsText.text = text2;
            }
        }

        public void UpdatePartyDamageValues(DPSPacket packet) {
            if (!createdUI || packet == null)
                return;

            List<DPSValues> values = packet.partyDamageValues;
            long startTime = packet.dungeonStartTime;
            long endTime = packet.bossFightEndTime;

            if (_UIMode == UIMode.Auto) {
                var bossDamageValues = packet.bossDamageValues;
                if (bossDamageValues != null && bossDamageValues.Count > 0) {
                    values = packet.bossDamageValues;
                    startTime = packet.bossFightStartTime;
                    endTime = packet.bossFightEndTime;
                }
            } else if (_UIMode == UIMode.Boss) {
                values = packet.bossDamageValues;
                startTime = packet.bossFightStartTime;
                endTime = packet.bossFightEndTime;
            }

            if (values?.Count == 0) {
                showPartyUI = false;
                return;
            }

            values.Sort((b, a) => a.totalDamage.CompareTo(b.totalDamage));

            bool foundPlayer = false;
            int totalDamage = 0;
            string playerSteamID = Player._mainPlayer._steamID;
            foreach (DPSValues v in values) {
                totalDamage += v.totalDamage;
                if (!foundPlayer && v.steamID == playerSteamID)
                    foundPlayer = true;
            }

            if (!foundPlayer)
                return;

            if (endTime == 0)
                endTime = DateTime.UtcNow.Ticks / 10000;

            float totalTime = (endTime - startTime) / 1000f;
            float dps = totalDamage / totalTime;

            partyTotalText.text = "Total: " + totalDamage;
            string text = dps.ToString("n1");
            if (text.EndsWith(".0"))
                text = text.Substring(0, text.Length - 2);

            partyDpsText.text = text + " DPS";

            PartyMemberBar[] array = memberBars;
            for (int i = 0; i < array.Length; i++)
                array[i].self.SetActive(false);

            showPartyUI = true;
            bool addedLocalPlayer = false;
            for (int i = 0; i < values.Count; i++) {
                DPSValues v = values[i];
                float fillPercent = (float)v.totalDamage / Mathf.Max(values[0].totalDamage, 1);
                Plugin.logger.LogDebug($"{v.nickname} dmg: {v.totalDamage} UpdatePartyValues  percent fill: {fillPercent}");
                if (v.steamID == playerSteamID)
                    addedLocalPlayer = true;

                if (i < 4 || addedLocalPlayer || !(v.steamID != playerSteamID)) {
                    memberBars[Math.Min(4, i)].UpdateInfo(v, fillPercent);
                    if (i >= 4 && addedLocalPlayer)
                        break;
                }
            }
        }

        internal void clearDamageValues() {
            partyTotalText.text = "Total: 0";
            partyDpsText.text = "0 DPS";

            foreach (PartyMemberBar obj in memberBars) {
                obj.UpdateInfo(new DPSValues(), 0f);
                obj.self.SetActive(false);
            }
        }

        public void Update() {
            if (!createdUI || Plugin._AmHeadless)
                return;

            if ((gameUI._displayUI && !Player._mainPlayer._inUI) != showUI) {
                showUI = gameUI._displayUI && !Player._mainPlayer._inUI;
                localDpsContainer.SetActive(showLocalUI && _userShowLocalUI && showUI);
            }

            if (showUI && _showPartyUI && _userShowPartyUI && partyAnimPos < 1f) {
                if (!partyDpsContainer.activeSelf)
                    partyDpsContainer.SetActive(true);

                if (DPSUI_Config.transitionTime.Value == 0) {
                    partyDpsTransform.anchoredPosition = Vector2.zero;
                    partyAnimPos = 1;
                } else {
                    partyAnimPos += Time.deltaTime / DPSUI_Config.transitionTime.Value;
                    if (partyAnimPos > 1f)
                        partyAnimPos = 1f;

                    Vector2 pos = Vector2.zero;
                    pos.x = partyDpsTransform.rect.width * (1f - partyAnimCurve.Evaluate(partyAnimPos));
                    partyDpsTransform.anchoredPosition = pos;
                }
            }

            if ((!showUI || !_showPartyUI || !_userShowPartyUI) && partyAnimPos > 0f) {

                if (DPSUI_Config.transitionTime.Value == 0) {
                    partyDpsContainer.SetActive(false);
                    partyAnimPos = 0;
                } else {
                    partyAnimPos -= Time.deltaTime / DPSUI_Config.transitionTime.Value;

                    if (partyAnimPos < 0f) {
                        partyAnimPos = 0f;
                        partyDpsContainer.SetActive(false);
                    }

                    Vector2 pos = Vector2.zero;
                    pos.x = partyDpsTransform.rect.width * (1f - partyAnimCurve.Evaluate(partyAnimPos));

                    partyDpsTransform.anchoredPosition = pos;
                }
            }
        }
    }
}
