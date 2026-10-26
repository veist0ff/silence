namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Смерть персонажа, 0x100461528 и хвост causeDamage.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Очки за убийство в режиме на вылет.</summary>
        public const int ArcadeScoreKnockout = 500;

        /// <summary>Очки за убийство в режимах с одной вагонеткой.</summary>
        public const int ArcadeScorePayload = 150;

        /// <summary>Очки за убийство в режиме удержания зоны.</summary>
        public const int ArcadeScoreHotZone = 100;

        /// <summary>Очки за убийство во всех прочих режимах.</summary>
        public const int ArcadeScoreDefault = 200;

        /// <summary>Вид урона, при котором смерть не засчитывается убийце.</summary>
        public const int DamageTypeNoKillCredit = 9;

        /// <summary>Вид урона, при котором время убийства не запоминается.</summary>
        public const int DamageTypeNoKillTime = 17;

        /// <summary>
        /// 0x100461528. Записывает убийство убийце.
        ///
        /// Кроме самой записи сюда же идут очки за бой, и их размер зависит от
        /// режима: там, где убийство решает исход, оно и стоит дороже.
        /// </summary>
        public virtual void OnDeath(LogicPlayer killer, int bountyStars)
        {
            if (killer == null) return;

            killer.KilledPlayer(PlayerIndex, bountyStars);
            killer.Stats.PlayerKilled();
            killer.Stats.AddArcadeScore(GetArcadeScoreForKill());
        }

        /// <summary>Сколько очков за бой стоит убийство в этом режиме.</summary>
        protected virtual int GetArcadeScoreForKill()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return ArcadeScoreDefault;

            if (battle.IsKnockout()) return ArcadeScoreKnockout;
            if (battle.IsPayloadSingle()) return ArcadeScorePayload;
            if (battle.IsHotZone()) return ArcadeScoreHotZone;

            return ArcadeScoreDefault;
        }

        /// <summary>
        /// Хвост causeDamage, 0x100456360. Что происходит, когда персонаж умер.
        ///
        /// Убийство засчитывается не всегда: часть видов урона считается смертью
        /// без убийцы, а часть не оставляет и времени убийства. Записывается оно
        /// только тому, кто вообще способен убивать в этом режиме.
        /// </summary>
        protected virtual void HandleKillCredit(LogicCharacterServer attacker, int damageType)
        {
            if (attacker == null) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            LogicPlayer killer = attacker.GetPlayer();
            if (killer == null) return;

            if (damageType == DamageTypeNoKillCredit) return;
            if (!killer.IsAlive && !battle.HasRespawns()) return;

            OnDeath(killer, GetBountyStarsForKill());

            if (damageType == DamageTypeNoKillTime) return;

            killer.AddKilledPlayerTicks(PlayerIndex, battle.Tick);
        }

        /// <summary>
        /// Сколько звёзд убийца получает за эту жертву. В режимах без звёзд это
        /// всегда единица, в награде за голову — накопленное жертвой.
        /// </summary>
        protected virtual int GetBountyStarsForKill()
        {
            return 1;
        }
    }
}
