using System.Runtime.InteropServices;

namespace DereTore.Exchange.Audio.HCA.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AthHeader
    {
        public uint ATH
        {
            get => _ath;
            set => _ath = value;
        }

        public ushort Type
        {
            get => _type;
            set => _type = value;
        }

        private uint _ath;
        private ushort _type;
    }
}