using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public struct Const<T>
    {
        public T Value { get; private set; }

        public Const(T value) : this()
        #region 공부정리
        // this() 가 이 구조체에 정의되지 않았지만, 사용 가능한 건, 정의가 안됐을 시, 기본으로 모든 필드멤버를 0 또는 null로 초기화함
        #endregion
        {
            Value = value;
        }
    }
}
