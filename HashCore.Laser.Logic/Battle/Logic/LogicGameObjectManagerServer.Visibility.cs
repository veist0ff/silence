namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Одна точка, с которой команда смотрит на поле. Из LogicTeamMemberElement.
    ///
    /// Смотрит команда не только глазами своих бойцов: вышки, метки и прочие
    /// «глаза» попадают сюда же, каждый со своим радиусом и формой.
    /// </summary>
    public class LogicTeamMemberElement
    {
        /// <summary>Откуда смотрят.</summary>
        public int X;
        public int Y;

        /// <summary>Поле +8. Радиус считается кругом, а не ромбом.</summary>
        public bool Circular;

        /// <summary>Поле +12. Радиус обзора в клетках.</summary>
        public int RadiusTiles;

        /// <summary>Поле +16. В каком измерении эти глаза находятся.</summary>
        public int DimensionIndex;
    }

    /// <summary>
    /// Видимость точки для команды: 0x10046ebf4, 0x10046ee60, 0x10046eb44.
    /// </summary>
    public partial class LogicGameObjectManagerServer
    {
        /// <summary>Отступы окна обзора, с которыми зовут isInsideVisibilityRect.</summary>
        public const int VisibilityRectMargin = 100;

        /// <summary>Радиус в клетках, с которым смотрит сам боец.</summary>
        public const int OwnVisibilityRadiusTiles = 2;

        /// <summary>Виды зон, сквозь которые команда тоже видит поле.</summary>
        public const int VisionAreaEffectA = 28;
        public const int VisionAreaEffectB = 29;

        /// <summary>Поле +128. Объекты, внутри которых никого не видно.</summary>
        public readonly List<LogicGameObjectServer> VisibilityBlockers = new List<LogicGameObjectServer>();

        /// <summary>
        /// 0x10046eb44. Не закрыта ли точка чем-то, что глушит обзор.
        ///
        /// Пока таких объектов нет, видно всюду. Как только появился хоть один,
        /// видно только то, что попало внутрь его радиуса, — снаружи обзора нет.
        /// </summary>
        public bool VisibilityStormCheck(int x, int y)
        {
            if (VisibilityBlockers.Count == 0) return true;

            for (int i = 0; i < VisibilityBlockers.Count; i++)
            {
                LogicGameObjectServer blocker = VisibilityBlockers[i];
                if (blocker == null) continue;

                int distance = LogicGamePlayUtil.GetDistanceSquaredBetween(blocker.X, blocker.Y, x, y);
                int radius = blocker.GetRadius();

                if (distance < radius * radius) return true;
            }

            return false;
        }

        /// <summary>
        /// 0x10046ebf4. Видит ли команда точку, если не считать стен.
        ///
        /// Сначала точка должна попасть в окно обзора смотрящего — за краем его
        /// экрана нет ничего. Дальше её ищут по очереди: своими глазами, глазами
        /// союзников из того же измерения и зонами обзора своей команды. Хватает
        /// любого одного.
        /// </summary>
        public bool IsPositionVisibleToPlayerTeam(
            LogicTileMap map,
            int fromX, int fromY,
            List<LogicTeamMemberElement> teamMembers,
            int toX, int toY,
            bool mirrored,
            int teamIndex,
            int dimensionIndex,
            bool checkStorm)
        {
            if (map == null) return false;

            if (!map.IsInsideVisibilityRect(
                    fromX, fromY, toX, toY,
                    VisibilityRectMargin, VisibilityRectMargin, VisibilityRectMargin,
                    mirrored))
            {
                return false;
            }

            if (checkStorm && !VisibilityStormCheck(toX, toY)) return false;

            if (teamMembers != null)
            {
                for (int i = 0; i < teamMembers.Count; i++)
                {
                    LogicTeamMemberElement member = teamMembers[i];
                    if (member == null || member.DimensionIndex != dimensionIndex) continue;

                    if (map.CanPlayerSeeFromToNoLOS(
                            member.X, member.Y, toX, toY, member.RadiusTiles, member.Circular))
                    {
                        return true;
                    }
                }
            }

            if (map.CanPlayerSeeFromToNoLOS(fromX, fromY, toX, toY, OwnVisibilityRadiusTiles, false))
                return true;

            for (int i = 0; i < AreaEffects.Count; i++)
            {
                LogicAreaEffectServer area = AreaEffects[i] as LogicAreaEffectServer;
                if (area == null) continue;

                int kind = area.GetVisionKind();
                if (kind != VisionAreaEffectA && kind != VisionAreaEffectB) continue;

                if (area.TeamIndex != teamIndex) continue;

                int radiusTiles = area.GetRadius() / LogicTile.TileSize;

                if (map.CanPlayerSeeFromToNoLOS(area.X, area.Y, toX, toY, radiusTiles, true))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 0x10046ee60. То же, но ещё и со стенами: видно только то, до чего есть
        /// прямая линия.
        /// </summary>
        public bool IsPositionVisibleToPlayerTeamAndLineOfSightNotBlocked(
            LogicTileMap map,
            int fromX, int fromY,
            List<LogicTeamMemberElement> teamMembers,
            int toX, int toY,
            bool mirrored,
            int teamIndex,
            int dimensionIndex,
            bool checkStorm)
        {
            if (!IsPositionVisibleToPlayerTeam(
                    map, fromX, fromY, teamMembers, toX, toY, mirrored, teamIndex, dimensionIndex, checkStorm))
            {
                return false;
            }

            return map.IsPlayerLineOfSightClear(fromX, fromY, toX, toY);
        }
    }
}
