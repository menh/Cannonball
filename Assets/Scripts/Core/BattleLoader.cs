using System;
using System.Collections.Generic;

namespace Jingzhou.Battle
{
    public static class BattleLoader
    {
        public static BattleSession Load(string json)
        {
            return new BattleSession(Parse(json));
        }

        public static BattleDefinition Parse(string json)
        {
            Dictionary<string, object> root = SimpleJson.ParseObject(json);
            BattleDefinition definition = new BattleDefinition();
            definition.Title = SimpleJson.RequireString(root, "title");
            definition.Subtitle = SimpleJson.RequireString(root, "subtitle");
            definition.MaxTurns = SimpleJson.RequireInt(root, "maxTurns");
            definition.Width = SimpleJson.RequireInt(root, "width");
            definition.Height = SimpleJson.RequireInt(root, "height");
            definition.CityX = SimpleJson.RequireInt(root, "cityX");
            definition.CityY = SimpleJson.RequireInt(root, "cityY");

            List<object> rows = SimpleJson.RequireArray(root, "rows");
            definition.RowsNorthToSouth = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                string row = rows[i] as string;
                if (row == null)
                    throw new FormatException("Terrain row " + i + " must be a string.");
                definition.RowsNorthToSouth[i] = row;
            }

            AddUnits(definition, SimpleJson.RequireArray(root, "player"), Faction.Player);
            AddUnits(definition, SimpleJson.RequireArray(root, "enemy"), Faction.Enemy);
            return definition;
        }

        static void AddUnits(BattleDefinition definition, List<object> list, Faction faction)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Dictionary<string, object> obj = SimpleJson.AsObject(list[i], faction == Faction.Player ? "player unit" : "enemy unit");
                UnitDef unit = new UnitDef();
                unit.Id = SimpleJson.RequireString(obj, "id");
                unit.Name = SimpleJson.RequireString(obj, "name");
                unit.Type = ParseTroop(SimpleJson.RequireString(obj, "troop"));
                unit.Faction = faction;
                unit.Troops = SimpleJson.RequireInt(obj, "troops");
                unit.Might = SimpleJson.RequireInt(obj, "might");
                unit.Move = SimpleJson.RequireInt(obj, "move");
                unit.Range = SimpleJson.RequireInt(obj, "range");
                unit.X = SimpleJson.RequireInt(obj, "x");
                unit.Y = SimpleJson.RequireInt(obj, "y");
                unit.Commander = SimpleJson.OptionalBool(obj, "commander", false);
                unit.GuanYu = SimpleJson.OptionalBool(obj, "guanYu", false);
                definition.Units.Add(unit);
            }
        }

        static TroopType ParseTroop(string value)
        {
            if (value == "infantry" || value == "步兵") return TroopType.Infantry;
            if (value == "cavalry" || value == "骑兵") return TroopType.Cavalry;
            if (value == "archer" || value == "弓兵") return TroopType.Archer;
            throw new FormatException("Unknown troop type \"" + value + "\".");
        }
    }
}
