namespace HashCore.Laser.Titan.Library.Blake
{
    #region Usings

    using System;

    #endregion Usings

    public class Blake2BHasher : Blake2BBase
    {
        private readonly Blake2BConfig _Config = new Blake2BConfig();
        private readonly Blake2BCore _Core = new Blake2BCore();
        private readonly byte[] _Key;
        private readonly int _OutputSize;
        private readonly ulong[] _RawConfig;

        public Blake2BHasher()
        {
            this._RawConfig = Blake2Builder.ConfigB(this._Config, null);

            if (this._Config.Key != null && this._Config.Key.Length != 0)
            {
                this._Key = new byte[24];
                Array.Copy(this._Config.Key, this._Key, this._Config.Key.Length);
            }

            this._OutputSize = this._Config.OutputSize;

            this.Init();
        }

        public override byte[] Finish()
        {
            byte[] _FResult = this._Core.HashFinal();

            if (this._OutputSize != _FResult.Length)
            {
                byte[] _Result = new byte[this._OutputSize];
                Array.Copy(_FResult, _Result, _Result.Length);
                return _Result;
            }

            return _FResult;
        }

        public override sealed void Init()
        {
            this._Core.Initialize(this._RawConfig);

            if (this._Key != null)
            {
                this._Core.HashCore(this._Key, 0, this._Key.Length);
            }
        }

        public override void Update(byte[] _Data, int _Index, int _Count)
        {
            this._Core.HashCore(_Data, _Index, _Count);
        }
    }
}
