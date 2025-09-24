using UnityEngine;

namespace HNS.Core
{
    /// <summary>
    /// MVVM ViewModel의 베이스 인터페이스
    /// Inspector 기반 Unity Events와 ReactiveProperty를 연결하는 표준 계약
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// ViewModel 초기화 - ReactiveProperty를 UnityEvent에 연결
        /// </summary>
        void Initialize();

        /// <summary>
        /// ViewModel 정리 - 이벤트 구독 해제
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 로딩 상태 확인  
        /// </summary>
        bool IsLoading { get; }
    }
}