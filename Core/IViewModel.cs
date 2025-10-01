namespace Core
{
    /// <summary>
    /// ViewModel 인터페이스
    /// 모든 ViewModel이 반드시 구현해야 하는 계약
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// 초기화 여부
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 로딩 중 여부
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// ViewModel 초기화
        /// View에서 호출됨
        /// </summary>
        void Initialize();

        /// <summary>
        /// ViewModel 정리 (메모리 해제, 이벤트 구독 해제 등)
        /// View가 파괴될 때 호출됨
        /// </summary>
        void Cleanup();
    }
}