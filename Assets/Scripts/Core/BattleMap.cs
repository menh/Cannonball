using System;

namespace Jingzhou.Battle
{
    public sealed class BattleMap
    {
        public const float ForestDefense = 0.15f;
        public const float HillDefense = 0.22f;
        public const float CityDefense = 0.28f;

        public readonly int Width;
        public readonly int Height;
        public readonly int CityX;
        public readonly int CityY;
        readonly TerrainKind[] cells;

        BattleMap(int width, int height, int cityX, int cityY, TerrainKind[] cells)
        {
            Width = width;
            Height = height;
            CityX = cityX;
            CityY = cityY;
            this.cells = cells;
        }

        public static BattleMap FromRows(int width, int height, string[] rowsNorthToSouth, int cityX, int cityY)
        {
            if (width < 4 || height < 4 || width > 32 || height > 32)
                throw new FormatException("Map size must be between 4 and 32.");
            if (rowsNorthToSouth == null || rowsNorthToSouth.Length != height)
                throw new FormatException("Terrain rows must match the map height. Row 0 is the north edge.");
            if (cityX < 0 || cityY < 0 || cityX >= width || cityY >= height)
                throw new FormatException("Jingzhou city tile is outside the map.");

            TerrainKind[] cells = new TerrainKind[width * height];
            for (int row = 0; row < height; row++)
            {
                string line = rowsNorthToSouth[row] ?? "";
                if (line.Length != width)
                    throw new FormatException("Terrain row " + row + " must be " + width + " characters.");
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                    cells[y * width + x] = Parse(line[x], row, x);
            }

            cells[cityY * width + cityX] = TerrainKind.City;
            return new BattleMap(width, height, cityX, cityY, cells);
        }

        static TerrainKind Parse(char c, int row, int x)
        {
            switch (c)
            {
                case '.': return TerrainKind.Plain;
                case 'F': return TerrainKind.Forest;
                case 'H': return TerrainKind.Hill;
                case 'R': return TerrainKind.River;
                case '+': return TerrainKind.Road;
                case 'C': return TerrainKind.City;
                default:
                    throw new FormatException("Unknown terrain '" + c + "' at row " + row + ", column " + x + ".");
            }
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool IsCity(int x, int y)
        {
            return x == CityX && y == CityY;
        }

        public TerrainKind Get(int x, int y)
        {
            return cells[y * Width + x];
        }

        public int MoveCost(int x, int y)
        {
            if (!InBounds(x, y))
                return -1;
            switch (Get(x, y))
            {
                case TerrainKind.Plain:
                case TerrainKind.Road:
                case TerrainKind.City:
                    return 1;
                case TerrainKind.Forest:
                case TerrainKind.Hill:
                    return 2;
                default:
                    return -1;
            }
        }

        public float Defense(int x, int y)
        {
            switch (Get(x, y))
            {
                case TerrainKind.Forest: return ForestDefense;
                case TerrainKind.Hill: return HillDefense;
                case TerrainKind.City: return CityDefense;
                default: return 0f;
            }
        }

        public string Describe(int x, int y)
        {
            if (!InBounds(x, y))
                return "";
            switch (Get(x, y))
            {
                case TerrainKind.Plain: return "平原 · 移动消耗 1";
                case TerrainKind.Road: return "渡口 · 移动消耗 1";
                case TerrainKind.Forest: return "森林 · 移动消耗 2 · 防御 " + Percent(ForestDefense);
                case TerrainKind.Hill: return "山地 · 移动消耗 2 · 防御 " + Percent(HillDefense);
                case TerrainKind.City: return "荆州城 · 移动消耗 1 · 防御 " + Percent(CityDefense);
                default: return "河川 · 不可通行";
            }
        }

        public static string Percent(float value)
        {
            return ((int)Math.Round(value * 100f)).ToString() + "%";
        }
    }
}
