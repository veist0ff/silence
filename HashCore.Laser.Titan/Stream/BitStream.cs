
    /// <summary>
    /// Битовый поток кадра боя. Каждая функция — зеркало одноимённой из клиента v64
    /// (BitStream::write* / read*), включая ОБРЕЗАНИЕ значений.
    ///
    /// Это принципиально. Клиент никогда не пишет число, которое не влезает в своё
    /// поле: writePositiveInt зажимает его в [0, 2^bits − 1], writeInt — в
    /// [−(2^bits − 1), 2^bits − 1], writePositiveVInt — в [0, 2^(2^bits) − 1] и
    /// пишет 0 вместо отрицательного. Прежняя реализация этого не делала:
    /// отрицательное число в writePositiveVInt уходило как префикс «максимальная
    /// длина» и НОЛЬ бит значения — клиент дочитывал 16 бит из следующих полей, и
    /// весь остаток кадра разъезжался. Одного −1 в поле эффекта попадания хватало,
    /// чтобы персонажи не появлялись.
    ///
    /// Биты идут от младшего к старшему внутри байта (writeBits: value &amp; (1 &lt;&lt; i)
    /// в позицию bitOffset).
    /// </summary>
    public class BitStream
    {
        private byte[] buffer;
        private int offset;
        private int length;
        private int bitOffset;

        public BitStream(int length)
        {
            this.buffer = new byte[Math.Max(length, 1)];
            this.length = length;
        }

        public BitStream(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        /// <summary>Сколько байт уже занято записью (с неполным последним).</summary>
        public int GetWriteOffset()
        {
            return this.bitOffset > 0 ? this.offset + 1 : this.offset;
        }

        public int GetLength()
        {
            int written = GetWriteOffset();
            return this.length > written ? this.length : written;
        }

        /// <summary>Позиция чтения/записи в битах — для сверки кадров.</summary>
        public int GetBitPosition()
        {
            return this.offset * 8 + this.bitOffset;
        }

        public byte[] GetByteArray()
        {
            return this.buffer;
        }

        public void ResetOffset()
        {
            this.offset = 0;
            this.bitOffset = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            int need = this.offset + capacity + 1;
            if (need > this.buffer.Length)
            {
                byte[] tmp = new byte[Math.Max(need, this.buffer.Length * 2)];
                Array.Copy(this.buffer, 0, tmp, 0, this.buffer.Length);
                this.buffer = tmp;
            }
        }

        // ================================================================ запись

        /// <summary>BitStream::writeBits: младшие bits бит value, от младшего.</summary>
        /// <summary>Отладка: пока не null, каждая запись добавляется сюда (метод, биты, значение).</summary>
        public System.Collections.Generic.List<string> Trace;

        private void WriteBitsRaw(int value, int bits)
        {
            if (Trace != null)
            {
                var st = new System.Diagnostics.StackTrace(1, false);
                string m = "?";
                for (int i = 0; i < st.FrameCount; i++)
                {
                    var mb = st.GetFrame(i)?.GetMethod();
                    if (mb == null) continue;
                    if (mb.DeclaringType == typeof(BitStream)) { m = mb.Name; continue; }
                    break;
                }
                Trace.Add($"{m}:{bits}b={value}");
            }
            EnsureCapacity(bits / 8 + 2);
            if (this.offset == 0 && this.bitOffset == 0) this.buffer[0] = 0;
            for (int i = 0; i < bits; i++)
            {
                int bit = (value >> i) & 1;
                if (this.bitOffset == 0) this.buffer[this.offset] = 0;
                this.buffer[this.offset] |= (byte)(bit << this.bitOffset);
                this.bitOffset++;
                if (this.bitOffset == 8)
                {
                    this.offset++;
                    this.bitOffset = 0;
                    this.buffer[this.offset] = 0;
                }
            }
        }

        public void WriteBit(byte data)
        {
            WriteBitsRaw(data & 1, 1);
        }

        public void WriteBits(byte[] bits, long count)
        {
            for (long i = 0; i < count; i++)
                WriteBitsRaw((bits[i / 8] >> (int)(i % 8)) & 1, 1);
        }

        /// <summary>BitStream::writePositiveInt: зажим в [0, 2^bits − 1].</summary>
        public void WritePositiveInt(int value, int bits)
        {
            int max = bits >= 31 ? int.MaxValue : (1 << bits) - 1;
            int v = value <= 0 ? 0 : (value >= max ? max : value);
            WriteBitsRaw(v, bits);
        }

        public bool WriteBoolean(bool value)
        {
            WriteBitsRaw(value ? 1 : 0, 1);
            return value;
        }

        public void WritePositiveIntMax1(int value) => WritePositiveInt(value, 1);
        public void WritePositiveIntMax3(int value) => WritePositiveInt(value, 2);
        public void WritePositiveIntMax7(int value) => WritePositiveInt(value, 3);
        public void WritePositiveIntMax15(int value) => WritePositiveInt(value, 4);
        public void WritePositiveIntMax31(int value) => WritePositiveInt(value, 5);
        public void WritePositiveIntMax63(int value) => WritePositiveInt(value, 6);
        public void WritePositiveIntMax127(int value) => WritePositiveInt(value, 7);
        public void WritePositiveIntMax255(int value) => WritePositiveInt(value, 8);
        public void WritePositiveIntMax511(int value) => WritePositiveInt(value, 9);
        public void WritePositiveIntMax1023(int value) => WritePositiveInt(value, 10);
        public void WritePositiveIntMax2047(int value) => WritePositiveInt(value, 11);
        public void WritePositiveIntMax4095(int value) => WritePositiveInt(value, 12);
        public void WritePositiveIntMax8191(int value) => WritePositiveInt(value, 13);
        public void WritePositiveIntMax16383(int value) => WritePositiveInt(value, 14);
        public void WritePositiveIntMax32767(int value) => WritePositiveInt(value, 15);
        public void WritePositiveIntMax65535(int value) => WritePositiveInt(value, 16);
        public void WritePositiveIntMax131071(int value) => WritePositiveInt(value, 17);
        public void WritePositiveIntMax262143(int value) => WritePositiveInt(value, 18);
        public void WritePositiveIntMax524287(int value) => WritePositiveInt(value, 19);
        public void WritePositiveIntMax1048575(int value) => WritePositiveInt(value, 20);
        public void WritePositiveIntMax2097151(int value) => WritePositiveInt(value, 21);
        public void WritePositiveIntMax4194303(int value) => WritePositiveInt(value, 22);

        public int WritePositiveIntMax134217727(int value)
        {
            WritePositiveInt(value, 27);
            return value;
        }

        /// <summary>
        /// BitStream::writeInt: зажим в [−(2^bits − 1), 2^bits − 1], затем знаковый
        /// бит (1 — неотрицательное, 0 — отрицательное) и модуль в bits битах.
        /// </summary>
        public void WriteInt(int value, int bits)
        {
            int max = (1 << bits) - 1;
            int v = value >= max ? max : value;
            if (v <= -max) v = -max;
            if (v < 0)
            {
                WriteBitsRaw(0, 1);
                v = -v;
            }
            else
            {
                WriteBitsRaw(1, 1);
            }
            WriteBitsRaw(v, bits);
        }

        public void WriteIntMax1(int value) => WriteInt(value, 1);
        public void WriteIntMax3(int value) => WriteInt(value, 2);
        public void WriteIntMax7(int value) => WriteInt(value, 3);
        public void WriteIntMax15(int value) => WriteInt(value, 4);
        public void WriteIntMax31(int value) => WriteInt(value, 5);
        public void WriteIntMax63(int value) => WriteInt(value, 6);
        public void WriteIntMax127(int value) => WriteInt(value, 7);
        public void WriteIntMax255(int value) => WriteInt(value, 8);
        public void WriteIntMax511(int value) => WriteInt(value, 9);
        public void WriteIntMax1023(int value) => WriteInt(value, 10);
        public void WriteIntMax2047(int value) => WriteInt(value, 11);
        public void WriteIntMax4095(int value) => WriteInt(value, 12);
        public void WriteIntMax16383(int value) => WriteInt(value, 14);
        public void WriteIntMax32767(int value) => WriteInt(value, 15);
        public void WriteIntMax65535(int value) => WriteInt(value, 16);

        /// <summary>
        /// BitStream::writePositiveVInt: длина значения (не меньше 1) минус один в
        /// bits битах, затем само значение этой длины. Значение зажимается в
        /// [0, 2^min(2^bits, 31) − 1].
        /// </summary>
        public void WritePositiveVInt(int value, int bits)
        {
            int valueBits = 1 << bits;
            if (valueBits >= 31) valueBits = 31;
            int max = valueBits >= 31 ? int.MaxValue : (1 << valueBits) - 1;
            int v = value <= 0 ? 0 : (value >= max ? max : value);

            int len = 1;
            if (v != 0)
            {
                len = 0;
                for (int t = v; t != 0; t >>= 1) len++;
            }
            WriteBitsRaw(len - 1, bits);
            WriteBitsRaw(v, len);
        }

        public void WritePositiveVIntMax255(int value) => WritePositiveVInt(value, 3);
        public void WritePositiveVIntMax65535(int value) => WritePositiveVInt(value, 4);
        public void WritePositiveVIntMax2147483647(int value) => WritePositiveVInt(value, 5);

        /// <summary>Устаревшее имя: тот же 5-битный префикс, что у Max2147483647.</summary>
        public void WritePositiveVIntMax16777215(int value) => WritePositiveVInt(value, 5);

        /// <summary>«Часто ноль»: 1 — ноль, иначе 0 и VInt. Отрицательное — как ноль.</summary>
        public void WritePositiveVIntOftenZero(int value, int bits)
        {
            if (value <= 0)
            {
                WriteBitsRaw(1, 1);
                return;
            }
            WriteBitsRaw(0, 1);
            WritePositiveVInt(value, bits);
        }

        public void WritePositiveVIntMax255OftenZero(int value) => WritePositiveVIntOftenZero(value, 3);
        public void WritePositiveVIntMax65535OftenZero(int value) => WritePositiveVIntOftenZero(value, 4);
        public void WritePositiveVIntMax2147483647OftenZero(int value) => WritePositiveVIntOftenZero(value, 5);

        /// <summary>Сырой байт в текущую позицию (используется только в заголовках).</summary>
        public void WriteByte(byte value)
        {
            WriteBitsRaw(value, 8);
        }

        // ================================================================ чтение

        /// <summary>BitStream::readBits: bits бит, от младшего.</summary>
        public int ReadBits(int bits)
        {
            int result = 0;
            for (int i = 0; i < bits; i++)
            {
                int b = this.offset < this.buffer.Length ? (this.buffer[this.offset] >> this.bitOffset) & 1 : 0;
                result |= b << i;
                this.bitOffset++;
                if (this.bitOffset == 8)
                {
                    this.bitOffset = 0;
                    this.offset++;
                }
            }
            return result;
        }

        public byte ReadBit() => (byte)ReadBits(1);
        public bool ReadBoolean() => ReadBits(1) == 1;
        public int ReadPositiveInt(int bits) => ReadBits(bits);

        public int ReadPositiveIntMax3() => ReadBits(2);
        public int ReadPositiveIntMax7() => ReadBits(3);
        public int ReadPositiveIntMax15() => ReadBits(4);
        public int ReadPositiveIntMax31() => ReadBits(5);
        public int ReadPositiveIntMax63() => ReadBits(6);
        public int ReadPositiveIntMax127() => ReadBits(7);
        public int ReadPositiveIntMax255() => ReadBits(8);
        public int ReadPositiveIntMax511() => ReadBits(9);
        public int ReadPositiveIntMax1023() => ReadBits(10);
        public int ReadPositiveIntMax2047() => ReadBits(11);
        public int ReadPositiveIntMax4095() => ReadBits(12);
        public int ReadPositiveIntMax8191() => ReadBits(13);
        public int ReadPositiveIntMax16383() => ReadBits(14);
        public int ReadPositiveIntMax32767() => ReadBits(15);
        public int ReadPositiveIntMax65535() => ReadBits(16);
        public int ReadPositiveIntMax131071() => ReadBits(17);
        public int ReadPositiveIntMax2097151() => ReadBits(21);
        public int ReadPositiveIntMax134217727() => ReadBits(27);

        /// <summary>BitStream::readInt*: знак (1 → +, 0 → −), затем модуль.</summary>
        public int ReadInt(int bits)
        {
            int sign = 2 * ReadBits(1) - 1;
            return sign * ReadBits(bits);
        }

        public int ReadIntMax3() => ReadInt(2);
        public int ReadIntMax7() => ReadInt(3);
        public int ReadIntMax15() => ReadInt(4);
        public int ReadIntMax31() => ReadInt(5);
        public int ReadIntMax63() => ReadInt(6);
        public int ReadIntMax127() => ReadInt(7);
        public int ReadIntMax255() => ReadInt(8);
        public int ReadIntMax1023() => ReadInt(10);
        public int ReadIntMax4095() => ReadInt(12);
        public int ReadIntMax16383() => ReadInt(14);
        public int ReadIntMax32767() => ReadInt(15);
        public int ReadIntMax65535() => ReadInt(16);

        public int ReadPositiveVInt(int bits)
        {
            int len = ReadBits(bits) + 1;
            return ReadBits(len);
        }

        public int ReadPositiveVIntMax255() => ReadPositiveVInt(3);
        public int ReadPositiveVIntMax65535() => ReadPositiveVInt(4);
        public int ReadPositiveVIntMax2147483647() => ReadPositiveVInt(5);

        public int ReadPositiveVIntOftenZero(int bits)
        {
            if (ReadBits(1) == 1) return 0;
            return ReadPositiveVInt(bits);
        }

        public int ReadPositiveVIntMax255OftenZero() => ReadPositiveVIntOftenZero(3);
        public int ReadPositiveVIntMax65535OftenZero() => ReadPositiveVIntOftenZero(4);
        public int ReadPositiveVIntMax2147483647OftenZero() => ReadPositiveVIntOftenZero(5);

        public byte[] ReadBytes(int bitCount)
        {
            byte[] data = new byte[(bitCount + 7) / 8];
            for (int i = 0; i < bitCount; i++)
                data[i / 8] |= (byte)(ReadBits(1) << (i % 8));
            return data;
        }
    }
