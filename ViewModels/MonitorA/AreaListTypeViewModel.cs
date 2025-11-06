using Assets.Scripts_refactoring.Models.MonitorA;
using HNS.MonitorA.Models;
using HNS.MonitorA.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts_refactoring.ViewModels.MonitorA
{
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

            // 초기 데이터 로드
            StartCoroutine(LoadAllAreasCoroutine());
        }

        #endregion

        #region Public Methods

        public void LoadAllAreas()
        {
            StartCoroutine(LoadAllAreasCoroutine());
        }

        /// <summary>
        /// 특정 타입만 새로고침
        /// </summary>
        public void RefreshAreasByType(AreaData.AreaType areaType)  // ⭐ 수정!
        {
            LogInfo($"{areaType} 타입 새로고침");

            if (areaType == AreaData.AreaType.Ocean)  // ⭐ 수정!
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

        private System.Collections.IEnumerator LoadAllAreasCoroutine()
        {
            LogInfo("지역별 관측소 현황 로드 시작...");

            bool isComplete = false;
            List<AreaObservatoryStatusData> result = null;
            string error = null;

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

            while (!isComplete)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(error))
            {
                LogError($"데이터 로드 실패: {error}");
                yield break;
            }

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
                .Where(a => a.AreaType == AreaData.AreaType.Ocean)  // ⭐ 수정!
                .Select(MapToModel)
                .ToList();

            nuclearAreas = result
                .Where(a => a.AreaType == AreaData.AreaType.Nuclear)  // ⭐ 수정!
                .Select(MapToModel)
                .ToList();

            LogInfo($"해양시설: {oceanAreas.Count}개, 발전소: {nuclearAreas.Count}개");

            OnOceanAreasChanged?.Invoke(oceanAreas);
            OnNuclearAreasChanged?.Invoke(nuclearAreas);
        }

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