using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;
        // PacketSession 자식부턴 이것을 재정의 할수없음
        // 패킷은 [Size(2)] [ID[2]] ..... [Size(2)] [ID[2]].....
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            // TCP 이기때문에 패킷이 잘려서 올수도있기때문에 체크를해야함.
            int processLen = 0;

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                //패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset); //ushort만큼 긁어옴
                if (buffer.Count < dataSize)
                    break;

                //여기까지 왔으면 패킷 사용가능하다는뜻
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;

                //버퍼를 사용했으니 초기화한다.
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize,
                    buffer.Count - dataSize);
            }
            // 내가 처리한 바이트수
            return processLen;
        }

        //한번더래핑
        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    // 세션을 각각의 손님이 모두 가지고있다.
    public abstract class Session
    {
        //send 를 한다고해서 매번마다 async를 하는게아니라 모아뒀다가 한번에 async한다. (async 가 완료될때까지 Send는 쌓아두기만한다)
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); //대기중인목록
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        RecvBuffer _recvBuffer = new RecvBuffer(1024);



        Socket _socket;
        int _disconnected = 0;
        object _lock = new object();
        
        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisConnected(EndPoint endPoint);

        // 세션을 생성했다는건 연결이완료됬다는뜻... 그러므로 메세지를 받을준비를 해둔다.
        public void Start(Socket socket)
        {
            _socket = socket;
            
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            
            _recvArgs.UserToken = 1;
            RegisterRecv();
        }


        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0) // 내가1빠 Send 비동기등록후 전송 완료됬다고 이벤트실행전까진 계속 넣어놓기만한다.
                    RegisterSend();
            }
        }


        #region 네트워크 통신

        void RegisterSend()
        {

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                //ArraySegment을 사용하는이유? a[][][][][][][][][][] c++ 이라면 포인터주소를 알려주면되는데 C#은 불가능 (배열의조각)
                _pendingList.Add(buff);
                //_sendArgs.SetBuffer(buff, 0, buff.Length); => 같이사용하면 문제가됨
            }

            _sendArgs.BufferList = _pendingList;

            bool pending = _socket.SendAsync(_sendArgs); //이친구도 커널단에서 예약됨
            if (pending == false)
                OnSendCompleted(null, _sendArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    //TODO
                    try
                    {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        //축하해용 보내기완료!
                        OnSend(_sendArgs.BytesTransferred);

                        // 누군가가 또 send했다면
                        if (_sendQueue.Count > 0)
                            RegisterSend();
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    DisConnect();
                }
            }
        }

        void RegisterRecv()
        {
            _recvBuffer.Clean();
            ArraySegment<byte> writeSegment = _recvBuffer.WriteSegment;
            // 비동기적인 recv이기 때문에 이벤트가 실행될때를 대비해 args를 (버퍼를 얼마나받을지)셋팅하는것, 어디에다가 저장할지
            _recvArgs.SetBuffer(writeSegment);

            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnRecvCompleted(null, _recvArgs);
        }

        // 띠링~ 클라이언트한테서 뭔가를 받았어용 어쩌면 새로운 쓰레드 생성!
        // 어디에 데이터를 쓸지는  _recvArgs.SetBuffer 통해 저장해놨음
        // TCP 통신이기때문에 데이터가 온전히 안왔을수도있다.
        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {

                    // Write 커서를 이동 (데이터를 받았기때문에) 쓰자!
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        DisConnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다.
                    // OnRecv는 컨텐츠쪽에서 구현
                    // 여기까지왔으면 받은데이터를 OnRecv 에서검사해 읽을수있으면 읽은만큼반환
                    // 읽을수없으면 0을반환
                    int processLen = OnRecv(_recvBuffer.ReadSegment);

                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        DisConnect();
                        return;
                    }

                    //Read 커서를 이동 읽자!
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        DisConnect();
                        return;
                    }

                    // 또 받기등록
                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                // 실패
                DisConnect();
            }
        }

        public void DisConnect()
        {
            //OnRecvCompleted는 다른 스레드 이기때문에 _disconnected는 임계영역이다. (레이스 컨디션 발생)
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisConnected(_socket.RemoteEndPoint);
            _disconnected = 1;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        #endregion

    }
}