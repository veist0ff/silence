namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Подбор и потеря предметов. Из tick персонажа и его смерти.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Сколько подобранного боец несёт на себе.</summary>
        public int CarriedItemCount;

        /// <summary>
        /// Тянет к себе всё, до чего дотянулся.
        ///
        /// Проверка идёт по расстоянию до предмета, а не по клеткам: кристалл
        /// подбирается на бегу, и упираться в сетку тут нечем.
        /// </summary>
        protected virtual void TickItemPickup()
        {
            if (Manager == null || !IsAlive()) return;

            List<LogicGameObjectServer> items = Manager.Items;

            for (int i = 0; i < items.Count; i++)
            {
                LogicPointItem point = items[i] as LogicPointItem;
                if (point == null || !point.CanBePickedUpBy(this)) continue;

                if (point.DimensionIndex != DimensionIndex) continue;

                int distance = LogicGamePlayUtil.GetDistanceSquaredBetween(X, Y, point.X, point.Y);
                if (distance >= LogicPointItem.PickUpRange * LogicPointItem.PickUpRange) continue;

                point.PickUp(this);
            }
        }

        /// <summary>
        /// 0x100460638 и соседние. Что делает подобранный предмет.
        ///
        /// Кристалл прибавляет очко игроку и остаётся висеть на бойце: при смерти
        /// он его потеряет, поэтому счёт и число несомого ведутся вместе.
        /// </summary>
        public virtual void ApplyItem(LogicItemServer item)
        {
            LogicPointItem point = item as LogicPointItem;
            if (point == null) return;

            CarriedItemCount++;

            LogicPlayer player = GetPlayer();
            if (player != null) player.AddScore(1);
        }

        /// <summary>
        /// Хвост смерти для режимов с носимым. Всё, что боец нёс, рассыпается
        /// вокруг него и снова становится ничьим.
        ///
        /// Каждый кристалл летит в свою сторону на случайное расстояние и
        /// подтягивается к ближайшему месту, где можно стоять: иначе половина
        /// рассыпанного оказывалась бы в стене.
        /// </summary>
        protected virtual void DropCarriedItems(ItemData pointData)
        {
            if (CarriedItemCount < 1 || pointData == null) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            LogicTileMap map = battle.TileMap;
            if (map == null) return;

            int count = CarriedItemCount;
            CarriedItemCount = 0;

            LogicPlayer player = GetPlayer();
            if (player != null) player.AddScore(-count);

            for (int i = 0; i < count; i++)
            {
                int distance = battle.GetRandomInt(300, 600);
                int angle = battle.GetRandomInt(360);

                int toX = ClampInsideMap(X + LogicMath.GetRotatedX(distance, 0, angle), map.LogicWidth);
                int toY = ClampInsideMap(Y + LogicMath.GetRotatedY(distance, 0, angle), map.LogicHeight);

                int spotX;
                int spotY;
                LogicGamePlayUtil.GetClosestPassableSpot(0, toX, toY, map, out spotX, out spotY, true, false);

                LogicPointItem point = new LogicPointItem(pointData);

                battle.GameObjectManager.AddGameObject(point);
                point.Throw(X, Y, spotX, spotY);
            }
        }
    }
}
