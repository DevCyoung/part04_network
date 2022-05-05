using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerCore;

namespace DummyClient
{
    class Packet
    {
        public ushort size;
        public ushort packetId;
    }

    class PlayerInfoReq : Packet
    {
        public long playerId;
    }

    class PlayerInfoOK : Packet
    {
        public int hp;
        public int attack;
    }

    public enum PacketID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,
    }

    class ServerSession : Session
    {

        static unsafe void ToBytes(byte[] array, int offset, ulong value)
        {
            fixed( byte* ptr = &array[offset])
            {
                *(ulong*)ptr = value;
            }
        }

        // 태초에 연결됬을때 할일
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected: {endPoint}");
            PlayerInfoReq packet = new PlayerInfoReq() { size = 4, packetId = (ushort)PacketID.PlayerInfoReq, playerId = 1001 };

            //보낸다.
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> s = SendBufferHelper.Open(4096);

                ushort size = 0;
                bool success = true;

                //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count - count), count);
                size += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), packet.packetId);
                size += 2;
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), packet.playerId);
                size += 8;
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count - size), size);

                ArraySegment<byte> sendBuff = SendBufferHelper.Close(size);

                if (success)
                    base.Send(sendBuff);
            }
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected: {endPoint}");
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
