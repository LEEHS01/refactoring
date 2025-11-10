using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 지역 알람 차트 ViewModel
    /// - MapAreaViewModel 이벤트 구독 (UiManager 사용 안 함)
    /// - 12개월 히스토리 데이터 로드
    /// </summary>
    public class AreaAlarmChartViewModel : MonoBehaviour
    {
        #region Singleton

        public static AreaAlarmChartViewModel Instance { get; private set; }

        #endregion

        #region Repository

        private AreaAlarmChartRepository _repository;

        #endregion

        #region Current State

        private int _currentAreaId = -1;
        private AreaChartData _currentChartData;

        #endregion

        #region Events

        [Serializable]
        public class IntEvent : UnityEvent<int> { }

        [Serializable]
        public class ChartDataEvent : UnityEvent<AreaChartData> { }

        [Serializable]
        public class ErrorEvent : UnityEvent<string> { }

        [Header("Unity Events")]
        public IntEvent OnAreaEntered = new IntEvent();              // 지역 진입 → 차트 표시
        public UnityEvent OnAreaExited = new UnityEvent();           // 지역 퇴장 → 차트 숨김
        public ChartDataEvent OnChartDataLoaded = new ChartDataEvent(); // 차트 데이터 로드 완료
        public ErrorEvent OnError = new ErrorEvent();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Repository 생성
            _repository = new AreaAlarmChartRepository();

            Debug.Log("[AreaAlarmChartViewModel] 초기화 완료");
        }

        private void Start()
        {
            // MapAreaViewModel 이벤트 구독
            SubscribeToMapAreaViewModel();
        }

        private void OnDestroy()
        {
            // MapAreaViewModel 이벤트 해제
            UnsubscribeFromMapAreaViewModel();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region MapAreaViewModel Events

        private void SubscribeToMapAreaViewModel()
        {
            if (MapAreaViewModel.Instance != null)
            {
                // 지역 진입 시
                MapAreaViewModel.Instance.OnAreaInfoLoaded.AddListener(OnAreaInfoLoaded);

                Debug.Log("[AreaAlarmChartViewModel] MapAreaViewModel 이벤트 구독 완료");
            }
            else
            {
                Debug.LogWarning("[AreaAlarmChartViewModel] MapAreaViewModel.Instance가 null입니다!");
            }
        }

        private void UnsubscribeFromMapAreaViewModel()
        {
            if (MapAreaViewModel.Instance != null)
            {
                MapAreaViewModel.Instance.OnAreaInfoLoaded.RemoveListener(OnAreaInfoLoaded);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 지역 진입 (MapAreaViewModel.OnAreaInfoLoaded)
        /// </summary>
        private void OnAreaInfoLoaded(AreaInfoData areaInfo)
        {
            int areaId = areaInfo.AreaId;

            Debug.Log($"[AreaAlarmChartViewModel] 지역 진입: AreaId={areaId}, AreaName={areaInfo.AreaName}");

            _currentAreaId = areaId;

            // 1. 차트 표시 이벤트 발행
            OnAreaEntered?.Invoke(areaId);

            // 2. 차트 데이터 로드
            StartCoroutine(LoadChartDataCoroutine(areaId));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 차트 숨김 (외부에서 호출 가능)
        /// </summary>
        public void HideChart()
        {
            Debug.Log("[AreaAlarmChartViewModel] 차트 숨김 요청");

            _currentAreaId = -1;
            OnAreaExited?.Invoke();
        }

        /// <summary>
        /// 현재 지역 차트 새로고침
        /// </summary>
        public void RefreshCurrentChart()
        {
            if (_currentAreaId > 0)
            {
                Debug.Log($"[AreaAlarmChartViewModel] 차트 새로고침: AreaId={_currentAreaId}");
                StartCoroutine(LoadChartDataCoroutine(_currentAreaId));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 차트 데이터 로드 코루틴
        /// </summary>
        private IEnumerator LoadChartDataCoroutine(int areaId)
        {
            Debug.Log($"[AreaAlarmChartViewModel] 차트 데이터 로드 시작: AreaId={areaId}");

            bool isCompleted = false;

            yield return _repository.GetAreaChartData(
                areaId,
                (chartData) =>
                {
                    _currentChartData = chartData;
                    OnChartDataLoaded?.Invoke(chartData);
                    isCompleted = true;
                    Debug.Log($"[AreaAlarmChartViewModel] 차트 데이터 로드 완료: {chartData.AreaName}");
                },
                (error) =>
                {
                    Debug.LogError($"[AreaAlarmChartViewModel] 차트 데이터 로드 실패: {error}");
                    OnError?.Invoke($"차트 데이터 로드 실패: {error}");
                    isCompleted = true;
                }
            );

            // 로드 완료 대기
            while (!isCompleted)
                yield return null;
        }

        #endregion
    }
}