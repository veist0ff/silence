namespace HashCore.Laser.Logic.Command.Home
{
     
    using HashCore.Laser.Logic.Home;
    
    public class LogicPurchaseOfferCommand : Command
    {
        public int OfferIndex;
        public int DataGlobalId;
        public int Unknown;
        public int Currency;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            OfferIndex = stream.ReadVInt();
            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Unknown = ByteStreamHelper.ReadDataReference(stream);
            Currency = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (OfferIndex > -1) {
                if (!CanExecute(homeMode)) return -1;

                homeMode.Home.PurchaseOffer(OfferIndex);

            }
            else if (OfferIndex == -1) {
                homeMode.Home.PurchaseOfferWithCatalog(DataGlobalId, Currency);
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 519;
        }
    }
}
