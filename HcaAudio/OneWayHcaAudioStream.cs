﻿using System;
using System.IO;

namespace DereTore.Exchange.Audio.HCA
{
    public sealed class OneWayHcaAudioStream : HcaAudioStreamBase
    {
        public OneWayHcaAudioStream(Stream baseStream)
            : this(baseStream, DecodeParams.Default)
        {
        }

        public OneWayHcaAudioStream(Stream baseStream, bool outputWaveHeader)
            : this(baseStream, DecodeParams.Default, outputWaveHeader)
        {
        }

        public OneWayHcaAudioStream(Stream baseStream, DecodeParams decodeParams)
            : this(baseStream, decodeParams, true)
        {
        }

        public OneWayHcaAudioStream(Stream baseStream, DecodeParams decodeParams, bool outputWaveHeader)
            : base(baseStream, decodeParams)
        {
            _decoder = new OneWayHcaDecoder(baseStream, decodeParams);
            OutputWaveHeader = outputWaveHeader;
            _state = HcaAudioStreamDecodeState.Initialized;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!HasMoreData) return 0;
            var writeLengthLimit = Math.Min(count, buffer.Length);
            var outputWaveHeader = OutputWaveHeader;
            var decoder = _decoder as OneWayHcaDecoder;
            if (decoder == null) throw new NullReferenceException();
            while (true)
            {
                int maxToCopy;
                var state = _state;
                switch (state)
                {
                    case HcaAudioStreamDecodeState.Initialized:
                    {
                        if (!outputWaveHeader)
                        {
                            _state = HcaAudioStreamDecodeState.WaveHeaderTransmitted;
                            continue;
                        }

                        _waveHeaderSize = decoder.GetMinWaveHeaderBufferSize();
                        _waveHeaderBuffer = new byte[_waveHeaderSize];
                        _waveHeaderSizeLeft = _waveHeaderSize;
                        decoder.WriteWaveHeader(_waveHeaderBuffer);
                        maxToCopy = Math.Min(writeLengthLimit, _waveHeaderSizeLeft);
                        Array.Copy(_waveHeaderBuffer, _waveHeaderSize - _waveHeaderSizeLeft, buffer, offset, maxToCopy);
                        _waveHeaderSizeLeft -= maxToCopy;
                        _state = _waveHeaderSizeLeft <= 0
                            ? HcaAudioStreamDecodeState.WaveHeaderTransmitted
                            : HcaAudioStreamDecodeState.WaveHeaderTransmitting;
                        return maxToCopy;
                    }
                    case HcaAudioStreamDecodeState.WaveHeaderTransmitting:
                    {
                        if (!outputWaveHeader)
                        {
                            _state = HcaAudioStreamDecodeState.WaveHeaderTransmitted;
                            continue;
                        }

                        maxToCopy = Math.Min(writeLengthLimit, _waveHeaderSizeLeft);
                        Array.Copy(_waveHeaderBuffer, _waveHeaderSize - _waveHeaderSizeLeft, buffer, offset, maxToCopy);
                        _waveHeaderSizeLeft -= maxToCopy;
                        if (_waveHeaderSizeLeft <= 0) _state = HcaAudioStreamDecodeState.WaveHeaderTransmitted;
                        return maxToCopy;
                    }
                    case HcaAudioStreamDecodeState.WaveHeaderTransmitted:
                    {
                        _standardWaveDataSize = decoder.GetMinWaveDataBufferSize();
                        _waveDataSize = _standardWaveDataSize * BlockBatchSize;
                        _waveDataBuffer = new byte[_waveDataSize];
                        var decodedBlockCount = decoder.DecodeBlocks(_waveDataBuffer);
                        _waveDataSize = (int) (decodedBlockCount * decoder.GetMinWaveDataBufferSize());
                        _waveDataSizeLeft = _waveDataSize;
                        maxToCopy = Math.Min(writeLengthLimit, _waveDataSizeLeft);
                        Array.Copy(_waveDataBuffer, _waveDataSize - _waveDataSizeLeft, buffer, offset, maxToCopy);
                        _waveDataSizeLeft -= maxToCopy;
                        if (_waveDataSizeLeft <= 0)
                            _state = decoder.HasMore
                                ? HcaAudioStreamDecodeState.DataTransmitting
                                : HcaAudioStreamDecodeState.WaveHeaderTransmitted;
                        else
                            _state = HcaAudioStreamDecodeState.DataTransmitting;
                        return maxToCopy;
                    }
                    case HcaAudioStreamDecodeState.DataTransmitting:
                    {
                        if (_waveDataSizeLeft <= 0)
                        {
                            _waveDataSize = _standardWaveDataSize * BlockBatchSize;
                            _waveDataBuffer = new byte[_waveDataSize];
                            var decodedBlockCount = decoder.DecodeBlocks(_waveDataBuffer);
                            if (decodedBlockCount > 0 || decoder.HasMore)
                            {
                                _waveDataSize = (int) (decodedBlockCount * decoder.GetMinWaveDataBufferSize());
                                _waveDataSizeLeft = _waveDataSize;
                                _state = HcaAudioStreamDecodeState.DataTransmitting;
                            }
                            else
                            {
                                _state = HcaAudioStreamDecodeState.WaveHeaderTransmitted;
                                continue;
                            }
                        }

                        maxToCopy = Math.Min(writeLengthLimit, _waveDataSizeLeft);
                        Array.Copy(_waveDataBuffer, _waveDataSize - _waveDataSizeLeft, buffer, offset, maxToCopy);
                        _waveDataSizeLeft -= maxToCopy;
                        return maxToCopy;
                    }
                    case HcaAudioStreamDecodeState.DataTransmitted:
                        return 0;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state));
                }
            }
        }

        public override bool CanSeek => false;

        public override long Length
        {
            get
            {
                if (!EnsureNotDisposed()) return 0;
                if (_length != null) return _length.Value;
                var hcaInfo = HcaInfo;
                long totalLength = 0;
                if (OutputWaveHeader) totalLength += _waveHeaderSize;
                totalLength += _decoder.GetMinWaveDataBufferSize() * hcaInfo.BlockCount;
                _length = totalLength;
                return _length.Value;
            }
        }

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override float LengthInSeconds => HcaHelper.CalculateLengthInSeconds(HcaInfo);

        public override uint LengthInSamples => HcaHelper.CalculateLengthInSamples(HcaInfo);

        public int BlockBatchSize => 10;

        protected override bool HasMoreData
        {
            get
            {
                const HcaAudioStreamDecodeState canDoHasMoreCheck = HcaAudioStreamDecodeState.WaveHeaderTransmitting |
                                                                    HcaAudioStreamDecodeState.WaveHeaderTransmitted |
                                                                    HcaAudioStreamDecodeState.DataTransmitting;
                if ((_state & canDoHasMoreCheck) != 0)
                {
                    var decoder = _decoder as OneWayHcaDecoder;
                    var hasMore = decoder?.HasMore ?? false;
                    return !(_waveDataSizeLeft <= 0 && _waveHeaderSizeLeft <= 0 && !hasMore);
                }
                else
                {
                    return _state == HcaAudioStreamDecodeState.Initialized;
                }
            }
        }

        private bool OutputWaveHeader { get; }

        private HcaAudioStreamDecodeState _state;
        private byte[] _waveHeaderBuffer;
        private int _waveHeaderSize;
        private int _waveHeaderSizeLeft;
        private byte[] _waveDataBuffer;
        private int _waveDataSize;
        private int _standardWaveDataSize;
        private int _waveDataSizeLeft;
        private long? _length;
    }
}