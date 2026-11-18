using System;

namespace Jingzhou.Battle
{
    public static class BattleRules
    {
        public const float Advantage = 1.32f;
        public const float Disadvantage = 0.78f;
        public const float CounterScale = 0.55f;

        public static string TroopName(TroopType type)
        {
            switch (type)
            {
                case TroopType.Infantry: return "步兵";
                case TroopType.Cavalry: return "骑兵";
                case TroopType.Archer: return "弓兵";
                default: return "部队";
            }
        }

        public static string AdvantageText(TroopType type)
        {
            switch (type)
            {
                case TroopType.Cavalry: return "克制弓兵，被步兵克制";
                case TroopType.Archer: return "克制步兵，被骑兵克制";
                case TroopType.Infantry: return "克制骑兵，被弓兵克制";
                default: return "";
            }
        }

        public static float Matchup(TroopType attacker, TroopType defender)
        {
            if (attacker == TroopType.Cavalry && defender == TroopType.Archer) return Advantage;
            if (attacker == TroopType.Archer && defender == TroopType.Infantry) return Advantage;
            if (attacker == TroopType.Infantry && defender == TroopType.Cavalry) return Advantage;
            if (attacker == TroopType.Archer && defender == TroopType.Cavalry) return Disadvantage;
            if (attacker == TroopType.Infantry && defender == TroopType.Archer) return Disadvantage;
            if (attacker == TroopType.Cavalry && defender == TroopType.Infantry) return Disadvantage;
            return 1f;
        }

        public static string MatchupLabel(float matchup)
        {
            if (matchup > 1.01f) return "克制";
            if (matchup < 0.99f) return "被克";
            return "均势";
        }

        public static int ComputeDamage(UnitState attacker, UnitState defender, float defenderTerrain, float scale)
        {
            float ratio = attacker.Troops / (float)Math.Max(1, attacker.MaxTroops);
            float force = 0.75f + 0.25f * ratio;
            float raw = (9f + attacker.Might * 0.72f) * Matchup(attacker.Type, defender.Type) * force * (1f - defenderTerrain) * scale;
            int damage = (int)Math.Round(raw);
            if (damage < 1)
                damage = 1;
            return damage;
        }

        public static CombatPreview Preview(UnitState attacker, UnitState defender, float defenderTerrain, float attackerTerrain, int distance)
        {
            CombatPreview preview = new CombatPreview();
            preview.Damage = ComputeDamage(attacker, defender, defenderTerrain, 1f);
            preview.Matchup = Matchup(attacker.Type, defender.Type);
            preview.MatchupLabel = MatchupLabel(preview.Matchup);
            preview.DefenderDies = preview.Damage >= defender.Troops;
            bool canCounter = !preview.DefenderDies && distance >= 1 && distance <= defender.AttackRange;
            preview.WillCounter = canCounter;
            if (canCounter)
            {
                preview.CounterDamage = ComputeDamage(defender, attacker, attackerTerrain, CounterScale);
                preview.AttackerDies = preview.CounterDamage >= attacker.Troops;
            }
            return preview;
        }

        public static int Distance(UnitState a, int x, int y)
        {
            return Math.Abs(a.X - x) + Math.Abs(a.Y - y);
        }

        public static int Distance(UnitState a, UnitState b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
    }
}
