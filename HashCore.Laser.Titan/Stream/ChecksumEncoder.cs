
    using HashCore.Laser.Server;

    using System.Runtime.CompilerServices;
    using System.Text;

    public class ChecksumEncoder
    {
        private uint _checksum;
        private uint _snapshotChecksum;

        private bool _enabled;

        public ChecksumEncoder()
        {
            this._enabled = true;
        }

        public void EnableCheckSum(bool enable)
        {
            if (!this._enabled || enable)
            {
                if (!this._enabled && enable)
                {
                    this._checksum = this._snapshotChecksum;
                }

                this._enabled = enable;
            }
            else
            {
                this._snapshotChecksum = this._checksum;
                this._enabled = false;
            }
        }

        public void ResetCheckSum()
        {
            this._checksum = 0;
        }

        public virtual bool WriteBoolean(bool value)
        {
            this._checksum = (uint)((value ? 13 : 7) + this.RotateRight(this._checksum, 31));
            return value;
        }

        public virtual void WriteByte(byte value)
        {
            this._checksum = (uint)(value + this.RotateRight(this._checksum, 31) + 11);
        }

        public virtual void WriteShort(short value)
        {
            this._checksum = (uint)(value + this.RotateRight(this._checksum, 31) + 19);
        }

        public virtual void WriteInt(int value)
        {
            this._checksum = (uint)(value + this.RotateRight(this._checksum, 31) + 9);
        }

        public virtual void WriteVInt(int value)
        {
            this._checksum = (uint)(value + this.RotateRight(this._checksum, 31) + 33);
        }

        public virtual void WriteLong(LogicLong value)
        {
            value.Encode(this);
        }

        public virtual void WriteBytes(byte[] value, int length)
        {
            this._checksum = (uint)(((value != null ? length + 28 : 27) + (this._checksum >> 31)) | (this._checksum << (32 - 31)));
        }

        public virtual void WriteString(string value)
        {
            this._checksum = (uint)((value != null ? value.Length + 28 : 27) + this.RotateRight(this._checksum, 31));
        }

        public virtual void WriteStringReference(string value)
        {
            this._checksum = (uint)(value.Length + this.RotateRight(this._checksum, 31) + 38);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint RotateRight(uint value, int count)
        {
            return ((value >> count) | (value << (32 - count)));
        }

        public void WriteVLong(LogicLong value)
        {
            WriteVInt(value.GetHigherInt());
            WriteVInt(value.GetLowerInt());
        }

        public virtual int HashCode()
        {
            Logger.Error("ChecksumEncoder hashCode not designed");
            return 42;
        }

        public bool IsCheckSumEnabled()
        {
            return this._enabled;
        }

        public virtual bool IsCheckSumOnlyMode()
        {
            return true;
        }

        public bool Equals(ChecksumEncoder encoder)
        {
            if (encoder != null)
            {
                uint checksum = encoder._checksum;
                uint checksum2 = this._checksum;

                if (!encoder._enabled)
                {
                    checksum = encoder._snapshotChecksum;
                }

                if (!this._enabled)
                {
                    checksum2 = this._snapshotChecksum;
                }

                return checksum == checksum2;
            }

            return false;
        }

        public int GetCheckSum()
        {
            return (int)this._checksum;
        }
    }
