using System;
using System.Net;
using System.Text;
using System.Threading;
using ServerCore;

namespace Server
{
    // 대부분 패킷의 설계는 첫번째인자는 패킷의 사이즈 
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

    class ClientSession : PacketSession
    {
        // 태초에 연결됬을때 할일
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected: {endPoint}");

            Packet knight = new Packet() { size = 100, packetId = 10 };

            try
            {
                //보낸다
                //byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");

                //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
                //byte[] buffer = BitConverter.GetBytes(knight.size);
                //byte[] buffer2 = BitConverter.GetBytes(knight.packetId);
                //Array.Copy(buffer,  0, openSegment.Array, openSegment.Offset, buffer.Length);
                //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

                ////보내야할 버퍼
                //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

                //base.Send(sendBuff); // 계속해서 보내고있음
                Thread.Sleep(5000);
                base.DisConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        //완벽히 완성된 패킷
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            ushort count = 0;


            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            switch ((PacketID)id)
            {
                case PacketID.PlayerInfoReq:
                    long playerid = BitConverter.ToInt64(buffer.Array, buffer.Offset + count);
                    count += 8;
                    Console.WriteLine($"Player InfoReq: {playerid}, Size {size}");
                    break;
                case PacketID.PlayerInfoOk:

                    break;
                default:
                    break;
            }

            Console.WriteLine($"RecvPacketId: {id}, SIze {size}");
        }

        public override void OnDisConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisConnected: {endPoint}");
        }


        // 이동 패킷15 ((3,2) 좌표로 이동하고싶다)
        // 15 3 2 만약 15와 3까지만 왔다면 실행을하면안된다.
        //public override int OnRecv(ArraySegment<byte> buffer)
        //{
        //    string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        //    Console.WriteLine($"[From Client] {recvData}");
        //    return buffer.Count;
        //}

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

    }
}
