using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Jingzhou.Battle.Presentation
{
    public sealed class BattleHud
    {
        public const float PanelWidth = 400f;
        public const float TopHeight = 64f;

        Text status;
        Text objective;
        Text detail;
        Text hint;
        Text log;
        Button waitButton;
        Button endButton;
        GameObject resultRoot;
        Text resultHeadline;
        Text resultDetail;
        Font font;
        public Canvas Canvas;

        public void Build(Font font, UnityAction onWait, UnityAction onEnd, UnityAction onRestart)
        {
            this.font = font;
            GameObject canvasGo = new GameObject("Battle HUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            Canvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject events = new GameObject("EventSystem");
            events.AddComponent<UnityEngine.EventSystems.EventSystem>();
            events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Image top = Panel(canvasGo.transform, "Top", BattlePalette.Panel);
            RectTransform topRect = top.rectTransform;
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0f, TopHeight);
            topRect.anchoredPosition = Vector2.zero;
            status = AddText(top.transform, 26, TextAnchor.MiddleLeft, BattlePalette.Paper);
            status.rectTransform.offsetMin = new Vector2(24f, 0f);
            status.rectTransform.offsetMax = new Vector2(-24f, 0f);

            Image side = Panel(canvasGo.transform, "Side", BattlePalette.Panel);
            RectTransform sideRect = side.rectTransform;
            sideRect.anchorMin = new Vector2(1f, 0f);
            sideRect.anchorMax = new Vector2(1f, 1f);
            sideRect.pivot = new Vector2(1f, 1f);
            sideRect.offsetMin = new Vector2(-PanelWidth, 0f);
            sideRect.offsetMax = new Vector2(0f, -TopHeight);

            GameObject column = Child(side.transform, "Column");
            Stretch(column.GetComponent<RectTransform>(), 16f, 14f, 16f, 14f);
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            objective = BlockText(column.transform, 78f, 16, TextAnchor.UpperLeft, BattlePalette.Gold);
            BlockText(column.transform, 108f, 15, TextAnchor.UpperLeft, BattlePalette.Muted).text =
                "点选我军武将。绿格可移动，再点敌军攻击。弓兵射程 2，远射不会被近战反击，但会被中间的武将挡住。骑克弓，弓克步，步克骑。渡口是过河的路。";
            detail = BlockText(column.transform, 132f, 18, TextAnchor.UpperLeft, BattlePalette.Paper);
            hint = BlockText(column.transform, 48f, 16, TextAnchor.UpperLeft, BattlePalette.Good);
            log = BlockText(column.transform, 220f, 15, TextAnchor.UpperLeft, BattlePalette.Muted);

            GameObject buttons = Child(column.transform, "Buttons");
            LayoutElement buttonRow = buttons.AddComponent<LayoutElement>();
            buttonRow.preferredHeight = 44f;
            buttonRow.minHeight = 44f;
            HorizontalLayoutGroup row = buttons.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            row.childControlHeight = true;
            row.childControlWidth = true;
            waitButton = MakeButton(buttons.transform, "待命", onWait, new Color(0.24f, 0.38f, 0.30f, 1f));
            endButton = MakeButton(buttons.transform, "结束回合", onEnd, new Color(0.55f, 0.24f, 0.20f, 1f));

            resultRoot = Child(canvasGo.transform, "Result");
            Image dim = resultRoot.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.03f, 0.03f, 0.78f);
            Stretch(resultRoot.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            GameObject card = Child(resultRoot.transform, "Card");
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.14f, 0.11f, 0.09f, 1f);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 280f);
            resultHeadline = AddText(card.transform, 54, TextAnchor.MiddleCenter, BattlePalette.Gold);
            resultHeadline.rectTransform.anchorMin = new Vector2(0f, 0.55f);
            resultHeadline.rectTransform.anchorMax = new Vector2(1f, 1f);
            resultHeadline.rectTransform.offsetMin = new Vector2(24f, 0f);
            resultHeadline.rectTransform.offsetMax = new Vector2(-24f, -16f);
            resultDetail = AddText(card.transform, 22, TextAnchor.MiddleCenter, BattlePalette.Paper);
            resultDetail.rectTransform.anchorMin = new Vector2(0f, 0.28f);
            resultDetail.rectTransform.anchorMax = new Vector2(1f, 0.58f);
            resultDetail.rectTransform.offsetMin = new Vector2(28f, 0f);
            resultDetail.rectTransform.offsetMax = new Vector2(-28f, 0f);
            Button restart = MakeButton(card.transform, "再战", onRestart, new Color(0.62f, 0.46f, 0.18f, 1f));
            RectTransform restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0f);
            restartRect.anchorMax = new Vector2(0.5f, 0f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.anchoredPosition = new Vector2(0f, 28f);
            restartRect.sizeDelta = new Vector2(180f, 46f);
            resultRoot.SetActive(false);
        }

        public void ShowBattle(BattleSession session)
        {
            objective.text = "胜利：击败" + session.EnemyCommander.Name + "，或坚守到第" + session.MaxTurns + "回合。\n失败：关羽阵亡，或敌军进入荆州。";
            HideResult();
        }

        public void SetStatus(string text, Color color)
        {
            status.text = text;
            status.color = color;
        }

        public void SetDetail(string text)
        {
            detail.text = text;
        }

        public void SetHint(string text)
        {
            hint.text = text;
        }

        public void SetLog(string text)
        {
            log.text = text;
        }

        public void SetButtons(bool canWait, bool canEnd)
        {
            waitButton.interactable = canWait;
            endButton.interactable = canEnd;
        }

        public void ShowResult(BattleSession session)
        {
            bool victory = session.Outcome == BattleOutcome.Victory;
            resultHeadline.text = session.OutcomeHeadline;
            resultHeadline.color = victory ? BattlePalette.Good : BattlePalette.Bad;
            resultDetail.text = session.OutcomeDetail;
            resultRoot.SetActive(true);
            resultRoot.transform.SetAsLastSibling();
        }

        public void HideResult()
        {
            if (resultRoot != null)
                resultRoot.SetActive(false);
        }

        public void ShowFatal(string message)
        {
            if (resultRoot == null)
                return;
            resultHeadline.text = "无法开战";
            resultHeadline.color = BattlePalette.Bad;
            resultDetail.text = message;
            resultRoot.SetActive(true);
        }

        static Image Panel(Transform parent, string name, Color color)
        {
            GameObject go = Child(parent, name);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        Text BlockText(Transform parent, float height, int size, TextAnchor anchor, Color color)
        {
            GameObject go = Child(parent, "block");
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return AddText(go.transform, size, anchor, color);
        }

        Text AddText(Transform parent, int size, TextAnchor anchor, Color color)
        {
            GameObject go = Child(parent, "text");
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
            return text;
        }

        Button MakeButton(Transform parent, string label, UnityAction onClick, Color color)
        {
            GameObject go = Child(parent, label);
            Image image = go.AddComponent<Image>();
            image.color = color;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 42f;
            element.minHeight = 42f;
            element.preferredWidth = 150f;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            button.colors = colors;
            GameObject labelGo = Child(go.transform, "label");
            Text labelText = labelGo.AddComponent<Text>();
            labelText.font = font;
            labelText.text = label;
            labelText.fontSize = 20;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(labelText.rectTransform, 0f, 0f, 0f, 0f);
            return button;
        }

        static GameObject Child(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
