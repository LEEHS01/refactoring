using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Repositories;
using HNS.Services;  // ⭐ 추가
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 지역 상세 지도 ViewModel
    /// Monitor B 패턴 적용
    /// </summary>
    public class MapAreaViewModel : MonoBehaviour
    {
        #region Singleton
        public static MapAreaViewModel Instance { get; private set; }
        #endregion

        #region Repository & Service
        private MapAreaRepository _repository;
        private SchedulerService _schedulerService;  // ⭐ 추가
        #endregion

        #region Current Data
        private int _currentAreaId = -1;
        private AreaInfoData _currentAreaInfo;
        private List<ObsMarkerData> _currentObservatories;
        #endregion

        #region Events
        [Serializable]
        public class AreaInfoEvent : UnityEvent<AreaInfoData> { }

        [Serializable]
        public class ObsMarkerListEvent : UnityEvent<List<ObsMarkerData>> { }

        [Serializable]
        public class ErrorEvent : UnityEvent<string> { }

        [Header("Unity Events")]
        public AreaInfoEvent OnAreaInfoLoaded = new AreaInfoEvent();
        public ObsMarkerListEvent OnObservatoriesLoaded = new ObsMarkerListEvent();
        public ErrorEvent OnError = new ErrorEvent();
        public UnityEvent OnAreaCleared = new UnityEvent();
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
            _repository = new MapAreaRepository();

            Debug.Log("[MapAreaViewModel] 초기화 완료");
        }

        // ⭐⭐⭐ 추가
        private void Start()
        {
            SubscribeToScheduler();
        }

        private void OnDestroy()
        {
            UnsubscribeFromScheduler();  // ⭐ 추가

            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Scheduler 이벤트 구독 (⭐⭐⭐ 새로 추가)

        private void SubscribeToScheduler()
        {
            _schedulerService = FindFirstObjectByType<SchedulerService>();

            if (_schedulerService == null)
            {
                Debug.LogWarning("[MapAreaViewModel] SchedulerService를 찾을 수 없습니다.");
            }
            else
            {
                // 알람 발생/해제 시 현재 지역의 관측소 마커 갱신
                _schedulerService.OnAlarmDetected += OnAlarmChanged;
                _schedulerService.OnAlarmCancelled += OnAlarmChanged;
                Debug.Log("[MapAreaViewModel] SchedulerService 이벤트 구독 완료");
            }
        }

        private void UnsubscribeFromScheduler()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnAlarmDetected -= OnAlarmChanged;
                _schedulerService.OnAlarmCancelled -= OnAlarmChanged;
            }
        }

        private void OnAlarmChanged()
        {
            // 현재 지역이 선택되어 있으면 관측소 마커 업데이트
            if (_currentAreaId > 0)
            {
                Debug.Log($"[MapAreaViewModel] ⭐ 알람 변경 감지 → 관측소 마커 업데이트 (AreaId={_currentAreaId})");
                RefreshObservatoryMarkers();
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// 지역 데이터 로드
        /// </summary>
        public void LoadAreaData(int areaId)
        {
            Debug.Log($"[MapAreaViewModel] 지역 데이터 로드 시작: AreaId={areaId}");

            // ✅ 이미 로드된 지역이어도 이벤트는 발생시킴 (UI 복원용)
            if (_currentAreaId == areaId && _currentAreaInfo != null)
            {
                Debug.Log($"[MapAreaViewModel] 이미 로드된 지역입니다: AreaId={areaId} - 캐시 데이터 사용");

                // ⭐ 캐시된 데이터로 이벤트 발생 (MapAreaView가 AreaListTypeView를 숨기기 위해 필요)
                OnAreaInfoLoaded?.Invoke(_currentAreaInfo);

                if (_currentObservatories != null && _currentObservatories.Count > 0)
                {
                    OnObservatoriesLoaded?.Invoke(_currentObservatories);
                    Debug.Log($"[MapAreaViewModel] 캐시된 관측소 데이터 이벤트 발생: {_currentObservatories.Count}개");
                }

                return;
            }

            // 새 지역 데이터 로딩
            _currentAreaId = areaId;
            StartCoroutine(LoadAreaDataCoroutine(areaId));
        }

        /// <summary>
        /// 현재 지역 새로고침
        /// </summary>
        public void RefreshCurrentArea()
        {
            if (_currentAreaId > 0)
            {
                Debug.Log($"[MapAreaViewModel] 현재 지역 새로고침: AreaId={_currentAreaId}");
                StartCoroutine(LoadAreaDataCoroutine(_currentAreaId));
            }
        }

        /// <summary>
        /// ⭐⭐⭐ 관측소 마커만 새로고침 (알람 변경 시)
        /// </summary>
        private void RefreshObservatoryMarkers()
        {
            if (_currentAreaId <= 0) return;

            StartCoroutine(RefreshObservatoryMarkersCoroutine());
        }

        /// <summary>
        /// 지역 데이터 초기화 (HOME으로 돌아갈 때)
        /// </summary>
        public void ClearAreaData()
        {
            Debug.Log($"[MapAreaViewModel] 지역 데이터 초기화: 이전 AreaId={_currentAreaId}");

            // ⭐ 3D 관측소가 활성화되어 있으면 먼저 정리
            if (Area3DViewModel.Instance != null && Area3DViewModel.Instance.IsObservatoryActive)
            {
                Area3DViewModel.Instance.CloseObservatory();
                Debug.Log("[MapAreaViewModel] 3D 관측소 자동 정리");
            }

            // 데이터 초기화
            _currentAreaId = -1;
            _currentAreaInfo = null;
            _currentObservatories = null;

            // 진행 중인 코루틴 중단
            StopAllCoroutines();

            // 이벤트 발행
            OnAreaCleared?.Invoke();

            Debug.Log("[MapAreaViewModel] 지역 데이터 초기화 완료 - HOME 상태로 복귀");
        }
        #endregion

        #region Private Methods - Coroutines
        /// <summary>
        /// 지역 데이터 로드 코루틴
        /// </summary>
        private IEnumerator LoadAreaDataCoroutine(int areaId)
        {
            bool areaInfoLoaded = false;
            bool observatoriesLoaded = false;

            // 1. 지역 정보 로드
            yield return _repository.GetAreaInfo(
                areaId,
                (areaInfo) =>
                {
                    _currentAreaInfo = areaInfo;
                    OnAreaInfoLoaded?.Invoke(areaInfo);
                    areaInfoLoaded = true;
                    Debug.Log($"[MapAreaViewModel] 지역 정보 로드 완료: {areaInfo.AreaName}");
                },
                (error) =>
                {
                    Debug.LogError($"[MapAreaViewModel] 지역 정보 로드 실패: {error}");
                    OnError?.Invoke($"지역 정보 로드 실패: {error}");
                    areaInfoLoaded = true;
                }
            );

            // 지역 정보 로드 완료 대기
            while (!areaInfoLoaded)
                yield return null;

            // 2. 관측소 마커 데이터 로드
            yield return _repository.GetObservatoryMarkers(
                areaId,
                (observatories) =>
                {
                    _currentObservatories = observatories;
                    OnObservatoriesLoaded?.Invoke(observatories);
                    observatoriesLoaded = true;
                    Debug.Log($"[MapAreaViewModel] 관측소 마커 로드 완료: {observatories.Count}개");
                },
                (error) =>
                {
                    Debug.LogError($"[MapAreaViewModel] 관측소 마커 로드 실패: {error}");
                    OnError?.Invoke($"관측소 마커 로드 실패: {error}");
                    observatoriesLoaded = true;
                }
            );

            // 관측소 데이터 로드 완료 대기
            while (!observatoriesLoaded)
                yield return null;

            Debug.Log($"[MapAreaViewModel] 전체 데이터 로드 완료: AreaId={areaId}");
        }

        /// <summary>
        /// ⭐⭐⭐ 관측소 마커만 새로고침하는 코루틴
        /// </summary>
        private IEnumerator RefreshObservatoryMarkersCoroutine()
        {
            bool completed = false;

            yield return _repository.GetObservatoryMarkers(
                _currentAreaId,
                (observatories) =>
                {
                    _currentObservatories = observatories;
                    OnObservatoriesLoaded?.Invoke(observatories);
                    completed = true;
                    Debug.Log($"[MapAreaViewModel] 관측소 마커 색상 업데이트 완료: {observatories.Count}개");
                },
                (error) =>
                {
                    Debug.LogError($"[MapAreaViewModel] 관측소 마커 업데이트 실패: {error}");
                    completed = true;
                }
            );

            while (!completed)
                yield return null;
        }
        #endregion

        #region Properties (읽기 전용)
        /// <summary>
        /// 현재 선택된 지역 ID
        /// </summary>
        public int CurrentAreaId => _currentAreaId;

        /// <summary>
        /// 현재 지역 정보
        /// </summary>
        public AreaInfoData CurrentAreaInfo => _currentAreaInfo;

        /// <summary>
        /// 현재 관측소 리스트
        /// </summary>
        public List<ObsMarkerData> CurrentObservatories => _currentObservatories;

        /// <summary>
        /// 지역이 선택되어 있는지 여부
        /// </summary>
        public bool IsAreaSelected => _currentAreaId > 0;
        #endregion
    }
}