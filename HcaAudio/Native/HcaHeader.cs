﻿using System.Runtime.InteropServices;

namespace DereTore.Exchange.Audio.HCA.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HcaHeader
    {
        public uint HCA
        {
            get => _hca;
            set => _hca = value;
        }

        public ushort Version
        {
            get => _version;
            set => _version = value;
        }

        public ushort DataOffset
        {
            get => _dataOffset;
            set => _dataOffset = value;
        }

        private uint _hca;
        private ushort _version;
        private ushort _dataOffset;
    }
}