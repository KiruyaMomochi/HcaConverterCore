using System.Runtime.InteropServices;

namespace DereTore.Exchange.Audio.HCA.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct CommentHeader
    {
        public uint COMM
        {
            get => _comm;
            set => _comm = value;
        }

        public byte Length
        {
            get => _length;
            set => _length = value;
        }

        private uint _comm;
        private byte _length;
    }
}