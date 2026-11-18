using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jingzhou.Battle.Presentation
{
    public sealed class BattlePresenter : MonoBehaviour
    {
        BattleSession session;
        string battleJson;
        Font font;
        readonly BattleBoardView board = new BattleBoardView();
        readonly BattleHud hud = new BattleHud();
        UnitState selected;
        bool busy;
        bool endArmed;
        string hintOverride = "";
        int framedWidth = -1;
        int framedHeight = -1;
        float framedScale = -1f;
        int hoverX = -999;
        int hoverY = -999;

        void Awake()
        {
            font = LoadFont();
            EnsureCamera();
            hud.Build(font, OnWait, OnEndTurn, OnRestart);
            TextAsset asset = Resources.Load<TextAsset>("Battle/jingzhou");
            if (asset == null)
            {
                hud.ShowFatal("找不到 Resources/Battle/jingzhou。");
                return;
            }
            battleJson = asset.text;
            try
            {
                session = BattleLoader.Load(battleJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                hud.ShowFatal(ex.Message);
                return;
            }
            WarmFont();
            board.Rebuild(session, font);
            hud.ShowBattle(session);
            FrameCamera(true);
            Refresh();
        }

        void Update()
        {
            if (session == null)
                return;
            FrameCamera(false);
            board.Tick(Time.deltaTime);
            if (!busy && HoverChanged())
                Refresh();
            if (busy || session.Outcome != BattleOutcome.Ongoing)
                return;
            if (WasCancel() && selected != null && selected.Act == ActState.Ready)
            {
                selected = null;
                endArmed = false;
                Refresh();
            }
            else if (WasClick())
            {
                OnMapClick();
            }
        }

        bool HoverChanged()
        {
            int x;
            int y;
            int nextX = -999;
            int nextY = -999;
            if (TryPickTile(out x, out y))
            {
                nextX = x;
                nextY = y;
            }
            if (nextX == hoverX && nextY == hoverY)
                return false;
            hoverX = nextX;
            hoverY = nextY;
            return true;
        }

        void OnMapClick()
        {
            if (session.Phase != BattlePhase.Player)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            endArmed = false;
            int x;
            int y;
            if (!TryPickTile(out x, out y))
            {
                if (selected != null && selected.Act == ActState.Ready)
                    selected = null;
                Refresh();
                return;
            }

            UnitState occupant = session.UnitAt(x, y);
            if (selected != null && occupant != null && occupant.Faction == Faction.Enemy)
            {
                int distance = BattleRules.Distance(selected, occupant);
                if (session.CanAct(selected) && distance >= 1 && distance <= selected.AttackRange)
                {
                    StartCoroutine(AttackRoutine(selected, occupant));
                    return;
                }
                hintOverride = "不在射程内。";
                Refresh();
                return;
            }

            if (selected != null && selected.Act == ActState.Ready && (occupant == null || occupant == selected))
            {
                MoveField field = session.GetMoveField(selected);
                if (field.Stops.Contains(new GridPos(x, y)) && (x != selected.X || y != selected.Y))
                {
                    StartCoroutine(MoveRoutine(selected, x, y));
                    return;
                }
            }

            if (occupant != null && occupant.Faction == Faction.Player && session.CanAct(occupant)
                && (selected == null || selected.Act == ActState.Ready))
            {
                selected = occupant;
                Refresh();
                return;
            }

            if (selected != null && selected.Act == ActState.Moved)
            {
                hintOverride = "请先攻击，或按待命。";
                Refresh();
                return;
            }

            if (selected != null && selected.Act == ActState.Ready)
                selected = null;
            Refresh();
        }

        void OnWait()
        {
            if (busy || session == null || !session.CanAct(selected))
                return;
            string reason;
            if (!session.PlayerWait(selected, out reason))
            {
                hintOverride = reason;
                Refresh();
                return;
            }
            selected = null;
            endArmed = false;
            Refresh();
        }

        void OnEndTurn()
        {
            if (busy || session == null || session.Outcome != BattleOutcome.Ongoing || session.Phase != BattlePhase.Player)
                return;
            if (HasUnfinishedUnits() && !endArmed)
            {
                endArmed = true;
                hintOverride = "仍有武将未行动。再按一次结束回合。";
                Refresh();
                return;
            }
            endArmed = false;
            StartCoroutine(EnemyRoutine());
        }

        void OnRestart()
        {
            if (string.IsNullOrEmpty(battleJson))
                return;
            StopAllCoroutines();
            busy = false;
            endArmed = false;
            selected = null;
            hintOverride = "";
            try
            {
                session = BattleLoader.Load(battleJson);
            }
            catch (System.Exception ex)
            {
                hud.ShowFatal(ex.Message);
                return;
            }
            board.Rebuild(session, font);
            hud.ShowBattle(session);
            hud.HideResult();
            FrameCamera(true);
            Refresh();
        }

        IEnumerator MoveRoutine(UnitState unit, int x, int y)
        {
            busy = true;
            MoveResult result = session.PlayerMove(unit, x, y);
            if (!result.Ok)
            {
                hintOverride = result.Reason;
                busy = false;
                Refresh();
                yield break;
            }
            yield return board.PlayMove(result.Path, unit.Id);
            busy = false;
            Refresh();
        }

        IEnumerator AttackRoutine(UnitState attacker, UnitState defender)
        {
            busy = true;
            AttackResult result = session.PlayerAttack(attacker, defender);
            if (!result.Ok)
            {
                hintOverride = result.Reason;
                busy = false;
                Refresh();
                yield break;
            }
            yield return board.PlayAttack(result.Report);
            if (selected != null && (!selected.IsAlive || selected.Act == ActState.Done))
                selected = null;
            busy = false;
            Refresh();
            if (session.Outcome != BattleOutcome.Ongoing)
                hud.ShowResult(session);
        }

        IEnumerator EnemyRoutine()
        {
            busy = true;
            selected = null;
            session.EndPlayerTurn();
            Refresh();
            yield return new WaitForSeconds(0.2f);
            int guard = 0;
            while (session.Outcome == BattleOutcome.Ongoing && session.Phase == BattlePhase.Enemy && guard++ < 32)
            {
                EnemyAction action = session.TakeEnemyAction();
                if (action == null || action.PhaseEnded)
                    break;
                if (action.Moved)
                    yield return board.PlayMove(action.Path, action.Unit.Id);
                if (action.Attack != null)
                    yield return board.PlayAttack(action.Attack);
                Refresh();
                if (session.Outcome != BattleOutcome.Ongoing)
                    break;
                yield return new WaitForSeconds(0.12f);
            }
            busy = false;
            Refresh();
            if (session.Outcome != BattleOutcome.Ongoing)
                hud.ShowResult(session);
        }

        void Refresh()
        {
            if (session == null)
                return;
            board.RefreshUnits(session, selected);
            List<GridPos> moves = new List<GridPos>();
            List<UnitState> targets = new List<UnitState>();
            if (selected != null && session.CanAct(selected) && session.Phase == BattlePhase.Player)
            {
                if (selected.Act == ActState.Ready)
                {
                    MoveField field = session.GetMoveField(selected);
                    foreach (GridPos pos in field.Stops)
                        moves.Add(pos);
                }
                targets = session.AttackTargetsFrom(selected, selected.X, selected.Y);
            }
            board.ShowHighlights(moves, targets, selected != null && session.CanAct(selected) ? selected : null);
            hud.SetStatus(StatusLine(), StatusColor());
            hud.SetDetail(DetailLine());
            hud.SetHint(string.IsNullOrEmpty(hintOverride) ? DefaultHint() : hintOverride);
            hud.SetLog(TailLog(12));
            bool playing = !busy && session.Outcome == BattleOutcome.Ongoing && session.Phase == BattlePhase.Player;
            hud.SetButtons(playing && session.CanAct(selected), playing);
            hintOverride = "";
        }

        string StatusLine()
        {
            string phase = "我军回合";
            if (session.Outcome == BattleOutcome.Victory)
                phase = "胜利";
            else if (session.Outcome == BattleOutcome.Defeat)
                phase = "失败";
            else if (session.Phase == BattlePhase.Enemy)
                phase = "敌军回合";
            return session.Title + "      第 " + session.Turn + " / " + session.MaxTurns + " 回合      " + phase;
        }

        Color StatusColor()
        {
            if (session.Outcome == BattleOutcome.Victory)
                return BattlePalette.Good;
            if (session.Outcome == BattleOutcome.Defeat)
                return BattlePalette.Bad;
            if (session.Phase == BattlePhase.Enemy)
                return BattlePalette.Bad;
            return BattlePalette.Paper;
        }

        string DetailLine()
        {
            int x;
            int y;
            UnitState hover = null;
            bool onMap = !busy && TryPickTile(out x, out y);
            if (onMap)
                hover = session.UnitAt(x, y);

            StringBuilder text = new StringBuilder();
            if (selected != null)
            {
                AppendUnit(text, selected);
                if (hover != null && hover.Faction == Faction.Enemy && session.CanAct(selected)
                    && session.CanStrike(selected, selected.X, selected.Y, hover))
                {
                    int distance = BattleRules.Distance(selected, hover);
                    if (distance >= 1)
                    {
                        CombatPreview preview = BattleRules.Preview(
                            selected,
                            hover,
                            session.Map.Defense(hover.X, hover.Y),
                            session.Map.Defense(selected.X, selected.Y),
                            distance);
                        text.Append("\n对").Append(hover.Name).Append("：伤害 ").Append(preview.Damage);
                        text.Append("（").Append(preview.MatchupLabel).Append("）");
                        if (preview.WillCounter)
                            text.Append("，反击 ").Append(preview.CounterDamage);
                        else
                            text.Append("，无反击");
                        if (preview.DefenderDies)
                            text.Append("，可击退");
                    }
                }
            }
            else if (hover != null)
            {
                AppendUnit(text, hover);
            }
            else if (onMap)
            {
                text.Append(session.Map.Describe(x, y));
            }
            else
            {
                text.Append("点选武将，查看兵力与兵种。");
            }
            return text.ToString();
        }

        void AppendUnit(StringBuilder text, UnitState unit)
        {
            text.Append(unit.Name);
            if (unit.IsGuanYu)
                text.Append(" · 主将");
            else if (unit.IsCommander)
                text.Append(" · 敌将");
            else if (unit.Faction == Faction.Player)
                text.Append(" · 我军");
            else
                text.Append(" · 敌军");
            text.Append("\n").Append(BattleRules.TroopName(unit.Type));
            text.Append(" · 兵力 ").Append(unit.Troops).Append("/").Append(unit.MaxTroops);
            text.Append("\n移动 ").Append(unit.Move).Append(" · 射程 ").Append(unit.AttackRange);
            text.Append("\n").Append(BattleRules.AdvantageText(unit.Type));
            text.Append("\n").Append(session.Map.Describe(unit.X, unit.Y));
        }

        string DefaultHint()
        {
            if (session.Outcome != BattleOutcome.Ongoing)
                return "";
            if (busy || session.Phase == BattlePhase.Enemy)
                return "敌军行动中…";
            if (endArmed)
                return "仍有武将未行动。再按一次结束回合。";
            if (selected == null)
                return "点选我军武将。绿格可移动。";
            if (selected.Act == ActState.Moved)
                return "请点红色目标攻击，或按「待命」。";
            return "移动后可以攻击，也可直接攻击射程内的敌军。Esc 取消选择。";
        }

        string TailLog(int count)
        {
            int start = session.Log.Count - count;
            if (start < 0)
                start = 0;
            StringBuilder text = new StringBuilder();
            for (int i = start; i < session.Log.Count; i++)
            {
                if (i > start)
                    text.Append('\n');
                text.Append(session.Log[i]);
            }
            return text.ToString();
        }

        bool HasUnfinishedUnits()
        {
            for (int i = 0; i < session.Units.Count; i++)
            {
                UnitState unit = session.Units[i];
                if (unit.Faction == Faction.Player && unit.IsAlive && unit.Act != ActState.Done)
                    return true;
            }
            return false;
        }

        bool TryPickTile(out int x, out int y)
        {
            x = 0;
            y = 0;
            Camera cam = Camera.main;
            if (cam == null || session == null)
                return false;
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(screen);
            x = Mathf.RoundToInt(world.x);
            y = Mathf.RoundToInt(world.y);
            return session.Map.InBounds(x, y);
        }

        void FrameCamera(bool force)
        {
            float scale = hud.Canvas != null ? hud.Canvas.scaleFactor : 1f;
            if (scale < 0.01f)
                scale = 1f;
            if (!force && Screen.width == framedWidth && Screen.height == framedHeight && Mathf.Abs(scale - framedScale) < 0.001f)
                return;
            Camera cam = Camera.main;
            if (cam == null || session == null)
                return;
            framedWidth = Screen.width;
            framedHeight = Screen.height;
            framedScale = scale;
            float right = Mathf.Clamp((BattleHud.PanelWidth * scale) / Screen.width, 0.2f, 0.42f);
            float top = Mathf.Clamp((BattleHud.TopHeight * scale) / Screen.height, 0.05f, 0.16f);
            cam.rect = new Rect(0f, 0f, 1f - right, 1f - top);
            cam.orthographic = true;
            float viewWidth = Mathf.Max(1f, Screen.width * cam.rect.width);
            float viewHeight = Mathf.Max(1f, Screen.height * cam.rect.height);
            float aspect = viewWidth / viewHeight;
            float halfH = session.Map.Height * 0.5f + 0.7f;
            float halfW = session.Map.Width * 0.5f + 0.45f;
            cam.orthographicSize = Mathf.Max(halfH, halfW / aspect);
            cam.transform.position = new Vector3((session.Map.Width - 1) * 0.5f, (session.Map.Height - 1) * 0.5f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BattlePalette.Background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 50f;
        }

        void EnsureCamera()
        {
            if (Camera.main != null)
                return;
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            go.AddComponent<AudioListener>();
        }

        void WarmFont()
        {
            if (font == null)
                return;
            StringBuilder sample = new StringBuilder();
            sample.Append(battleJson);
            sample.Append("关羽守荆州吕蒙陆逊潘璋朱然韩当关平周仓廖化步兵骑兵弓兵兵力回合胜利失败攻击移动结束待命地形平原森林山地河川渡口城池荆州我军敌军主将敌将克制被克均势射程反击可击退无撤退城池陷落防守成功再战点选武将绿色红色远射不会被近战相克仍有未行动请先按取消选择行动中进入向推进伤害归零失守攻势被击退尽数来犯率江东军守住第回合");
            sample.Append("0123456789");
            font.RequestCharactersInTexture(sample.ToString(), 32, FontStyle.Normal);
            font.RequestCharactersInTexture(sample.ToString(), 22, FontStyle.Normal);
            font.RequestCharactersInTexture(sample.ToString(), 16, FontStyle.Normal);
        }

        static Font LoadFont()
        {
            Font bundled = Resources.Load<Font>("Fonts/JingzhouFont");
            if (bundled != null)
                return bundled;
            string[] preferred = {
                "Droid Sans Fallback",
                "Noto Sans CJK SC",
                "Noto Sans SC",
                "Source Han Sans SC",
                "WenQuanYi Micro Hei",
                "Microsoft YaHei",
                "PingFang SC",
                "Heiti SC",
                "SimHei",
                "Arial Unicode MS"
            };
            Font os = Font.CreateDynamicFontFromOSFont(preferred, 32);
            if (os != null)
                return os;
            return Font.CreateDynamicFontFromOSFont("Arial", 32);
        }

        static bool WasClick()
        {
            return Input.GetMouseButtonDown(0);
        }

        static bool WasCancel()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
    }
}
