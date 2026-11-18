using System;
using System.Collections.Generic;

namespace Jingzhou.Battle
{
    public enum TroopType
    {
        Infantry,
        Cavalry,
        Archer
    }

    public enum Faction
    {
        Player,
        Enemy
    }

    public enum TerrainKind
    {
        Plain,
        Forest,
        Hill,
        River,
        Road,
        City
    }

    public enum ActState
    {
        Ready,
        Moved,
        Done
    }

    public enum BattlePhase
    {
        Player,
        Enemy
    }

    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat
    }

    public enum VictoryKind
    {
        None,
        CommanderDefeated,
        EnemyRouted,
        AttackRepelled
    }

    public enum DefeatKind
    {
        None,
        GuanYuFallen,
        CityFallen
    }

    public struct GridPos : IEquatable<GridPos>
    {
        public int X;
        public int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int Manhattan(int x, int y)
        {
            return Math.Abs(X - x) + Math.Abs(Y - y);
        }

        public bool Equals(GridPos other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos && Equals((GridPos)obj);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }
    }

    public sealed class UnitState
    {
        public string Id;
        public string Name;
        public TroopType Type;
        public Faction Faction;
        public int MaxTroops;
        public int Troops;
        public int Might;
        public int Move;
        public int AttackRange;
        public bool IsCommander;
        public bool IsGuanYu;
        public int X;
        public int Y;
        public ActState Act;

        public bool IsAlive
        {
            get { return Troops > 0; }
        }
    }

    public sealed class UnitDef
    {
        public string Id;
        public string Name;
        public TroopType Type;
        public Faction Faction;
        public int Troops;
        public int Might;
        public int Move;
        public int Range;
        public int X;
        public int Y;
        public bool Commander;
        public bool GuanYu;
    }

    public sealed class BattleDefinition
    {
        public string Title;
        public string Subtitle;
        public int MaxTurns;
        public int Width;
        public int Height;
        public int CityX;
        public int CityY;
        public string[] RowsNorthToSouth;
        public List<UnitDef> Units = new List<UnitDef>();
    }

    public sealed class CombatPreview
    {
        public int Damage;
        public int CounterDamage;
        public bool WillCounter;
        public bool DefenderDies;
        public bool AttackerDies;
        public float Matchup;
        public string MatchupLabel;
    }

    public sealed class AttackReport
    {
        public UnitState Attacker;
        public UnitState Defender;
        public int Damage;
        public int CounterDamage;
        public bool DidCounter;
        public bool DefenderDefeated;
        public bool AttackerDefeated;
        public string MatchupLabel;
    }

    public sealed class MoveResult
    {
        public bool Ok;
        public string Reason;
        public UnitState Unit;
        public List<GridPos> Path;
    }

    public sealed class AttackResult
    {
        public bool Ok;
        public string Reason;
        public AttackReport Report;
    }

    public sealed class EnemyAction
    {
        public bool PhaseEnded;
        public UnitState Unit;
        public bool Moved;
        public List<GridPos> Path;
        public bool CapturedCity;
        public AttackReport Attack;
    }
}
