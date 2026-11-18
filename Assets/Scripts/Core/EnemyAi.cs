using System.Collections.Generic;

namespace Jingzhou.Battle
{
    public static class EnemyAi
    {
        public struct Plan
        {
            public int X;
            public int Y;
            public UnitState Target;
        }

        public static Plan Choose(BattleSession session, UnitState unit)
        {
            MoveField field = Pathfinder.Build(session.Map, session.Units, unit);
            Plan best = new Plan();
            best.X = unit.X;
            best.Y = unit.Y;
            best.Target = null;
            int bestScore = int.MinValue;
            int bestDist = int.MaxValue;
            int bestX = int.MaxValue;
            int bestY = int.MaxValue;

            foreach (GridPos tile in field.Stops)
            {
                UnitState target = null;
                int attackScore = int.MinValue;
                for (int i = 0; i < session.Units.Count; i++)
                {
                    UnitState foe = session.Units[i];
                    if (foe.Faction == unit.Faction || !foe.IsAlive)
                        continue;
                    if (!session.CanStrike(unit, tile.X, tile.Y, foe))
                        continue;
                    int distance = MathAbs(foe.X - tile.X) + MathAbs(foe.Y - tile.Y);
                    CombatPreview preview = BattleRules.Preview(
                        unit,
                        foe,
                        session.Map.Defense(foe.X, foe.Y),
                        session.Map.Defense(tile.X, tile.Y),
                        distance);
                    int value = preview.Damage * 3 - preview.CounterDamage * 2;
                    if (preview.DefenderDies)
                        value += 220;
                    if (foe.IsGuanYu)
                        value += 260;
                    if (preview.Matchup > 1.01f)
                        value += 36;
                    if (preview.AttackerDies && !preview.DefenderDies)
                        value -= 300;
                    if (value > attackScore)
                    {
                        attackScore = value;
                        target = foe;
                    }
                }

                int cityDist = MathAbs(session.Map.CityX - tile.X) + MathAbs(session.Map.CityY - tile.Y);
                int score = (40 - cityDist) * 14;
                if (session.Map.IsCity(tile.X, tile.Y))
                    score += 80000;
                if (target != null)
                    score += 340 + attackScore;

                bool better = score > bestScore
                    || (score == bestScore && cityDist < bestDist)
                    || (score == bestScore && cityDist == bestDist && tile.X < bestX)
                    || (score == bestScore && cityDist == bestDist && tile.X == bestX && tile.Y < bestY);
                if (!better)
                    continue;
                bestScore = score;
                bestDist = cityDist;
                bestX = tile.X;
                bestY = tile.Y;
                best.X = tile.X;
                best.Y = tile.Y;
                best.Target = target;
            }

            return best;
        }

        static int MathAbs(int value)
        {
            return value < 0 ? -value : value;
        }
    }
}
