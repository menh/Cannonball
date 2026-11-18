using System;
using System.Collections.Generic;

namespace Jingzhou.Battle
{
    public sealed class BattleSession
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly int MaxTurns;
        public readonly BattleMap Map;
        public readonly List<UnitState> Units = new List<UnitState>();
        public readonly List<string> Log = new List<string>();
        public readonly UnitState GuanYu;
        public readonly UnitState EnemyCommander;

        public int Turn;
        public BattlePhase Phase;
        public BattleOutcome Outcome;
        public VictoryKind Victory;
        public DefeatKind Defeat;
        public string OutcomeHeadline = "";
        public string OutcomeDetail = "";

        public BattleSession(BattleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");
            if (definition.MaxTurns < 1)
                throw new FormatException("maxTurns must be at least 1.");
            Title = string.IsNullOrEmpty(definition.Title) ? "关羽守荆州" : definition.Title;
            Subtitle = definition.Subtitle ?? "";
            MaxTurns = definition.MaxTurns;
            Map = BattleMap.FromRows(definition.Width, definition.Height, definition.RowsNorthToSouth, definition.CityX, definition.CityY);

            UnitState guanYu = null;
            UnitState commander = null;
            HashSet<string> ids = new HashSet<string>();
            HashSet<GridPos> occupied = new HashSet<GridPos>();
            for (int i = 0; i < definition.Units.Count; i++)
            {
                UnitDef def = definition.Units[i];
                if (def == null || string.IsNullOrEmpty(def.Id) || string.IsNullOrEmpty(def.Name))
                    throw new FormatException("Every unit needs an id and a name.");
                if (!ids.Add(def.Id))
                    throw new FormatException("Duplicate unit id \"" + def.Id + "\".");
                if (def.Troops <= 0 || def.Move <= 0 || def.Range <= 0)
                    throw new FormatException(def.Name + " needs troops, move, and range above 0.");
                if (!Map.InBounds(def.X, def.Y))
                    throw new FormatException(def.Name + " is outside the map.");
                if (Map.MoveCost(def.X, def.Y) < 0)
                    throw new FormatException(def.Name + " starts on impassable terrain.");
                GridPos pos = new GridPos(def.X, def.Y);
                if (!occupied.Add(pos))
                    throw new FormatException("Two units start on the same tile.");

                UnitState unit = new UnitState();
                unit.Id = def.Id;
                unit.Name = def.Name;
                unit.Type = def.Type;
                unit.Faction = def.Faction;
                unit.MaxTroops = def.Troops;
                unit.Troops = def.Troops;
                unit.Might = def.Might;
                unit.Move = def.Move;
                unit.AttackRange = def.Range;
                unit.IsCommander = def.Commander;
                unit.IsGuanYu = def.GuanYu;
                unit.X = def.X;
                unit.Y = def.Y;
                unit.Act = ActState.Ready;
                Units.Add(unit);
                if (def.GuanYu)
                {
                    if (guanYu != null)
                        throw new FormatException("Only one Guan Yu can be marked.");
                    guanYu = unit;
                }
                if (def.Commander && def.Faction == Faction.Enemy)
                {
                    if (commander != null)
                        throw new FormatException("Only one enemy commander can be marked.");
                    commander = unit;
                }
            }

            if (guanYu == null || guanYu.Faction != Faction.Player)
                throw new FormatException("The player side needs 关羽 (guanYu: true).");
            if (commander == null)
                throw new FormatException("The enemy side needs one commander.");
            GuanYu = guanYu;
            EnemyCommander = commander;

            Turn = 1;
            Phase = BattlePhase.Player;
            Outcome = BattleOutcome.Ongoing;
            if (!string.IsNullOrEmpty(Subtitle))
                Log.Add(Subtitle);
            Log.Add("—— 第1回合 · 我军 ——");
        }

        public UnitState UnitAt(int x, int y)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                UnitState unit = Units[i];
                if (unit.IsAlive && unit.X == x && unit.Y == y)
                    return unit;
            }
            return null;
        }

        public bool CanAct(UnitState unit)
        {
            return Outcome == BattleOutcome.Ongoing
                && Phase == BattlePhase.Player
                && unit != null
                && unit.Faction == Faction.Player
                && unit.IsAlive
                && unit.Act != ActState.Done
                && Units.Contains(unit);
        }

        public MoveField GetMoveField(UnitState unit)
        {
            return Pathfinder.Build(Map, Units, unit);
        }

        public List<UnitState> AttackTargetsFrom(UnitState unit, int x, int y)
        {
            List<UnitState> targets = new List<UnitState>();
            if (unit == null || !unit.IsAlive)
                return targets;
            for (int i = 0; i < Units.Count; i++)
            {
                UnitState other = Units[i];
                if (other.Faction == unit.Faction || !other.IsAlive)
                    continue;
                if (CanStrike(unit, x, y, other))
                    targets.Add(other);
            }
            return targets;
        }

        public bool CanStrike(UnitState attacker, int fromX, int fromY, UnitState defender)
        {
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive)
                return false;
            if (attacker.Faction == defender.Faction)
                return false;
            int distance = Math.Abs(defender.X - fromX) + Math.Abs(defender.Y - fromY);
            if (distance < 1 || distance > attacker.AttackRange)
                return false;
            return HasClearShot(attacker, fromX, fromY, defender);
        }

        public bool HasClearShot(UnitState attacker, int fromX, int fromY, UnitState defender)
        {
            int dx = defender.X - fromX;
            int dy = defender.Y - fromY;
            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);
            if (adx + ady <= 1)
                return true;
            if (adx != 0 && ady != 0)
                return false;
            int sx = adx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int sy = ady == 0 ? 0 : (dy > 0 ? 1 : -1);
            int x = fromX + sx;
            int y = fromY + sy;
            while (x != defender.X || y != defender.Y)
            {
                if (!ShotTileClear(attacker, defender, x, y))
                    return false;
                x += sx;
                y += sy;
            }
            return true;
        }

        bool ShotTileClear(UnitState attacker, UnitState defender, int x, int y)
        {
            if (!Map.InBounds(x, y))
                return false;
            UnitState occupant = UnitAt(x, y);
            if (occupant == null || occupant == attacker || occupant == defender)
                return true;
            return false;
        }

        public MoveResult PlayerMove(UnitState unit, int x, int y)
        {
            MoveResult result = new MoveResult();
            result.Unit = unit;
            if (!CanAct(unit))
            {
                result.Reason = "现在不能移动。";
                return result;
            }
            if (unit.Act != ActState.Ready)
            {
                result.Reason = "这员武将已经移动过了。";
                return result;
            }
            MoveField field = GetMoveField(unit);
            GridPos dest = new GridPos(x, y);
            if (!field.Stops.Contains(dest))
            {
                result.Reason = "无法移动到那里。";
                return result;
            }
            result.Path = field.PathTo(dest);
            result.Ok = true;
            if (unit.X != x || unit.Y != y)
            {
                unit.X = x;
                unit.Y = y;
                Log.Add(unit.Name + " 移动。");
            }
            unit.Act = ActState.Moved;
            return result;
        }

        public AttackResult PlayerAttack(UnitState attacker, UnitState defender)
        {
            AttackResult result = new AttackResult();
            if (!CanAct(attacker))
            {
                result.Reason = "现在不能攻击。";
                return result;
            }
            if (defender == null || !defender.IsAlive || defender.Faction == attacker.Faction)
            {
                result.Reason = "没有可攻击的目标。";
                return result;
            }
            int distance = BattleRules.Distance(attacker, defender);
            if (distance < 1 || distance > attacker.AttackRange)
            {
                result.Reason = "目标不在射程内。";
                return result;
            }
            if (!HasClearShot(attacker, attacker.X, attacker.Y, defender))
            {
                result.Reason = "射界被挡住了。";
                return result;
            }
            result.Report = ResolveCombat(attacker, defender, distance);
            attacker.Act = ActState.Done;
            result.Ok = true;
            Evaluate();
            return result;
        }

        public bool PlayerWait(UnitState unit, out string reason)
        {
            if (!CanAct(unit))
            {
                reason = "现在不能待命。";
                return false;
            }
            unit.Act = ActState.Done;
            Log.Add(unit.Name + " 待命。");
            reason = "";
            return true;
        }

        public void EndPlayerTurn()
        {
            if (Outcome != BattleOutcome.Ongoing || Phase != BattlePhase.Player)
                return;
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Faction == Faction.Player)
                    Units[i].Act = ActState.Done;
            }
            Phase = BattlePhase.Enemy;
            Log.Add("—— 敌军行动 ——");
        }

        public EnemyAction TakeEnemyAction()
        {
            if (Outcome != BattleOutcome.Ongoing || Phase != BattlePhase.Enemy)
                return null;

            UnitState actor = NextEnemy();
            if (actor == null)
            {
                CloseEnemyPhase();
                EnemyAction ended = new EnemyAction();
                ended.PhaseEnded = true;
                return ended;
            }

            EnemyAi.Plan plan = EnemyAi.Choose(this, actor);
            EnemyAction action = new EnemyAction();
            action.Unit = actor;
            MoveField field = GetMoveField(actor);
            GridPos dest = new GridPos(plan.X, plan.Y);
            if (!field.Stops.Contains(dest))
                dest = new GridPos(actor.X, actor.Y);

            if (actor.X != dest.X || actor.Y != dest.Y)
            {
                action.Path = field.PathTo(dest);
                actor.X = dest.X;
                actor.Y = dest.Y;
                action.Moved = true;
                if (Map.IsCity(actor.X, actor.Y))
                {
                    action.CapturedCity = true;
                    actor.Act = ActState.Done;
                    Log.Add(actor.Name + " 进入荆州，城池陷落。");
                    SetDefeat(DefeatKind.CityFallen, "敌军踏入荆州，城池陷落。");
                    return action;
                }
            }
            else
            {
                action.Path = new List<GridPos>();
                action.Path.Add(dest);
            }

            if (plan.Target != null && plan.Target.IsAlive && Units.Contains(plan.Target))
            {
                int distance = BattleRules.Distance(actor, plan.Target);
                if (CanStrike(actor, actor.X, actor.Y, plan.Target))
                    action.Attack = ResolveCombat(actor, plan.Target, distance);
            }

            if (!action.Moved && action.Attack == null)
                Log.Add(actor.Name + " 待命。");
            else if (action.Moved && action.Attack == null)
                Log.Add(actor.Name + " 向荆州推进。");

            actor.Act = ActState.Done;
            Evaluate();
            return action;
        }

        UnitState NextEnemy()
        {
            UnitState best = null;
            int bestDist = int.MaxValue;
            for (int i = 0; i < Units.Count; i++)
            {
                UnitState unit = Units[i];
                if (unit.Faction != Faction.Enemy || !unit.IsAlive || unit.Act == ActState.Done)
                    continue;
                int dist = Math.Abs(unit.X - Map.CityX) + Math.Abs(unit.Y - Map.CityY);
                if (best == null || dist < bestDist || (dist == bestDist && (unit.X < best.X || (unit.X == best.X && unit.Y < best.Y))))
                {
                    best = unit;
                    bestDist = dist;
                }
            }
            return best;
        }

        AttackReport ResolveCombat(UnitState attacker, UnitState defender, int distance)
        {
            CombatPreview preview = BattleRules.Preview(
                attacker,
                defender,
                Map.Defense(defender.X, defender.Y),
                Map.Defense(attacker.X, attacker.Y),
                distance);

            defender.Troops -= preview.Damage;
            if (defender.Troops < 0)
                defender.Troops = 0;
            if (preview.WillCounter)
            {
                attacker.Troops -= preview.CounterDamage;
                if (attacker.Troops < 0)
                    attacker.Troops = 0;
            }

            AttackReport report = new AttackReport();
            report.Attacker = attacker;
            report.Defender = defender;
            report.Damage = preview.Damage;
            report.CounterDamage = preview.WillCounter ? preview.CounterDamage : 0;
            report.DidCounter = preview.WillCounter;
            report.DefenderDefeated = defender.Troops <= 0;
            report.AttackerDefeated = attacker.Troops <= 0;
            report.MatchupLabel = preview.MatchupLabel;

            string line = attacker.Name + " 攻击 " + defender.Name + "，伤害 " + report.Damage + "（" + report.MatchupLabel + "）。";
            if (report.DidCounter)
                line += defender.Name + " 反击，伤害 " + report.CounterDamage + "。";
            Log.Add(line);
            if (report.DefenderDefeated)
            {
                Log.Add(defender.Name + " 兵力归零，撤退。");
                Units.Remove(defender);
            }
            if (report.AttackerDefeated)
            {
                Log.Add(attacker.Name + " 兵力归零，撤退。");
                Units.Remove(attacker);
            }
            return report;
        }

        void Evaluate()
        {
            if (Outcome != BattleOutcome.Ongoing)
                return;
            if (GuanYu.Troops <= 0 || !Units.Contains(GuanYu))
            {
                SetDefeat(DefeatKind.GuanYuFallen, "关羽兵力归零，荆州失守。");
                return;
            }
            if (EnemyCommander.Troops <= 0 || !Units.Contains(EnemyCommander))
            {
                SetVictory(VictoryKind.CommanderDefeated, EnemyCommander.Name + "兵力归零，江东军撤退。");
                return;
            }
            bool anyEnemy = false;
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Faction == Faction.Enemy && Units[i].IsAlive)
                {
                    anyEnemy = true;
                    break;
                }
            }
            if (!anyEnemy)
                SetVictory(VictoryKind.EnemyRouted, "来犯之敌已尽数撤退。");
        }

        void CloseEnemyPhase()
        {
            if (Outcome != BattleOutcome.Ongoing)
                return;
            Evaluate();
            if (Outcome != BattleOutcome.Ongoing)
                return;
            if (Turn >= MaxTurns)
            {
                SetVictory(VictoryKind.AttackRepelled, "第" + MaxTurns + "回合结束，荆州仍在。敌军攻势被击退。");
                return;
            }
            Turn++;
            for (int i = 0; i < Units.Count; i++)
                Units[i].Act = ActState.Ready;
            Phase = BattlePhase.Player;
            Log.Add("—— 第" + Turn + "回合 · 我军 ——");
        }

        void SetVictory(VictoryKind kind, string detail)
        {
            Outcome = BattleOutcome.Victory;
            Victory = kind;
            OutcomeHeadline = "胜利";
            OutcomeDetail = detail;
            Log.Add(detail);
        }

        void SetDefeat(DefeatKind kind, string detail)
        {
            Outcome = BattleOutcome.Defeat;
            Defeat = kind;
            OutcomeHeadline = "失败";
            OutcomeDetail = detail;
            Log.Add(detail);
        }
    }
}
