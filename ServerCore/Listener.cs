using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory;

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _sessionFactory += sessionFactory;

            // 문지기 휴대폰                    //iPv4 or ipv6                  //통신방식
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //문지기 교육 식당주소, 후문정문 교육
            _listenSocket.Bind(endPoint);

            //영업시작
            //backlog : 최대 대기수
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted); //리스너... 손님 받아드릴때 완료이벤트 

            RegisterAccept(args);
        }

        //일단 예약한다. 비동기적으로 손님받기
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            //기존에 있던 잔재삭제.
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args); // 손님이 이미와있으면 false
            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        //레드존 멀티쓰레드, 레이스컨디션 위험 new 쓰레드일가능성이 높음
                                              //비동기이기 때문에 args를 통해 socket을 받는것임 

        //손님 입장완료!!
        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();             // 클라이언트와 recive, sender 를하기위해 session만들어줌
                session.Start(args.AcceptSocket);                       // 계속해서 받고있음 // 손님입장 완료됨
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);  // 손님받았으니까 연결됬을때 행동하기
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            // 손님 받았으니까 다음손놈 받기준비~
            RegisterAccept(args);
        }
    }
}
