using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    // 받기전용 버퍼 받을때마다 버퍼를 new 하는게아닌 세션에 미리만들어놓고 사용.
    public class RecvBuffer
    {
        // [rw][][][][][][][][][]
        ArraySegment<byte> _buffer;

        int _readPos;
        int _writePos;

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get => _writePos - _readPos; }
        public int FreeSize { get => _buffer.Count - _writePos; }

        // 사용할 segment 리턴
        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        // 쓸 segment 리턴
        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        // 초기화
        public void Clean()
        {
            int dataSize = DataSize;

            if (dataSize == 0)
            {
                _writePos = _readPos = 0;
                return;
            }

            // 남은 찌끄레기 있으면 시작위치로 복사.
            Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
                return false;
            _readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;
            _writePos += numOfBytes;
            return true;
        }

    }
}