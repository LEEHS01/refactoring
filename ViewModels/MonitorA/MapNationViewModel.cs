using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using HNS.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 전국 지도 ViewModel
    /// - AreaRepository로 지역별 알람 상태 조회
    /// - SchedulerService로 알람 변경 감지
    /// </summary>
    public class MapNationViewModel : MonoBehaviour
    {
        public static MapNationViewModel Instance { get; private set; }

        // Repository & Service
        private AreaRepository _areaRepository;
        private SchedulerService _schedulerService;  // FindFirstObjectByType으로 찾기

        // State
        public List<MapMarkerData> MarkerDataList { get; private set; }

        // Events
        public UnityEvent<List<MapMarkerData>> OnMarkersUpdated = new UnityEvent<List<MapMarkerData>>();
        public UnityEvent<string> OnError = new UnityEvent<string>();

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

            _areaRepository = new AreaRepository();
            MarkerDataList = new List<MapMarkerData>();

            Debug.Log("[MapNationViewModel] 초기화 완료");
        }

        private void Start()
        {
            SubscribeToScheduler();
            LoadMarkerData();
        }

        private void OnDestroy()
        {
            UnsubscribeFromScheduler();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Scheduler 이벤트 구독

        private void SubscribeToScheduler()
        {
            // SchedulerService 자동 검색
            _schedulerService = FindFirstObjectByType<SchedulerService>();

            if (_schedulerService == null)
            {
                Debug.LogWarning("[MapNationViewModel] SchedulerService를 찾을 수 없습니다. 이벤트 구독 건너뜀");
            }
            else
            {
                // 알람 발생/해제 시 마커 갱신
                _schedulerService.OnAlarmDetected += LoadMarkerData;
                _schedulerService.OnAlarmCancelled += LoadMarkerData;
                Debug.Log("[MapNationViewModel] SchedulerService 이벤트 구독 완료");
            }
        }

        private void UnsubscribeFromScheduler()
        {
            if (_schedulerService != null)
            {
                _schedulerService.OnAlarmDetected -= LoadMarkerData;
                _schedulerService.OnAlarmCancelled -= LoadMarkerData;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 마커 데이터 로드 (지역별 알람 상태)
        /// </summary>
        public void LoadMarkerData()
        {
            StartCoroutine(LoadMarkerDataCoroutine());
        }

        #endregion

        #region Private Methods

        private IEnumerator LoadMarkerDataCoroutine()
        {
            Debug.Log("[MapNationViewModel] 마커 데이터 로드 시작...");

            bool isComplete = false;
            List<AreaObservatoryStatusData> areaData = null;
            string errorMessage = null;

            // Repository를 통해 지역별 알람 상태 조회
            yield return _areaRepository.GetAllAreaObservatoryStatus(
                onSuccess: (data) =>
                {
                    areaData = data;
                    isComplete = true;
                },
                onError: (error) =>
                {
                    errorMessage = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            // 에러 처리
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"[MapNationViewModel] 데이터 로드 실패: {errorMessage}");
                OnError?.Invoke(errorMessage);
                yield break;
            }

            // 데이터 없음
            if (areaData == null || areaData.Count == 0)
            {
                Debug.LogWarning("[MapNationViewModel] 지역 데이터가 없습니다.");
                MarkerDataList = new List<MapMarkerData>();
                OnMarkersUpdated?.Invoke(MarkerDataList);
                yield break;
            }

            // AreaObservatoryStatusData → MapMarkerData 변환
            MarkerDataList = areaData.Select(area => new MapMarkerData
            {
                AreaId = area.AreaId,
                AreaName = area.AreaName,
                AreaType = area.AreaType,
                Status = CalculateAreaStatus(area)
            }).ToList();

            Debug.Log($"[MapNationViewModel] 마커 데이터 로드 완료: {MarkerDataList.Count}개");

            // 각 마커 상태 로그
            foreach (var marker in MarkerDataList)
            {
                string statusName = GetStatusName(marker.Status);
                Debug.Log($"  - {marker.AreaName}: {statusName}");
            }

            // View에 알림
            OnMarkersUpdated?.Invoke(MarkerDataList);
        }

        /// <summary>
        /// 지역 상태 계산 (우선순위: Red > Yellow > Purple > Green)
        /// </summary>
        private int CalculateAreaStatus(AreaObservatoryStatusData area)
        {
            if (area.RedCount > 0) return 2;     // Red - 경보
            if (area.YellowCount > 0) return 1;  // Yellow - 경계
            if (area.PurpleCount > 0) return 3;  // Purple - 설비이상
            return 0;                             // Green - 정상
        }

        private string GetStatusName(int status)
        {
            return status switch
            {
                0 => "정상(Green)",
                1 => "경계(Yellow)",
                2 => "경보(Red)",
                3 => "설비이상(Purple)",
                _ => "Unknown"
            };
        }

        #endregion
    }
}