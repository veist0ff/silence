using static System.Net.Mime.MediaTypeNames;
namespace HashCore.Laser.Logic.Home.Items
{

    public class OfferBundle
    {
        public List<LogicGemOffer> Items;
        public int Currency;
        public int Cost;
        public bool IsDailyDeals;
        public bool Purchased;
        public DateTime EndTime;
        public int OldCost;
        public string Title;
        public string BackgroundExportName;
        public string Claim;
        /// <summary>
        /// Метка «новое» (+60 у LogicOfferBundle): 0 новое, 1 просмотрено, 2 снято.
        /// Поле было, но в поток не попадало — клиент всегда получал ноль и рисовал
        /// «новое» заново после каждого перезахода.
        /// </summary>
        public int State;

        /// <summary>
        /// Идентификатор предложения (+72). По нему клиент адресует предложение в
        /// команде 515; при нуле у всех бандлов метка снималась не с того.
        /// </summary>
        public int OfferId;

        public int IsTID;
        public bool IsTrue;
        public int OfferType; // 1 - daily offer 2 - jackpot
        public bool OneTimeOffer;
        public bool LoadOnStartup;
        public bool Processed;
        public int TypeBenefit;
        public int Benefit;

        public int ShopPanelLayoutClass;
        public int ShopPanelLayoutType;

        public int ShopStyleSetClass;
        public int ShopStyleSetType;

        /// <summary>
        /// Контекст витрины (+176 в LogicOfferBundle). Shop::getBundleShopItemContext
        /// переводит его в номер вкладки: 7 -> 3, то есть магазин внутри события
        /// (NightMarketShopPopup), 6 -> 2, остальное -> обычный магазин.
        /// NightMarketShopPopup::collectValidBundles берёт только контекст 3, поэтому
        /// без семёрки предложение в витрине события не появится.
        /// </summary>
        public int ShopItemContext;

        /// <summary>
        /// Обложка предложения (+220 в LogicOfferBundle), список OfferCoverModes
        /// из enumerated_id_lists.csv: 0 — без обложки, 1 cover_gift_red,
        /// 2 cover_gift_gold, 3 cover_sb, 4 cover_sb_gold, 5 cover_sb_free,
        /// 6 cover_sb_epic. NightMarketBgHandler::onCreate при ненулевом значении
        /// накрывает товар клипом обложки, и до нажатия виден только он —
        /// раньше сюда безусловно уходила двойка, отсюда и «пустые» товары.
        /// </summary>
        public int CoverId;

        /// <summary>
        /// Номер звена в цепочке акций (+136 у LogicOfferBundle).
        /// LogicDailyData::getSortedChainOfferBundles (0x1004c2cdc) сортирует
        /// звенья цепочки именно по нему, по возрастанию.
        /// </summary>
        public int ChainOrder;

        /// <summary>
        /// Требование цепочки (+224 у LogicOfferBundle).
        /// LogicPurchaseOfferCommand::validateChainOfferBundlePurchaseRequirement
        /// (0x1003cdb78): 1 — забрать нельзя вовсе (код 16), 2 — требование снято,
        /// 0 — обычная цепочка: звено доступно, только когда предыдущее (по
        /// ChainOrder на единицу меньше) уже забрано. Для не-цепочек тут 2.
        /// </summary>
        public int ChainRequirement = 2;

        public OfferBundle()
        {
            Items = new List<LogicGemOffer>();
            State = 0;
        }

        public void Encode(ByteStream Stream)
        {
            Stream.WriteVInt(Items.Count);  // RewardCount
            foreach (LogicGemOffer gemOffer in Items)
            {
                gemOffer.Encode(Stream);
            }

            Stream.WriteVInt(Items.Count);  // RewardCount
            foreach (LogicGemOffer gemOffer in Items)
            {
                gemOffer.Encode(Stream);
            }
            Stream.WriteVInt(Currency);// Currency
            Stream.WriteVInt(Cost);// cost
            Stream.WriteVInt((int)(EndTime - DateTime.UtcNow).TotalSeconds);//
            Stream.WriteVInt(0);//
            Stream.WriteVInt(0);//
            Stream.WriteVInt(State); // +60 метка «новое»
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0);
            Stream.WriteVInt(OfferId); // +72 адрес предложения для команды 515
            Stream.WriteBoolean(true); //# Daily Offer
            Stream.WriteVInt(0); //# Old price
            Stream.WriteVInt(OldCost);
            Stream.WriteString(Title);// # Text
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            // Фон у всех акций, включая цепочку (chain offer): bgr_st.
            Stream.WriteString(Claim != null && Claim.StartsWith("gigi_early_") ? "offer_bgr_gigi" : "bgr_st");// # Background (у акций Гиги — фон темы Гиги)
            Stream.WriteVInt(-1);//
            Stream.WriteBoolean(Processed);// # This purchase is already being processed
            Stream.WriteVInt(TypeBenefit);// # Type Benefit
            Stream.WriteVInt(Benefit);// # Benefit
            Stream.WriteString("");//
            Stream.WriteBoolean(OneTimeOffer);//// # One time offer
            Stream.WriteBoolean(false);// # Claimed
            if (ShopPanelLayoutClass == 0 && ShopPanelLayoutType == 0)
            {
                Stream.WriteDataReference(0); // shop panel layouts 
            }
            else
            {
                Stream.WriteDataReference(ShopPanelLayoutClass, ShopPanelLayoutType); // shop panel layouts 
            }

            if (ShopStyleSetClass == 0 && ShopStyleSetType == 0)
            {
                Stream.WriteDataReference(0); // shop panel layouts 
            }
            else
            {
                Stream.WriteDataReference(ShopStyleSetClass, ShopStyleSetType); // shop panel layouts 
            }
            Stream.WriteDataReference(96, 1);//
            Stream.WriteBoolean(false);//
            Stream.WriteBoolean(false);//
            Stream.WriteBoolean(false);//
            Stream.WriteVInt(ShopItemContext);// +176 витрина: 7 = магазин события
            Stream.WriteVInt(0);//
            Stream.WriteVInt(Cost);//
            Stream.WriteBoolean(false);//
            Stream.WriteBoolean(false);
            Stream.WriteVInt(1);//asd
            Stream.WriteVInt(1);//
            Stream.WriteBoolean(false);//
            Stream.WriteVInt(ChainOrder);// +136 номер звена в цепочке акций
            Stream.WriteVInt(CoverId);// +220 обложка
            Stream.WriteVInt(ChainRequirement);// +224 требование цепочки
            Stream.WriteVInt(2);//
            Stream.WriteVInt(2);//
            Stream.WriteBoolean(false);//
            Stream.WriteBoolean(false); ;////
            // +238. LogicPurchaseOfferCommand::canExecuteInternal: для витрины события
            // (+176 == 7) покупка отклоняется с кодом 19, пока здесь не true.
            Stream.WriteBoolean(ShopItemContext == 7);//
            Stream.WriteVInt(2);//
            Stream.WriteDataReference(96, 1);//#v64
            Stream.WriteVInt(2);//) #v64
            Stream.WriteBoolean(false);// #v64
        }
    }
}
