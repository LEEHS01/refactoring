using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Repositories;
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

        #region Repository
        private MapAreaRepository _repository;
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
        public UnityEvent OnAreaCleared = new UnityEvent();  // ✅ 이 줄 추가!
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지역 데이터 로드
        /// </summary>
        public void LoadAreaData(int areaId)
        {
            if (_currentAreaId == areaId)
            {
                Debug.Log($"[MapAreaViewModel] 이미 로드된 지역입니다: AreaId={areaId}");
                return;
            }

            Debug.Log($"[MapAreaViewModel] 지역 데이터 로드 시작: AreaId={areaId}");
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
        /// 지역 데이터 초기화 (HOME으로 돌아갈 때)
        /// </summary>
        public void ClearAreaData()  // ✅ 이 메서드 전체 추가!
        {
            Debug.Log($"[MapAreaViewModel] 지역 데이터 초기화: 이전 AreaId={_currentAreaId}");

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