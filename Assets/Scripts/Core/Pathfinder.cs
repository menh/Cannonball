using System.Collections.Generic;

namespace Jingzhou.Battle
{
    public sealed class MoveField
    {
        public readonly Dictionary<GridPos, int> Cost = new Dictionary<GridPos, int>();
        public readonly Dictionary<GridPos, GridPos> CameFrom = new Dictionary<GridPos, GridPos>();
        public readonly HashSet<GridPos> Stops = new HashSet<GridPos>();

        public List<GridPos> PathTo(GridPos goal)
        {
            List<GridPos> path = new List<GridPos>();
            if (!Stops.Contains(goal) && !Cost.ContainsKey(goal))
                return path;
            GridPos cur = goal;
            int guard = 0;
            while (guard++ < 512)
            {
                path.Add(cur);
                GridPos previous;
                if (!CameFrom.TryGetValue(cur, out previous))
                    break;
                cur = previous;
            }
            path.Reverse();
            return path;
        }
    }

    public static class Pathfinder
    {
        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dy = { 0, 0, 1, -1 };

        public static MoveField Build(BattleMap map, IList<UnitState> units, UnitState mover)
        {
            MoveField field = new MoveField();
            HashSet<GridPos> allies = new HashSet<GridPos>();
            HashSet<GridPos> enemies = new HashSet<GridPos>();
            for (int i = 0; i < units.Count; i++)
            {
                UnitState other = units[i];
                if (other == mover || !other.IsAlive)
                    continue;
                GridPos pos = new GridPos(other.X, other.Y);
                if (other.Faction == mover.Faction)
                    allies.Add(pos);
                else
                    enemies.Add(pos);
            }

            GridPos start = new GridPos(mover.X, mover.Y);
            field.Cost[start] = 0;
            List<GridPos> open = new List<GridPos>();
            List<int> openCost = new List<int>();
            open.Add(start);
            openCost.Add(0);

            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (openCost[i] < openCost[best])
                        best = i;
                }
                GridPos current = open[best];
                int currentCost = openCost[best];
                open.RemoveAt(best);
                openCost.RemoveAt(best);
                int known;
                if (!field.Cost.TryGetValue(current, out known) || known != currentCost)
                    continue;

                for (int d = 0; d < 4; d++)
                {
                    int nx = current.X + Dx[d];
                    int ny = current.Y + Dy[d];
                    if (!map.InBounds(nx, ny))
                        continue;
                    int step = map.MoveCost(nx, ny);
                    if (step < 0)
                        continue;
                    int nextCost = currentCost + step;
                    if (nextCost > mover.Move)
                        continue;
                    GridPos next = new GridPos(nx, ny);
                    if (enemies.Contains(next))
                        continue;
                    int previous;
                    if (field.Cost.TryGetValue(next, out previous) && previous <= nextCost)
                        continue;
                    field.Cost[next] = nextCost;
                    field.CameFrom[next] = current;
                    open.Add(next);
                    openCost.Add(nextCost);
                }
            }

            foreach (KeyValuePair<GridPos, int> pair in field.Cost)
            {
                if (!allies.Contains(pair.Key))
                    field.Stops.Add(pair.Key);
            }
            return field;
        }
    }
}
