namespace GameServer
{
    public enum PROTOCOL : short
    {
        BEGIN = 0,
        CHAT_MSG_REQ = 1,
        CHAT_MSG_ACK = 2,

        END
        #region 공부정리
        // ○ enum에서 값을 할당하지 않을 경우, 이전 할당값의 +1을 부여한다. 따라서 여기서 END는 3이 된다
        #endregion
    }
}
