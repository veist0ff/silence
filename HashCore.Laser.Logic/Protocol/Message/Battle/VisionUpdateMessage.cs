namespace HashCore.Laser.Logic.Message.Battle
{
     
    public class VisionUpdateMessage : GameMessage
    {
        /// <summary>
        /// Больше семи кусков клиент собрать не может: номер куска он достаёт
        /// как `descriptor & 7` (MessageManager::combineVisionUpdates 0x10023d814)
        /// и кладёт кадр в слот `номер - 1`, так что номера идут с единицы.
        /// </summary>
        public const int MaxFragments = 7;

        public int Tick;
        public int HandledInputs;
        public int Viewers;
        public BitStream VisionBitStream;

        /// <summary>Кадр разрезан на куски и клиент должен склеить его сам.</summary>
        public bool IsFragment;

        /// <summary>
        /// Описатель куска: `всего кусков * 8 + номер куска`, номер с единицы.
        /// Клиент копит куски с одинаковым тиком в своём списке и, когда все
        /// слоты заполнены, склеивает их битстримы в том же порядке.
        /// </summary>
        public int FragmentDescriptor;

        public VisionUpdateMessage() : base()
        {
            ;
        }

        /// <summary>Описатель для куска: номера идут с единицы.</summary>
        public static int MakeFragmentDescriptor(int totalFragments, int oneBasedIndex)
        {
            return totalFragments * 8 + oneBasedIndex;
        }

        public override void Encode()
        {
            Stream.WriteVInt(Tick);
            Stream.WriteVInt(HandledInputs);
            Stream.WriteVInt(0); // unknown
            Stream.WriteVInt(Viewers);
            Stream.WriteBoolean(false);

            // +169 в клиенте: признак куска. За ним идёт описатель +172.
            Stream.WriteBoolean(IsFragment);
            if (IsFragment) Stream.WriteVInt(FragmentDescriptor);

            Stream.WriteBoolean(false); // optional timing pair at +176/+180

            Stream.WriteBytes(VisionBitStream.GetByteArray(), VisionBitStream.GetLength());
        }

        public override int GetMessageType()
        {
            return 24109;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
