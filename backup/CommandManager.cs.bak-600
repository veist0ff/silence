using System;
using System.Collections.Generic;

namespace HashCore.Laser.Logic.Command
{
    using HashCore.Laser.Logic.Command.Avatar;
    using HashCore.Laser.Logic.Command.Home;
    using HashCore.Laser.Server;

    public class CommandManager
    {
        private static Dictionary<int, Type> CommandTypes;

        static CommandManager()
        {
            CommandTypes = new Dictionary<int, Type>()
            {
                {500, typeof(LogicGatchaCommand)},
                {505, typeof(LogicSetPlayerThumbnailCommand)},
                {506, typeof(LogicSelectSkinCommand)},
                {514, typeof(LogicDeleteNotificationCommand)},
                {515, typeof(LogicClearShopTickersCommand)},
                {517, typeof(LogicClaimRankUpRewardCommand)},
                {519, typeof(LogicPurchaseOfferCommand)},
                {520, typeof(LogicLevelUpCommand)},
                {521, typeof(LogicPurchaseHeroLvlUpMaterialCommand)},
                {522, typeof(LogicHeroSeenCommand) },
                {525, typeof(LogicSelectCharacterCommand)},
                {527, typeof(LogicSetPlayerNameColorCommand)},
                {529, typeof(LogicSelectStarPowerCommand)},
                // Просмотр заданий: клиент шлёт 877, а не 533 — под 533 он не шлёт ничего.
                {877, typeof(LogicQuestsSeenCommand)},
                {526, typeof(LogicUnlockFreeSkinsCommand)},
                {534, typeof(LogicPurchaseBrawlPassCommand)},
                {535, typeof(LogicClaimTailRewardCommand)},
                {536, typeof(LogicPurchaseBrawlPassProgressCommand)},
                {538, typeof(LogicSelectEmoteCommand)},
                {543, typeof(LogicSelectGearCommand)},
                {567, typeof(LogicEditBattlePassCommand1)},
                {568, typeof(LogicEditBattlePassCommand)},
                {569, typeof(LogicClaimMasteriesCommand)},
                {570, typeof(LogicSelectFavouriteBrawlerCommand)},
                {576, typeof(LogicRankedSeenCommand)},
                {949, typeof(LogicIncludeHeroInFavouritesListCommand)},
                // Echoed back by the client, so both have to be decodable.
                {227, typeof(LogicBrawlerRecruitRoadChangedCommand)},
                {228, typeof(LogicRefreshRandomRewardsCommand)},
                {226, typeof(LogicLoginCalendarChangedCommand)},
                {203, typeof(LogicGiveDeliveryItemsCommand)},
                // Random rewards (LogicRandomRewardManager): Starr Drops and boxes.
                {571, typeof(LogicClaimRandomRewardCommand)},
                // Login calendar (LogicLoginCalendar).
                {550, typeof(LogicClaimLoginCalendarRewardCommand)},
                {556, typeof(LogicSetLoginCalendarStateCommand)},
                // Daily streak popup buttons.
                {595, typeof(LogicKeepDailyStreakCommand)},
                {596, typeof(LogicResetDailyStreakCommand)},
                // Recruit road (LogicBrawlerRecruitRoad).
                {559, typeof(LogicRecruitRoadSelectBrawlerCommand)},
                {560, typeof(LogicPurchaseBrawlerCommand)},
                {561, typeof(LogicRecruitRoadSwitchBrawlerCommand)},
                {562, typeof(LogicRecruitRoadClaimBrawlerCommand)},
                {563, typeof(LogicRecruitRoadProcessExtraRecruitTokensCommand)},
                {564, typeof(LogicRecruitRoadSelectNewBrawlerCommand)},
                {565, typeof(LogicRecruitRoadAbandonNewBrawlerCommand)},
                {664, typeof(LogicClaimTrophyWorldRewardCommand)},
                // Дорожка ежедневных побед: клиент отмечает, что её посмотрели.
                {750, typeof(LogicDailyWinTrackSeenCommand)},
                // Покупка гаджета, пассивки или гипера и покупка гира. Обе команды
                // клиент шлёт давно, а сервер их не знал — покупки не сохранялись.
                {557, typeof(LogicPurchaseGadgetOrStarPowerCommand)},
                {558, typeof(LogicPurchaseGearCommand)},
                // «Посмотрел эту запись данных». Без неё всё новое оставалось новым.
                {583, typeof(LogicSetDataSeenStateCommand)},
                // Ранговый пропуск: ступень и хвост. Награды берутся из
                // competitive_pass_tiers.csv и выдаются как в трофейной дороге.
                {591, typeof(LogicClaimCompetitivePassRewardCommand)},
                {594, typeof(LogicClaimCompetitivePassTailCommand)},

                // Всё, что ниже, клиент шлёт давно, а сервер не знал ни одного
                // из этих типов. Любой незнакомый тип обрывает разбор ВСЕГО хода
                // (см. DecodeCommand), поэтому вместе с ними пропадали и соседние
                // команды пакета — отсюда и расхождения состояний.
                // Форматы сняты с реестра клиента LogicCommandFactory::createCommandManual
                // (0x1003bb604) и с decode каждого класса.
                {503, typeof(LogicClaimDailyRewardCommand)},
                {508, typeof(LogicChangeControlModeCommand)},
                {509, typeof(LogicPurchaseDoubleCoinsCommand)},
                {511, typeof(LogicEmptyClientCommand)},
                {512, typeof(LogicToggleInGameHintsCommand)},
                {528, typeof(LogicViewInboxNotificationCommand)},
                {530, typeof(LogicSetPlayerAgeCommand)},
                {531, typeof(LogicCancelPurchaseOfferCommand)},
                {532, typeof(LogicItemSeenCommand)},
                {539, typeof(LogicBrawlPassAutoCollectWarningSeenCommand)},
                {540, typeof(LogicPurchaseChallengeLivesCommand)},
                {541, typeof(LogicClearESportsHubNotificationCommand)},
                {544, typeof(LogicSetConfFlag21Command)},
                {545, typeof(LogicNextRandomSkinCommand)},
                {547, typeof(LogicRandomSkinModeCommand)},
                {548, typeof(LogicSetSkinUISeenCommand)},
                {549, typeof(LogicSetConfFlag24Command)},
                {551, typeof(LogicClaimWCPPDropCommand)},
                {552, typeof(LogicChangeAvatarPrivacyPreferencesCommand)},
                {553, typeof(LogicUpdateAdvisedPrivacyPreferencesCommand)},
                {554, typeof(LogicRerollQuestCommand)},
                {555, typeof(LogicSelectSprayCommand)},
                // Открытие ящика. На сервере оно висело под номером 500, которого
                // у клиента нет вовсе — в его реестре такого случая просто нет.
                {566, typeof(LogicGatchaOpenCommand)},
                {572, typeof(LogicVideoAdStartCommand)},
                {573, typeof(LogicVideoAdFinishCommand)},
                {574, typeof(LogicVideoAdTrackingCommand)},
                {575, typeof(LogicVideoAdNotFinishedCommand)},
                {577, typeof(LogicSetPlayerFrameCommand)},
                {578, typeof(LogicClaimFameRewardCommand)},
                {579, typeof(LogicUseSpecialEventTokensCommand)},
                {580, typeof(LogicSetCoverOfferCommand)},
                {581, typeof(LogicVideoAdActionCommand)},
                {582, typeof(LogicClaimCollabPointsCommand)},
                {584, typeof(LogicCollabSideSeenCommand)},
                {586, typeof(LogicClaimContestRewardsCommand)},
                {587, typeof(LogicJoinClubEventSeenCommand)},
                {588, typeof(LogicContestClaimTicketsCommand)},
                {589, typeof(LogicJoinMoreActiveClubActionCommand)},
                {590, typeof(LogicAlliancePiggyBankTicketsSeenCommand)},
                {592, typeof(LogicSetUnknownAgeCommand)},
                {593, typeof(LogicPurchaseCompetitivePassProgressCommand)},
                {597, typeof(LogicSelectAvatarPassiveCommand)},
                {598, typeof(LogicUnselectAvatarPassiveCommand)},
                {599, typeof(LogicSelectBonusSkillCommand)},
                {600, typeof(LogicCollabTrickOrTreatCommand)},
                {601, typeof(LogicActivateSpecialRushModeCommand)},
                {607, typeof(LogicCancelShopPermanentPackPurchaseCommand)},
                {618, typeof(LogicStartCompetitivePassPurchaseCommand)},
                {628, typeof(LogicClaimPendingCollabEventRewardsCommand)},
                {676, typeof(LogicStartBrawlPassPurchaseCommand)},
                {752, typeof(LogicRecordsActionCommand)},
                {773, typeof(LogicStartGemPackPurchaseCommand)},
                {979, typeof(LogicSwitchMegaQuestCommand)},
                {1946, typeof(LogicDebugCommand)}
            };
        }

        public static Command DecodeCommand(ByteStream stream)
        {
            int type = stream.ReadVInt();
            Command command = CommandManager.CreateCommand(type);
            if (command == null)
            {
                Logger.Warning($"[command::execute] command {type} not found " );
                return null;
            }
            Logger.Print($"[command::execute] command {type} processed " );
            command.Decode(stream);
            return command;
        }

        public static Command CreateCommand(int type)
        {
            if (CommandTypes.ContainsKey(type))
            {
                return (Command)Activator.CreateInstance(CommandTypes[type]);
            }
            return null;
        }
    }
}
