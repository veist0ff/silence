namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Счёт игрока за бой, LogicPlayerStats. Поле +472 игрока.
    ///
    /// Сюда складывается всё, что считается по итогам боя, а не по ходу его:
    /// сам бой на эти числа не смотрит.
    /// </summary>
    public class LogicPlayerStats
    {
        /// <summary>Сколько игрок убил.</summary>
        public int Kills;

        /// <summary>Очки за бой в аркадных режимах.</summary>
        public int ArcadeScore;

        /// <summary>0x1004f... Игрок кого-то убил.</summary>
        public void PlayerKilled()
        {
            Kills++;
        }

        public void AddArcadeScore(int amount)
        {
            ArcadeScore += amount;
        }
    }

    /// <summary>Одно убийство: кого убили и сколько звёзд за это дали.</summary>
    public readonly struct LogicKillEntry
    {
        public readonly int PlayerIndex;
        public readonly int BountyStarsEarned;

        public LogicKillEntry(int playerIndex, int bountyStarsEarned)
        {
            PlayerIndex = playerIndex;
            BountyStarsEarned = bountyStarsEarned;
        }
    }

    /// <summary>
    /// Игрок боя, 1:1 с LogicPlayer из либы.
    ///
    /// Смещения, подтверждённые по коду:
    ///   +12  индекс команды          (executeOneTick читает его для энкода)
    ///   +20  X прошлой позиции       (setPrevPos)
    ///   +24  Y прошлой позиции
    ///   +28  счёт                    (обнуляется на game over в режиме 34)
    ///   +188 событие тика            (resetEventsOnTick, 0x1004f171c)
    ///   +204 событие тика
    ///   +368 урон за тик             (increaseDamageDoneForTick, 0x1004f17bc)
    /// </summary>
    public class LogicPlayer
    {
        public string Name;

        /// <summary>Индекс команды. Поле +12.</summary>
        public int TeamIndex;

        /// <summary>
        /// Номер игрока внутри команды. Клиент узнаёт «свой» блок по паре
        /// «команда и номер», а не по месту в массиве, поэтому позиционное
        /// сравнение промахивается у второй команды.
        /// </summary>
        public int PlayerIndex;

        /// <summary>Индекс выбранного бойца.</summary>
        public int HeroIndex;

        /// <summary>Заряд ульты и заряд сверх нормы, оба в тысячных.</summary>
        public int UltiCharge;
        public int OverCharge;

        /// <summary>Игрок жив.</summary>
        public bool IsAlive = true;

        /// <summary>Ульта готова.</summary>
        public bool HasUlti;

        /// <summary>Поле +144. На игроке сейчас сверхзаряд.</summary>
        public bool IsHypercharged;

        /// <summary>У бойца игрока вообще есть сверхзаряд.</summary>
        public bool HasOverCharge;

        /// <summary>Сверхзаряд набран и его можно включить.</summary>
        public bool OverChargeReady;

        /// <summary>Сверхзаряд включён прямо сейчас.</summary>
        public bool OverCharging;

        /// <summary>Сверхзаряд только что включился.</summary>
        public bool OverChargeStarted;

        /// <summary>Сверхзаряд только что кончился.</summary>
        public bool OverChargeEnded;

        /// <summary>Кого игрок убил за бой.</summary>
        public readonly List<LogicKillEntry> Kills = new List<LogicKillEntry>();

        /// <summary>Прошлая позиция, поля +20 и +24. Пишет setPrevPos.</summary>
        public int PrevX;
        public int PrevY;

        /// <summary>Счёт, поле +28.</summary>
        public int Score;

        /// <summary>
        /// Счёт дружелюбия. Растёт от стрельбы по своим и используется, чтобы
        /// ловить сговор между командами.
        /// </summary>
        public int TeamingScore;

        /// <summary>Глобальный id управляемого персонажа. get/setControlledCharacterGID.</summary>
        public int ControlledCharacterGlobalId;

        /// <summary>Первое событие тика, поле +188.</summary>
        public int TickEventA;

        /// <summary>Второе событие тика, поле +204.</summary>
        public int TickEventB;

        /// <summary>Урон, нанесённый за текущий тик, поле +368.</summary>
        public int DamageDoneForTick;

        /// <summary>Поле +224. Когда именно игрок кого кого-то убил: пары «жертва и тик».</summary>
        public readonly List<LogicKillEntry> KillTicks = new List<LogicKillEntry>();

        /// <summary>Поле +472. Счёт игрока за бой.</summary>
        public readonly LogicPlayerStats Stats = new LogicPlayerStats();

        /// <summary>
        /// 0x1004f0cdc. Игрок кого-то убил.
        ///
        /// Оригинал держит два списка рядом — кого убили и сколько за это дали
        /// звёзд, — и дописывает в оба разом. Здесь это одна запись из двух полей.
        /// </summary>
        public void KilledPlayer(int victimPlayerIndex, int bountyStars)
        {
            Kills.Add(new LogicKillEntry(victimPlayerIndex, bountyStars));
        }

        /// <summary>0x1004f0d1c. Запоминает тик, на котором убийство случилось.</summary>
        public void AddKilledPlayerTicks(int victimPlayerIndex, int tick)
        {
            KillTicks.Add(new LogicKillEntry(victimPlayerIndex, tick));
        }

        public void AddScore(int amount)
        {
            Score += amount;
        }

        public void SetPrevPos(int x, int y)
        {
            PrevX = x;
            PrevY = y;
        }

        public void IncreaseDamageDoneForTick(int amount)
        {
            DamageDoneForTick += amount;
        }

        /// <summary>
        /// 0x1004f171c. Чистит ровно три поля — +188, +204 и +368. Ничего больше
        /// оригинал здесь не трогает, поэтому и мы не трогаем: счёт и прочее живут
        /// дольше одного тика.
        /// </summary>
        public void ResetEventsOnTick()
        {
            TickEventA = 0;
            TickEventB = 0;
            DamageDoneForTick = 0;
        }
    }
}
