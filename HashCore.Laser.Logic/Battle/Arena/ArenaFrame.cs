namespace HashCore.Laser.Logic.Battle.Arena
{
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Кадр боя, написанный заново строго по разбору клиента v64:
    ///   LogicGameObjectManagerClient::decode (0x10046c51c) — заголовок, клетки,
    ///   игроки, хвост режима, список объектов;
    ///   LogicGameObjectClient::decode (0x10046bc2c) — начало каждого объекта;
    ///   LogicCharacterClient::decode (0x100447328) — тело бойца;
    ///   LogicSkillClient::decode (0x10047ea24) — навыки.
    /// Все «необязательные» блоки пишутся отсутствующими — это минимальный бой.
    /// Условия, от которых зависит, ЧИТАЕТ ли клиент поле, повторены дословно.
    /// </summary>
    public static class ArenaFrame
    {
        public static void Encode(BitStream s, ArenaBattle battle, BattlePlayer receiver, ArenaHero own)
        {
            int gmv = battle.GetGameModeVariation();
            GameModeVariationData gd = DataTables.Get(DataType.GameModeVariation)?.GetDataWithId<GameModeVariationData>(gmv);
            TileMap map = battle.GetTileMap();
            BattlePlayer[] players = battle.GetPlayers();

            // ---- заголовок
            int ownId = own?.ClientGlobalId ?? 0;
            s.WritePositiveIntMax2097151(ownId);
            s.WritePositiveIntMax2097151(ownId);
            s.WriteBoolean(false);                         // список Max63
            s.WriteBoolean(false);                         // Max255
            if (IsGemGrab(gmv)) s.WritePositiveVIntMax65535(0);
            s.WriteBoolean(false);                         // конец боя
            s.WriteIntMax15(-1);                           // состояние
            for (int i = 0; i < 4; i++) s.WriteBoolean(false);

            // ---- границы и клетки (вся карта)
            if (map.Width < 22)
            {
                s.WritePositiveIntMax31(0); s.WritePositiveIntMax63(0); s.WritePositiveIntMax31(map.Width - 1);
            }
            else
            {
                s.WritePositiveIntMax63(0); s.WritePositiveIntMax63(0); s.WritePositiveIntMax63(map.Width - 1);
            }
            s.WritePositiveIntMax63(map.Height - 1);
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    TileData t = map.GetTile(x, y, true, true).Data;
                    // LogicTile +66 / RestoreAfterDynamicOverlap / isIndestructibleWall
                    bool f66 = t.IsDestructible || (t.IsDestructibleNormalWeapon && (t.BlocksMovement || t.BlocksProjectiles));
                    bool wall = t.BlocksMovement && t.BlocksProjectiles && !t.IsDestructible;
                    if (f66 || t.RestoreAfterDynamicOverlap || wall) s.WriteBoolean(false);
                    if (t.BlocksProjectiles) s.WriteBoolean(false);
                }
            s.WritePositiveVIntMax65535OftenZero(0);       // изменённые клетки
            s.WritePositiveVIntMax65535OftenZero(0);       // временно непроходимые области
            s.WritePositiveVIntMax65535OftenZero(0);       // лужи

            // ---- игроки, в порядке StartLoading
            foreach (BattlePlayer p in players)
            {
                if (p == receiver)
                {
                    s.WritePositiveIntMax4095(0);          // ульта
                    s.WritePositiveIntMax3(0);
                    s.WritePositiveIntMax4095(0);          // перегрузка
                    s.WriteBoolean(false);
                    s.WriteBoolean(false);
                    s.WritePositiveVIntMax255OftenZero(0); // убийства
                    if (battle.EventModifiers.Contains(43)) s.WriteBoolean(false);
                    s.WriteBoolean(false);                 // вибрация
                }
                s.WritePositiveIntMax15(p.HeroIndex);
                s.WriteBoolean(false);                     // ссылка
                // гаджета нет (в StartLoading его нет) — блока хука владельца нет
                s.WritePositiveIntMax131071(0);
                ArenaHero h = battle.HeroOf(p);
                s.WriteBoolean(h != null && h.Alive);
                s.WriteBoolean(false);                     // ульта готова
                s.WriteBoolean(false);                     // бонус-навык
                s.WriteBoolean(false);                     // плохая связь
                s.WriteBoolean(false);                     // управляемый персонаж
                s.WriteBoolean(false);                     // +544
                if (gmv == 6 || gmv == 67 || gmv == 39 || gmv == 34) s.WritePositiveIntMax15(1); // 0 = индекс -1 у клиента, вылет шд
                if (gmv == 3) s.WriteBoolean(false);
                s.WritePositiveVIntMax255OftenZero(0);
                s.WriteBoolean(false);
                s.WriteBoolean(false);
                s.WriteBoolean(false);                     // пин
                s.WriteBoolean(false);                     // +144
                s.WriteIntMax16383(0);
                s.WritePositiveInt(0, 20);                 // Max1048575
                s.WriteBoolean(false);                     // uponDecodeBattleModeData
                s.WriteBoolean(false);
                s.WriteBoolean(false);                     // выбор усиления
                s.WriteIntMax31(0);                        // карты рогалика
                s.WriteBoolean(false); s.WriteBoolean(false); s.WriteBoolean(false);
                s.WriteIntMax31(0);
                s.WriteBoolean(false); s.WriteBoolean(false);
            }

            // ---- хвост режима
            s.WritePositiveVIntMax65535OftenZero(0);
            s.WritePositiveVIntMax255OftenZero(0);
            if (gd != null && gd.HasTimerAndCanEndBeforeTimerRunsOut) s.WritePositiveVIntMax65535OftenZero(0);
            if (gd != null && gd.RoundResetsWhenObjectiveIsMissing) { s.WriteBoolean(false); s.WriteIntMax1(0); }
            if (battle.EventModifiers.Contains(44)) { s.WriteIntMax63(0); s.WriteIntMax63(0); }
            WriteModeTail(s, battle, gmv, gd, players);

            foreach (BattlePlayer p in players)
            {
                s.WriteBoolean(false);                     // счёт
                s.WriteBoolean(false);                     // убийства
            }

            // ---- объекты: только бойцы
            List<ArenaHero> heroes = battle.Heroes;
            s.WritePositiveVIntMax65535(heroes.Count);
            foreach (ArenaHero h in heroes) ByteStreamHelper.WriteDataReference(s, h.Data.GetGlobalId());
            foreach (ArenaHero h in heroes) s.WritePositiveVIntMax65535(h.Instance);
            s.WriteBoolean(false);                         // отладочные пути
            foreach (ArenaHero h in heroes) EncodeHero(s, battle, h, h == own, gmv, gd, players.Length);
            s.WriteBoolean(false);                         // +216

            if (gmv == 41) { s.WritePositiveVIntMax255(0); s.WritePositiveVIntMax255(0); }
            else if (gmv == 7) s.WriteBoolean(false);
        }

        private static bool IsGemGrab(int v) => v == 0 || v == 33 || v == 46 || v == 65;

        /// <summary>Цепочка веток хвоста режима — дословно по клиенту.</summary>
        private static void WriteModeTail(BitStream s, ArenaBattle battle, int gmv, GameModeVariationData gd, BattlePlayer[] players)
        {
            if (gmv == 6 || gmv == 67 || gmv == 34)
            {
                int alive = 0;
                foreach (ArenaHero h in battle.Heroes) if (h.Alive) alive++;
                s.WritePositiveIntMax15(System.Math.Min(alive, 15));
                return;
            }
            if (gmv <= 59 && ((1L << gmv) & 0x800104000000200L) != 0)
            {
                var teams = new HashSet<int>();
                foreach (ArenaHero h in battle.Heroes) if (h.Alive) teams.Add(h.Team);
                s.WritePositiveIntMax7(System.Math.Min(teams.Count, 7));
                return;
            }
            if (gmv == 16) { s.WritePositiveIntMax8191(0); s.WritePositiveIntMax8191(0); return; }
            if (gd != null && gd.HasTwoBases)
            {
                s.WritePositiveIntMax127(100); s.WritePositiveIntMax127(100);
                if (gmv == 48)
                {
                    s.WritePositiveIntMax31(0); s.WritePositiveIntMax31(0); s.WriteBoolean(false);
                    s.WritePositiveIntMax127(0); s.WritePositiveIntMax127(0); s.WriteIntMax4095(0);
                }
                else if (gmv == 11)
                {
                    s.WritePositiveIntMax255(0); s.WritePositiveIntMax255(0);
                    s.WritePositiveIntMax31(0); s.WritePositiveIntMax31(0);
                    s.WritePositiveIntMax63(0); s.WritePositiveIntMax63(0); s.WriteBoolean(false);
                }
                return;
            }
            if (gmv == 7 || (gd != null && gd.IsTypeOfBossFight) || gmv == 14)
            {
                s.WritePositiveIntMax127(0);
                if (gmv == 18) s.WritePositiveIntMax127(0);
                else if (gmv == 14) s.WritePositiveIntMax16383(0);
                return;
            }
            if (gmv == 21 || gmv == 26 || gmv == 19) { s.WritePositiveIntMax127(0); s.WritePositiveIntMax127(0); return; }
            if (gd != null && gd.Name != null && gd.Name.StartsWith("PayloadSingle"))
            {
                for (int i = 0; i < 4; i++) s.WritePositiveIntMax127(0);
                return;
            }
            if (gmv == 8) { s.WritePositiveIntMax127(0); s.WritePositiveIntMax127(0); s.WriteBoolean(false); return; }
            int k = gmv - 28;
            if (k >= 0 && k <= 0x25)
            {
                if (((1L << k) & 0x3000000801L) != 0) { s.WriteBoolean(false); return; }
                if (k == 1) { s.WriteBoolean(false); s.WritePositiveIntMax31(0); s.WritePositiveIntMax127(0); s.WriteBoolean(false); return; }
                if (k == 8) { s.WritePositiveIntMax1023(0); s.WritePositiveIntMax3(0); s.WritePositiveIntMax3(0); return; }
            }
            if (gmv == 27) s.WritePositiveIntMax7(0);
            else if (gmv == 13) s.WritePositiveVIntMax2147483647OftenZero(0);
        }

        /// <summary>Тип заряда героя — как LogicCharacterClient::calculateChargeUp без карт.</summary>
        private static int ChargeUpType(ArenaHero h)
        {
            int t = 0;
            ProjectileData wp = h.Weapon != null ? DataTables.GetProjectileByName(h.Weapon.Projectile) : null;
            if (wp != null && HashCore.Laser.Logic.Battle.Logic.LogicDataAccess.GetBool(wp, "AttractsPet"))
                t = h.Data.UniqueProperty == 69 ? 11 : 1;
            if (h.Data.UniqueProperty == 3) t = 11;
            if (h.Data.UniqueProperty == 1) t = 8;
            if (h.Ulti != null && h.Ulti.ChargeType == 15) t = 7;
            if (h.Data.UniqueProperty == 10) t = 9;
            if (h.Data.UniqueProperty == 12) t = 10;
            if (h.Data.UniqueProperty == 17) t = 6;
            return t;
        }

        private static void EncodeHero(BitStream s, ArenaBattle battle, ArenaHero h, bool own, int gmv, GameModeVariationData gd, int playerCount)
        {
            CharacterData d = h.Data;

            // LogicGameObjectClient::decode
            s.WritePositiveVIntMax65535(h.X);
            s.WritePositiveVIntMax65535(h.Y);
            s.WritePositiveVIntMax65535(0);
            s.WritePositiveVIntMax255((h.Team << 4) | h.PlayerIndex);
            s.WritePositiveIntMax3(0);                     // состояние отрисовки
            s.WritePositiveIntMax15(10);                   // видимость
            s.WriteBoolean(false);
            s.WriteBoolean(false);

            // голова (герой — не «объект»)
            s.WriteBoolean(false);                         // контроль разума
            if (own) { s.WriteBoolean(false); s.WriteBoolean(false); s.WriteBoolean(false); }
            else { s.WritePositiveIntMax511(h.Angle); s.WritePositiveIntMax511(h.Angle); }
            s.WritePositiveIntMax7(h.Moving ? 1 : 0);      // состояние
            s.WriteBoolean(false);
            s.WriteIntMax63(0);
            s.WriteBoolean(false);
            s.WriteBoolean(false);                         // оглушение
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WriteBoolean(false);

            if (d.LifeTimeTicks >= 1) s.WritePositiveIntMax511(0);
            s.WritePositiveVIntMax65535OftenZero(0);       // эффект попадания
            s.WritePositiveVIntMax65535OftenZero(0);
            for (int i = 0; i < 9; i++) s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WriteBoolean(false);                         // перезарядка
            s.WriteBoolean(false);
            s.WritePositiveVIntMax255OftenZero(0);
            s.WriteBoolean(false);                         // статус-эффекты
            s.WriteBoolean(false);                         // компоненты
            s.WritePositiveVIntMax255OftenZero(0);         // яды
            s.WritePositiveVIntMax2147483647(h.Hitpoints);
            s.WritePositiveVIntMax2147483647(h.MaxHitpoints);
            if (h.Hitpoints <= 0) s.WriteIntMax15(0);
            s.WriteBoolean(false);

            // герой
            s.WritePositiveVIntMax255OftenZero(0);         // предметы / звёзды (0 даже с «−2»)
            if (gmv == 35) s.WritePositiveVIntMax65535OftenZero(0); else s.WritePositiveVIntMax255OftenZero(0);
            s.WriteBoolean(false);
            if (!h.Alive) s.WriteBoolean(false);
            s.WriteBoolean(false);                         // щит
            if (gmv == 35 || gmv == 48 || gmv == 58 || gmv == 59) s.WriteIntMax7(0); else s.WriteBoolean(false);

            if (gd != null && gd.ModeHasCarryables) s.WriteBoolean(false);
            s.WritePositiveVIntMax255OftenZero(0);         // заряд ульты
            s.WriteBoolean(false);                         // блок из 5 флагов
            if (own) s.WriteBoolean(false);                // обзор
            if (h.Weapon != null)
            {
                if (h.Weapon.ChargedShotCount >= 1) s.WriteIntMax3(0);
                if (own && h.Weapon.AttackPattern == 13) s.WritePositiveVIntMax255OftenZero(0);
                if (h.Weapon.AttackPattern == 15 || h.Weapon.AttackPattern == 17) s.WritePositiveVIntMax255OftenZero(0);
            }
            if (h.Ulti != null && h.Ulti.ChargeType == 15) s.WriteBoolean(false);
            if (d.ShouldEncodePetStatus || d.UniqueProperty == 13) s.WriteBoolean(false);
            if (d.PowerLevelsType == 1) s.WritePositiveIntMax7(0);
            else if (d.UniqueProperty == 1) s.WritePositiveIntMax3(0);
            else if (d.PowerLevelsType == 2) { s.WritePositiveIntMax31(0); s.WriteBoolean(false); }
            s.WriteBoolean(false);
            if (h.Ulti != null && DataTables.GetProjectileByName(h.Ulti.Projectile)?.UniqueProperty == 5) s.WriteBoolean(false);
            if (d.UniqueProperty == 19) s.WriteBoolean(false);
            switch (ChargeUpType(h))
            {
                case 0: break;
                case 1: case 7: case 11: case 12: s.WritePositiveVIntMax255OftenZero(0); break;
                case 5: s.WritePositiveIntMax2047(0); break;
                case 8: s.WritePositiveIntMax1023(0); break;
                case 9: s.WritePositiveIntMax7(0); break;
                default: s.WritePositiveIntMax1023(0); break;
            }

            // общий хвост
            if (d.PetAutoSpawnDelay >= 1) s.WritePositiveIntMax1023(0);
            if (!string.IsNullOrEmpty(d.Pet)) s.WriteBoolean(false);
            switch (d.UniqueProperty)
            {
                case 9: case 18: case 20: s.WriteBoolean(false); break;
                case 28: s.WriteBoolean(false); s.WriteBoolean(false); break;
                case 26: s.WriteBoolean(false); s.WriteBoolean(false); break;
                case 47: case 55: s.WritePositiveIntMax3(0); break;
            }
            if (d.UniqueProperty != 26) { s.WriteBoolean(false); s.WriteBoolean(false); }   // быстрое перемещение
            if (d.UniqueProperty == 22) s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            if (d.UniqueProperty == 11 && own) { s.WriteIntMax255(0); s.WritePositiveIntMax7(0); }
            if (d.UniqueProperty == 32) s.WriteBoolean(false);

            s.WriteBoolean(false);
            s.WritePositiveIntMax3(0);
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WritePositiveIntMax511(0);
            s.WriteBoolean(false); s.WriteBoolean(false); s.WriteBoolean(false); s.WriteBoolean(false);
            if (own) { s.WriteBoolean(false); s.WriteBoolean(false); }
            s.WritePositiveIntMax31(0);                    // числа урона

            // навыки: 4 ячейки
            EncodeSkill(s, h.Weapon, d, battle);
            EncodeSkill(s, h.Ulti, d, battle);
            s.WriteBoolean(false);
            s.WriteBoolean(false);

            s.WritePositiveIntMax3(0);
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            if (gmv == 48)
            {
                s.WritePositiveIntMax131071(0); s.WriteBoolean(false);
            }
            s.WritePositiveIntMax1023(0);
            s.WriteBoolean(false);
            s.WritePositiveVIntMax65535OftenZero(0);
            s.WritePositiveVIntMax2147483647OftenZero(0);
            s.WritePositiveIntMax3(0);
        }

        private static void EncodeSkill(BitStream s, SkillData sd, CharacterData owner, ArenaBattle battle)
        {
            if (!s.WriteBoolean(sd != null)) return;
            s.WritePositiveVIntMax255OftenZero(0);
            s.WritePositiveVIntMax255OftenZero(0);
            s.WriteBoolean(false);
            s.WriteBoolean(false);
            s.WritePositiveVIntMax255OftenZero(0);
            s.WritePositiveIntMax131071(0);
            s.WriteBoolean(false);
            if (sd.MaxCharge >= 1) s.WritePositiveIntMax4095(sd.MaxCharge * 1000 / (sd.HoldToShoot ? 20 : 1));
            if (sd.SkillChangeType != 0 || battle.EventModifiers.Contains(35)) s.WritePositiveIntMax511(sd.GetInstanceId());
            if (owner.UniqueProperty == 18) s.WriteBoolean(false);
            s.WriteBoolean(false);
        }
    }
}
