using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jingzhou.Battle.Presentation
{
    public sealed class BattleBoardView
    {
        sealed class Token
        {
            public string Id;
            public GameObject Root;
            public SpriteRenderer Body;
            public SpriteRenderer Ring;
            public Text Name;
            public Text Troops;
        }

        GameObject root;
        GameObject highlightRoot;
        Font font;
        Sprite tileSprite;
        Sprite circleSprite;
        Sprite ringSprite;
        readonly Dictionary<string, Token> tokens = new Dictionary<string, Token>();
        readonly List<GameObject> highlights = new List<GameObject>();
        readonly List<Floater> floaters = new List<Floater>();

        sealed class Floater
        {
            public GameObject Root;
            public Text Label;
            public float Age;
        }

        public void Rebuild(BattleSession session, Font font)
        {
            this.font = font;
            if (root != null)
                Object.Destroy(root);
            tokens.Clear();
            highlights.Clear();
            EnsureSprites();

            root = new GameObject("Board");
            highlightRoot = new GameObject("Highlights");
            highlightRoot.transform.SetParent(root.transform, false);

            for (int y = 0; y < session.Map.Height; y++)
            {
                for (int x = 0; x < session.Map.Width; x++)
                {
                    GameObject tile = new GameObject("tile-" + x + "-" + y);
                    tile.transform.SetParent(root.transform, false);
                    tile.transform.position = new Vector3(x, y, 0.1f);
                    SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = tileSprite;
                    renderer.color = ColorFor(session.Map.Get(x, y));
                    renderer.sortingOrder = 0;
                }
            }

            CreateCityMark(session);
            for (int i = 0; i < session.Units.Count; i++)
                CreateToken(session.Units[i]);
        }

        public void RefreshUnits(BattleSession session, UnitState selected)
        {
            for (int i = 0; i < session.Units.Count; i++)
            {
                UnitState unit = session.Units[i];
                Token token;
                if (!tokens.TryGetValue(unit.Id, out token))
                    continue;
                token.Root.transform.position = new Vector3(unit.X, unit.Y, 0f);
                token.Troops.text = unit.Troops.ToString();
                Color body = unit.Faction == Faction.Player ? BattlePalette.Player : BattlePalette.Enemy;
                if (unit.Act == ActState.Done)
                    body *= 0.62f;
                body.a = 1f;
                token.Body.color = body;
                bool leader = unit.IsGuanYu || unit.IsCommander;
                token.Ring.enabled = leader || unit == selected;
                token.Ring.color = BattlePalette.Gold;
                float scale = unit == selected ? 1.16f : (leader ? 1.02f : 0.92f);
                token.Ring.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void ShowHighlights(List<GridPos> moves, List<UnitState> targets, UnitState selected)
        {
            for (int i = 0; i < highlights.Count; i++)
            {
                if (highlights[i] != null)
                    Object.Destroy(highlights[i]);
            }
            highlights.Clear();
            if (selected == null)
                return;

            if (selected.Act == ActState.Ready)
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    GridPos pos = moves[i];
                    if (pos.X == selected.X && pos.Y == selected.Y)
                        continue;
                    highlights.Add(Marker(pos.X, pos.Y, BattlePalette.Move, 1));
                }
            }

            for (int i = 0; i < targets.Count; i++)
                highlights.Add(Marker(targets[i].X, targets[i].Y, BattlePalette.Attack, 2));

            highlights.Add(Marker(selected.X, selected.Y, BattlePalette.Select, 3));
        }

        public IEnumerator PlayMove(List<GridPos> path, string unitId)
        {
            Token token;
            if (path == null || path.Count == 0 || !tokens.TryGetValue(unitId, out token))
                yield break;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 from = token.Root.transform.position;
                Vector3 to = new Vector3(path[i].X, path[i].Y, 0f);
                float duration = 0.12f;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    token.Root.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
                    yield return null;
                }
                token.Root.transform.position = to;
            }
        }

        public IEnumerator PlayAttack(AttackReport report)
        {
            if (report == null)
                yield break;
            Token defender;
            Token attacker;
            tokens.TryGetValue(report.Defender.Id, out defender);
            tokens.TryGetValue(report.Attacker.Id, out attacker);
            if (defender != null)
            {
                defender.Troops.text = report.Defender.Troops.ToString();
                yield return Flash(defender, new Color(1f, 0.85f, 0.8f));
                SpawnFloat(defender.Root.transform.position + new Vector3(0.15f, 0.35f, 0f), "-" + report.Damage, BattlePalette.Damage);
            }
            if (report.DidCounter && attacker != null)
            {
                attacker.Troops.text = report.Attacker.Troops.ToString();
                yield return Flash(attacker, new Color(1f, 0.92f, 0.7f));
                SpawnFloat(attacker.Root.transform.position + new Vector3(-0.15f, 0.35f, 0f), "-" + report.CounterDamage, BattlePalette.Counter);
            }
            if (report.DefenderDefeated && defender != null)
                yield return FadeOut(defender);
            if (report.AttackerDefeated && attacker != null)
                yield return FadeOut(attacker);
            yield return new WaitForSeconds(0.18f);
        }

        GameObject Marker(int x, int y, Color color, int order)
        {
            GameObject go = new GameObject("mark");
            go.transform.SetParent(highlightRoot.transform, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = tileSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return go;
        }

        void CreateCityMark(BattleSession session)
        {
            GameObject mark = new GameObject("jingzhou");
            mark.transform.SetParent(root.transform, false);
            mark.transform.position = new Vector3(session.Map.CityX, session.Map.CityY - 0.34f, 0f);
            Canvas canvas = mark.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 4;
            RectTransform rect = mark.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 40f);
            rect.localScale = new Vector3(0.0065f, 0.0065f, 0.0065f);
            GameObject textGo = new GameObject("label", typeof(RectTransform));
            textGo.transform.SetParent(mark.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = font;
            text.text = "荆州";
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.35f, 0.16f, 0.08f, 1f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            if (font != null)
                font.RequestCharactersInTexture("荆州", 28, FontStyle.Normal);
        }

        void CreateToken(UnitState unit)
        {
            Token token = new Token();
            token.Id = unit.Id;
            token.Root = new GameObject("unit-" + unit.Id);
            token.Root.transform.SetParent(root.transform, false);
            token.Root.transform.position = new Vector3(unit.X, unit.Y, 0f);

            GameObject ring = new GameObject("ring");
            ring.transform.SetParent(token.Root.transform, false);
            token.Ring = ring.AddComponent<SpriteRenderer>();
            token.Ring.sprite = ringSprite;
            token.Ring.color = BattlePalette.Gold;
            token.Ring.sortingOrder = 5;
            token.Ring.enabled = unit.IsGuanYu || unit.IsCommander;

            GameObject body = new GameObject("body");
            body.transform.SetParent(token.Root.transform, false);
            body.transform.localScale = new Vector3(0.74f, 0.74f, 1f);
            token.Body = body.AddComponent<SpriteRenderer>();
            token.Body.sprite = circleSprite;
            token.Body.sortingOrder = 6;
            token.Body.color = unit.Faction == Faction.Player ? BattlePalette.Player : BattlePalette.Enemy;

            Canvas canvas = CreateWorldCanvas(token.Root.transform, 30);
            token.Name = CreateWorldText(canvas.transform, unit.Name, 32, new Vector2(0f, 10f), BattlePalette.Ink);
            token.Troops = CreateWorldText(canvas.transform, unit.Troops.ToString(), 22, new Vector2(0f, -24f), BattlePalette.Paper);
            if (font != null)
                font.RequestCharactersInTexture(unit.Name + unit.Troops, 32, FontStyle.Normal);
            tokens[unit.Id] = token;
        }

        Canvas CreateWorldCanvas(Transform parent, int order)
        {
            GameObject go = new GameObject("canvas");
            go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 90f);
            rect.localScale = new Vector3(0.0072f, 0.0072f, 0.0072f);
            return canvas;
        }

        Text CreateWorldText(Transform parent, string value, int size, Vector2 anchored, Color color)
        {
            GameObject go = new GameObject("text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.sizeDelta = new Vector2(140f, 40f);
            rect.anchoredPosition = anchored;
            return text;
        }

        IEnumerator Flash(Token token, Color color)
        {
            Color original = token.Body.color;
            token.Body.color = color;
            yield return new WaitForSeconds(0.08f);
            if (token.Body != null)
                token.Body.color = original;
        }

        IEnumerator FadeOut(Token token)
        {
            float duration = 0.28f;
            float t = 0f;
            Vector3 scale = token.Root.transform.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                token.Root.transform.localScale = Vector3.Lerp(scale, scale * 0.2f, k);
                SetAlpha(token, 1f - k);
                yield return null;
            }
            tokens.Remove(token.Id);
            Object.Destroy(token.Root);
        }

        void SpawnFloat(Vector3 position, string value, Color color)
        {
            GameObject go = new GameObject("float");
            go.transform.SetParent(root.transform, false);
            go.transform.position = position;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 50f);
            rect.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            Text text = CreateWorldText(go.transform, value, 36, Vector2.zero, color);
            if (font != null)
                font.RequestCharactersInTexture(value, 36, FontStyle.Normal);
            Floater floater = new Floater();
            floater.Root = go;
            floater.Label = text;
            floater.Age = 0f;
            floaters.Add(floater);
        }

        public void Tick(float dt)
        {
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                Floater floater = floaters[i];
                if (floater.Root == null)
                {
                    floaters.RemoveAt(i);
                    continue;
                }
                floater.Age += dt;
                floater.Root.transform.position += new Vector3(0f, dt * 0.55f, 0f);
                if (floater.Label != null)
                {
                    Color color = floater.Label.color;
                    color.a = Mathf.Clamp01(1f - floater.Age / 0.8f);
                    floater.Label.color = color;
                }
                if (floater.Age >= 0.8f)
                {
                    Object.Destroy(floater.Root);
                    floaters.RemoveAt(i);
                }
            }
        }

        static void SetAlpha(Token token, float alpha)
        {
            Color body = token.Body.color;
            body.a = alpha;
            token.Body.color = body;
            Color ring = token.Ring.color;
            ring.a = alpha;
            token.Ring.color = ring;
            Color name = token.Name.color;
            name.a = alpha;
            token.Name.color = name;
            Color troops = token.Troops.color;
            troops.a = alpha;
            token.Troops.color = troops;
        }

        void EnsureSprites()
        {
            if (tileSprite != null)
                return;
            tileSprite = Sprite.Create(SquareTexture(), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
            circleSprite = Sprite.Create(CircleTexture(false), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
            ringSprite = Sprite.Create(CircleTexture(true), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        static Texture2D SquareTexture()
        {
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color fill = Color.white;
            Color edge = new Color(0f, 0f, 0f, 0.38f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool border = x < 2 || y < 2 || x >= 62 || y >= 62;
                    tex.SetPixel(x, y, border ? edge : fill);
                }
            }
            tex.Apply();
            return tex;
        }

        static Texture2D CircleTexture(bool ring)
        {
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float radius = 30f;
            float inner = 22f;
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha;
                    if (ring)
                        alpha = Mathf.Clamp01(radius - distance + 0.6f) * Mathf.Clamp01(distance - inner + 0.6f);
                    else
                        alpha = Mathf.Clamp01(radius - distance + 0.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        static Color ColorFor(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Forest: return BattlePalette.Forest;
                case TerrainKind.Hill: return BattlePalette.Hill;
                case TerrainKind.River: return BattlePalette.River;
                case TerrainKind.Road: return BattlePalette.Road;
                case TerrainKind.City: return BattlePalette.City;
                default: return BattlePalette.Plain;
            }
        }
    }
}
