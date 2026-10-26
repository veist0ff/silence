
using HashCore.Laser.Logic.Util;

public static class ByteStreamHelper
    {
        public static void WriteDataReference(ChecksumEncoder stream, LogicData data)
        {
            if (data == null)
            {
                stream.WriteVInt(0);
                return;
            }

            stream.WriteVInt(data.GetClassId());
            stream.WriteVInt(data.GetInstanceId());
        }

        public static void WriteDataReference(ChecksumEncoder stream, int globalId)
        {
            if (globalId <= 0)
            {
                stream.WriteVInt(0);
                return;
            }

            stream.WriteVInt(GlobalId.GetClassId(globalId));
            stream.WriteVInt(GlobalId.GetInstanceId(globalId));
        }

        public static void WriteDataReference(BitStream bitStream, int globalId)
        {
            // ByteStreamHelper::writeDataReference(VisionEncoder), v64.
            // The instance id is a bounded VInt, not a fixed 10-bit value.
            bitStream.WritePositiveIntMax127(GlobalId.GetClassId(globalId));
            if (globalId > 0)
                bitStream.WritePositiveVIntMax65535(GlobalId.GetInstanceId(globalId));
        }

        public static int ReadDataReference(ByteStream stream)
        {
            int classId = stream.ReadVInt();
            if (classId <= 0) return 0;
            int instanceId = stream.ReadVInt();

            return GlobalId.CreateGlobalId(classId, instanceId);
        }
        public static LogicLong DecodeLogicLong(ByteStream stream)
        {
            int high = stream.ReadVInt();
            int low = stream.ReadVInt();
            return new LogicLong(high, low);
        }
        public static void EncodeLogicLong(ByteStream stream,long a2)
        {
            stream.WriteVInt(a2.GetHigherInt());
            stream.WriteVInt(a2.GetLowerInt());
        }
        public static void WriteIntList(ChecksumEncoder stream, IEnumerable<int> list)
        {
            int[] array = list.ToArray();

            stream.WriteVInt(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                stream.WriteVInt(array[i]);
            }
        }
        public static List<int> ReadIntList(ByteStream stream, int Max=900000)
        {
            List<int> list = new List<int>();
            for(int i = stream.ReadVInt(); i > 0; i--)
            {
                list.Add(stream.ReadVInt());
            }
            return list;
        }
        
    }
