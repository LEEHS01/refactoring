using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using HNS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RefactoredAreaData = HNS.Common.Models.AreaData;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 지역별 관측소 현황 ViewModel (Singleton)
    /// 알람 발생/해제 시 자동 업데이트
    /// </summary>
    public class AreaListTypeViewModel : MonoBehaviour
    {
        #region Singleton

        public static AreaListTypeViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LogInfo("Singleton 등록 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Events

        public event Action<List<AreaListModel>> OnOceanAreasChanged;
        public event Action<List<AreaListModel>> OnNuclearAreasChanged;

        #endregion

        #region Private Fields

        private AreaRepository repository;
        private SchedulerService schedulerService;

        private List<AreaListModel> oceanAreas = new();
        private List<AreaListModel> nuclearAreas = new();

        #endregion

        #region Public Properties

        public List<AreaListModel> OceanAreas => oceanAreas;
        public List<AreaListModel> NuclearAreas => nuclearAreas;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            LogInfo("Start() 시작");

            // Repository 초기화
            repository = new AreaRepository();

            // FindObjectOfType으로 찾기
            schedulerService = FindFirstObjectByType<SchedulerService>();

            if (schedulerService == null)
            {
                LogError("SchedulerService를 찾을 수 없습니다!");
            }
            else
            {
                SubscribeToScheduler();
            }

            // 초기 데이터 로드
            StartCoroutine(LoadAllAreasCoroutine());
        }

        private void OnDestroy()
        {
            UnsubscribeFromScheduler();
        }

        #endregion

        #region SchedulerService 구독

        /// <summary>
        /// SchedulerService 이벤트 구독 (알람 변경만)
        /// </summary>
        private void SubscribeToScheduler()
        {
            if (schedulerService == null) return;

            // 알람 발생 시: 즉시 업데이트
            schedulerService.OnAlarmDetected += OnAlarmChanged;

            // 알람 해제 시: 즉시 업데이트
            schedulerService.OnAlarmCancelled += OnAlarmChanged;

            LogInfo("알람 이벤트 구독 완료");
        }

        /// <summary>
        /// SchedulerService 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromScheduler()
        {
            if (schedulerService == null) return;

            schedulerService.OnAlarmDetected -= OnAlarmChanged;
            schedulerService.OnAlarmCancelled -= OnAlarmChanged;

            LogInfo("알람 이벤트 구독 해제");
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// 알람 발생/해제 시 업데이트
        /// </summary>
        private void OnAlarmChanged()
        {
            LogInfo("알람 변경 감지 - 지역별 현황 업데이트");
            StartCoroutine(LoadAllAreasCoroutine());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 특정 타입만 새로고침 (캐시된 데이터 재전송)
        /// </summary>
        public void RefreshAreasByType(RefactoredAreaData.AreaType areaType)
        {
            LogInfo($"{areaType} 타입 새로고침");

            if (areaType == RefactoredAreaData.AreaType.Ocean)
            {
                OnOceanAreasChanged?.Invoke(oceanAreas);
            }
            else
            {
                OnNuclearAreasChanged?.Invoke(nuclearAreas);
            }
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// 모든 지역 데이터 로드 (Coroutine)
        /// </summary>
        private System.Collections.IEnumerator LoadAllAreasCoroutine()
        {
            LogInfo("지역별 관측소 현황 로드 시작...");

            bool isComplete = false;
            List<AreaObservatoryStatusData> result = null;
            string error = null;

            // Repository에서 데이터 조회
            yield return repository.GetAllAreaObservatoryStatus(
                (data) =>
                {
                    result = data;
                    isComplete = true;
                },
                (err) =>
                {
                    error = err;
                    isComplete = true;
                }
            );

            // 완료 대기
            while (!isComplete)
            {
                yield return null;
            }

            // 에러 처리
            if (!string.IsNullOrEmpty(error))
            {
                LogError($"데이터 로드 실패: {error}");
                yield break;
            }

            // 데이터가 없으면 빈 리스트
            if (result == null || result.Count == 0)
            {
                LogWarning("지역 데이터가 없습니다!");
                oceanAreas = new List<AreaListModel>();
                nuclearAreas = new List<AreaListModel>();

                OnOceanAreasChanged?.Invoke(oceanAreas);
                OnNuclearAreasChanged?.Invoke(nuclearAreas);
                yield break;
            }

            // 해양시설/발전소 타입별 분류
            oceanAreas = result
                .Where(a => a.AreaType == RefactoredAreaData.AreaType.Ocean)
                .Select(MapToModel)
                .ToList();

            nuclearAreas = result
                .Where(a => a.AreaType == RefactoredAreaData.AreaType.Nuclear)
                .Select(MapToModel)
                .ToList();

            LogInfo($"해양시설: {oceanAreas.Count}개, 발전소: {nuclearAreas.Count}개");

            // 이벤트 발생
            OnOceanAreasChanged?.Invoke(oceanAreas);
            OnNuclearAreasChanged?.Invoke(nuclearAreas);
        }

        /// <summary>
        /// AreaObservatoryStatusData를 AreaListModel로 변환
        /// </summary>
        private AreaListModel MapToModel(AreaObservatoryStatusData data)
        {
            return new AreaListModel
            {
                AreaId = data.AreaId,
                AreaName = data.AreaName,
                GreenCount = data.GreenCount,
                YellowCount = data.YellowCount,
                RedCount = data.RedCount,
                PurpleCount = data.PurpleCount
            };
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[AreaListTypeViewModel] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AreaListTypeViewModel] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AreaListTypeViewModel] {message}");
        }

        #endregion
    }
}
