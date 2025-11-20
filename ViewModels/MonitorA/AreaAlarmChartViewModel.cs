// AreaAlarmChartViewModel.cs 전체 수정 버전

using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
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
        private Coroutine _loadingCoroutine;  // ⭐ 현재 실행 중인 코루틴 추적
        #endregion

        #region Events
        [Serializable]
        public class IntEvent : UnityEvent<int> { }

        [Serializable]
        public class ChartDataEvent : UnityEvent<AreaChartData> { }

        [Serializable]
        public class ErrorEvent : UnityEvent<string> { }

        [Header("Unity Events")]
        public IntEvent OnAreaEntered = new IntEvent();
        public UnityEvent OnAreaExited = new UnityEvent();
        public ChartDataEvent OnChartDataLoaded = new ChartDataEvent();
        public ErrorEvent OnError = new ErrorEvent();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _repository = new AreaAlarmChartRepository();

            Debug.Log("[AreaAlarmChartViewModel] 초기화 완료");
        }

        private void Start()
        {
            SubscribeToMapAreaViewModel();
        }

        private void OnDestroy()
        {
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
                MapAreaViewModel.Instance.OnAreaInfoLoaded.AddListener(OnAreaInfoLoaded);
                MapAreaViewModel.Instance.OnAreaCleared.AddListener(OnAreaCleared);
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
                MapAreaViewModel.Instance.OnAreaCleared.RemoveListener(OnAreaCleared);
            }
        }
        #endregion

        #region Event Handlers
        private void OnAreaInfoLoaded(AreaInfoData areaInfo)
        {
            int areaId = areaInfo.AreaId;

            Debug.Log($"[AreaAlarmChartViewModel] 지역 진입: AreaId={areaId}, AreaName={areaInfo.AreaName}");

            // ⭐⭐⭐ 이전 로딩 코루틴 취소
            if (_loadingCoroutine != null)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
                Debug.Log($"[AreaAlarmChartViewModel] 이전 로딩 취소: {_currentAreaId}");
            }

            _currentAreaId = areaId;

            // 1. 차트 표시 이벤트 발행
            OnAreaEntered?.Invoke(areaId);

            // 2. 새 차트 데이터 로드 시작
            _loadingCoroutine = StartCoroutine(LoadChartDataCoroutine(areaId));
        }

        private void OnAreaCleared()
        {
            Debug.Log("[AreaAlarmChartViewModel] HOME 복귀: 차트 숨김");

            // ⭐ 로딩 중이면 취소
            if (_loadingCoroutine != null)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
            }

            _currentAreaId = -1;
            _currentChartData = null;

            OnAreaExited?.Invoke();
        }
        #endregion

        #region Public Methods
        public void HideChart()
        {
            Debug.Log("[AreaAlarmChartViewModel] 차트 숨김 요청");

            // ⭐ 로딩 중이면 취소
            if (_loadingCoroutine != null)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
            }

            _currentAreaId = -1;
            OnAreaExited?.Invoke();
        }

        public void RefreshCurrentChart()
        {
            if (_currentAreaId > 0)
            {
                Debug.Log($"[AreaAlarmChartViewModel] 차트 새로고침: AreaId={_currentAreaId}");

                // ⭐ 이전 로딩 취소
                if (_loadingCoroutine != null)
                {
                    StopCoroutine(_loadingCoroutine);
                }

                _loadingCoroutine = StartCoroutine(LoadChartDataCoroutine(_currentAreaId));
            }
        }
        #endregion

        #region Private Methods
        private IEnumerator LoadChartDataCoroutine(int areaId)
        {
            Debug.Log($"[AreaAlarmChartViewModel] 차트 데이터 로드 시작: AreaId={areaId}");

            bool isCompleted = false;
            float timeout = 10f;  // ⭐ 10초 타임아웃
            float elapsed = 0f;

            yield return _repository.GetAreaChartData(
                areaId,
                (chartData) =>
                {
                    // ⭐⭐⭐ AreaId가 변경되었으면 무시!
                    if (_currentAreaId != areaId)
                    {
                        Debug.LogWarning($"[AreaAlarmChartViewModel] AreaId 변경됨: {areaId} -> {_currentAreaId}, 데이터 무시");
                        isCompleted = true;
                        return;
                    }

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

            // ⭐ 타임아웃 체크
            while (!isCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!isCompleted)
            {
                Debug.LogError($"[AreaAlarmChartViewModel] 차트 데이터 로드 타임아웃: AreaId={areaId}, 경과시간: {elapsed:F1}초");
                OnError?.Invoke($"차트 데이터 로드 타임아웃");
            }

            _loadingCoroutine = null;  // ⭐ 완료 후 초기화
        }
        #endregion
    }
}