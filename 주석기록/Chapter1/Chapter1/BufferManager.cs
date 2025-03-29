using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    internal class BufferManager
    {
        int m_numBytes;
        byte[] m_buffer;
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }
        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
                #region 공부정리
                // ○ SocketAsyncEventArgs(instance).SetBuffer 
                //  - 해당 인스턴스가 사용할 버퍼의 위치와 크기를 지정함. SocketAsyncEventArgs(instance)는 SetBuffer를 통해 할당받은 byte[]를 기반으로, 통신을 한다 함(From 지피티)
                //  - (1) : 참조되는 byte[]
                //  - (2) : 버퍼의 시작 인덱스
                //  - (3) : 사용할 버퍼의 크기
                //   => SocketAsynEventArgs Pool 이 공통의 m_buffer를 나눠 사용하는 구조이기 때문에, (2)항을 m_freeIndexPool.Pop()으로, (3)항을 m_bufferSize로 사용
                //      => 처음 시작하면, m_freeIndexPool.Count == 0 이고, else{ 구문이 작동. 사용을 다한 SocketasyncEventArgs는 하단의 FreeBuffers매서드를 통해 m_freeIndexPool로 푸시되어, 위 if(m_freeIndexPool.Count에서 사용됨) 
                #endregion
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }
        public void FreeBuffers(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            #region 공부정리
            // args.Offset : SetBuffer의 (2)값을 SocketAsyncEventArgs(instance).offset 으로 저장
            #endregion
            args.SetBuffer(null, 0, 0);
        }
    }
}
