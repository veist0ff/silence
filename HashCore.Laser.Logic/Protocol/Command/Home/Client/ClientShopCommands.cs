namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /*
     * Покупки, реклама и события. Форматы сняты с клиента; смотри
     * ClientSeenCommands.cs о том, почему незнакомый тип ломал весь ход.
     */

    /// <summary>
    /// Забрать ежедневную награду, тип 503. Два числа.
    /// </summary>
    public class LogicClaimDailyRewardCommand : Command
    {
        public int First;
        public int Second;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadVInt();
            Second = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 503;
        }
    }

    /// <summary>
    /// Покупка удвоителя монет, тип 509. Своих полей нет.
    /// </summary>
    public class LogicPurchaseDoubleCoinsCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 509;
        }
    }

    /// <summary>
    /// Отмена покупки предложения, тип 531. Два числа.
    /// </summary>
    public class LogicCancelPurchaseOfferCommand : Command
    {
        public int First;
        public int Second;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadVInt();
            Second = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 531;
        }
    }

    /// <summary>
    /// Покупка жизней в испытании, тип 540. Своих полей нет.
    /// </summary>
    public class LogicPurchaseChallengeLivesCommand : Command
    {
        /// <summary>Докупка жизней испытания, см. ChallengeManager.PurchaseLives.</summary>
        public override int Execute(HomeMode homeMode)
        {
            // Если проверка не прошла, клиент тоже ничего не сделал (execute
            // начинается с той же проверки), поэтому ошибку не возвращаем: иначе
            // HomeMode пошлёт рассинхрон на ровном месте.
            int result = HashCore.Laser.Logic.Home.Challenges.ChallengeManager.PurchaseLives(homeMode);
            if (result != 0) HashCore.Laser.Server.Logger.Print($"[challenge] докупка жизней отклонена, код {result}");
            return 0;
        }

        public override int GetCommandType()
        {
            return 540;
        }
    }

    /// <summary>
    /// Открыть ящик, тип 566. Ссылка на данные ящика.
    /// На сервере под этим номером стояла команда 500, которой у клиента нет вовсе
    /// (в реестре клиента такого случая нет), поэтому открытие не доходило.
    /// </summary>
    public class LogicGatchaOpenCommand : Command
    {
        public int ContainerGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            ContainerGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 566;
        }
    }

    /// <summary>
    /// Забрать награду за славу, тип 578. Сырой int.
    /// </summary>
    public class LogicClaimFameRewardCommand : Command
    {
        public int Index;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Index = stream.ReadInt();
        }

        /// <summary>
        /// Как LogicClaimFameRewardCommand::execute/canExecute (0x1003c4bd0/0x1003c4c48):
        /// славы должно хватать, награда — вещь уровня этой славы, уже открытая не
        /// выдаётся повторно. Раньше сервер ничего не делал, клиент же добавлял вещь
        /// себе — и игрока выбивало рассинхроном.
        /// </summary>
        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Avatar == null || homeMode.Home == null) return 1;
            if (homeMode.Avatar.FameCredits < Index) return 1;

            int globalId = HashCore.Laser.Logic.Home.Fame.FameTiers.GetRewardGlobalId(Index);
            if (globalId == 0) return 0;

            ClientHome home = homeMode.Home;
            List<int> target = (globalId / 1000000) switch
            {
                28 => home.UnlockedThumbnails ??= new List<int>(),
                52 => home.UnlockedEmotes ??= new List<int>(),
                85 => home.UnlockedFrames ??= new List<int>(),
                _ => null,
            };
            if (target == null) return 0;
            if (target.Contains(globalId)) return 2;

            target.Add(globalId);
            return 0;
        }

        public override int GetCommandType()
        {
            return 578;
        }
    }

    /// <summary>
    /// Потратить жетоны особого события, тип 579. Своих полей нет.
    /// </summary>
    public class LogicUseSpecialEventTokensCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 579;
        }
    }

    /// <summary>
    /// Обложка предложения снята, тип 580. Булево и число.
    /// </summary>
    public class LogicSetCoverOfferCommand : Command
    {
        public bool Opened;
        public int OfferId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Opened = stream.ReadBoolean();
            OfferId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 580;
        }
    }

    /// <summary>
    /// Забрать очки коллаборации, тип 582. Два числа.
    /// </summary>
    public class LogicClaimCollabPointsCommand : Command
    {
        public int First;
        public int Second;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadVInt();
            Second = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 582;
        }
    }

    /// <summary>
    /// Забрать награды состязания, тип 586. Одно число.
    /// </summary>
    public class LogicClaimContestRewardsCommand : Command
    {
        public int Index;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Index = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 586;
        }
    }

    /// <summary>
    /// Забрать билеты состязания, тип 588. Своих полей нет.
    /// </summary>
    public class LogicContestClaimTicketsCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 588;
        }
    }

    /// <summary>
    /// Перейти в более активный клуб, тип 589. Четыре числа.
    /// </summary>
    public class LogicJoinMoreActiveClubActionCommand : Command
    {
        public int First;
        public int Second;
        public int Third;
        public int Fourth;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadVInt();
            Second = stream.ReadVInt();
            Third = stream.ReadVInt();
            Fourth = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 589;
        }
    }

    /// <summary>
    /// Покупка прогресса рангового пропуска, тип 593. Одно число.
    /// </summary>
    public class LogicPurchaseCompetitivePassProgressCommand : Command
    {
        public int Amount;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Amount = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 593;
        }
    }

    /// <summary>
    /// Отмена покупки постоянного набора, тип 607. Ссылка на данные.
    /// </summary>
    public class LogicCancelShopPermanentPackPurchaseCommand : Command
    {
        public int PackGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PackGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 607;
        }
    }

    /// <summary>
    /// Начало покупки рангового пропуска, тип 618. Ссылка на данные.
    /// </summary>
    public class LogicStartCompetitivePassPurchaseCommand : Command
    {
        public int PassGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PassGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 618;
        }
    }

    /// <summary>
    /// Забрать отложенные награды коллаборации, тип 628.
    /// Число, булево, ссылка на данные, число, булево.
    /// </summary>
    public class LogicClaimPendingCollabEventRewardsCommand : Command
    {
        public int First;
        public bool Second;
        public int DataGlobalId;
        public int Third;
        public bool Fourth;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadVInt();
            Second = stream.ReadBoolean();
            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Third = stream.ReadVInt();
            Fourth = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 628;
        }
    }

    /// <summary>
    /// Начало покупки боевого пропуска, тип 676. Ссылка на данные.
    /// </summary>
    public class LogicStartBrawlPassPurchaseCommand : Command
    {
        public int PassGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PassGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 676;
        }
    }

    /// <summary>
    /// Начало покупки набора кристаллов, тип 773. Ссылка на данные.
    /// </summary>
    public class LogicStartGemPackPurchaseCommand : Command
    {
        public int PackGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            PackGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 773;
        }
    }

    /// <summary>
    /// Забрать награду из ящика чемпионата, тип 551. Ссылка на данные.
    /// </summary>
    public class LogicClaimWCPPDropCommand : Command
    {
        public int DropGlobalId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            DropGlobalId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 551;
        }
    }

    /// <summary>
    /// Показ рекламы начался, тип 572. Сырой int.
    /// </summary>
    public class LogicVideoAdStartCommand : Command
    {
        public int AdType;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            AdType = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 572;
        }
    }

    /// <summary>
    /// Показ рекламы завершён, тип 573. Сырой int.
    /// </summary>
    public class LogicVideoAdFinishCommand : Command
    {
        public int AdType;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            AdType = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 573;
        }
    }

    /// <summary>
    /// Учёт показа рекламы, тип 574. Сырой int.
    /// </summary>
    public class LogicVideoAdTrackingCommand : Command
    {
        public int AdType;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            AdType = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 574;
        }
    }

    /// <summary>
    /// Реклама не досмотрена, тип 575. Два сырых int.
    /// </summary>
    public class LogicVideoAdNotFinishedCommand : Command
    {
        public int AdType;
        public int Reason;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            AdType = stream.ReadInt();
            Reason = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 575;
        }
    }

    /// <summary>
    /// Действие по рекламе, тип 581. Три сырых int.
    /// </summary>
    public class LogicVideoAdActionCommand : Command
    {
        public int First;
        public int Second;
        public int Third;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            First = stream.ReadInt();
            Second = stream.ReadInt();
            Third = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 581;
        }
    }

}
