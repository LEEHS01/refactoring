using HNS.MonitorA.Models;
using Models.MonitorA;
using Models.MonitorB;
using Onthesys;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AreaDataModel = HNS.MonitorA.Models.AreaData;  // ✅ alias 추가
using ObsDataModel = HNS.MonitorA.Models.ObsData;
using ObservatoryModel = Models.MonitorB.ObservatoryModel;    // ✅ alias 추가

namespace HNS.MonitorA.Repositories
{
    /// <summary>
    /// 환경설정 팝업 Repository
    /// DatabaseService를 통해 데이터 처리 (코루틴 방식)
    /// </summary>
    public class PopupSettingRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        #region 지역 및 관측소 데이터

        /// <summary>
        /// 모든 지역 목록 가져오기 (코루틴)
        /// </summary>
        public IEnumerator GetAreas(
            Action<List<AreaDataModel>> onSuccess,  // ✅ alias 사용
            Action<string> onError)
        {
            Debug.Log("[PopupSettingRepository] GetAreas 호출");

            List<ObservatoryModel> obsModels = null;
            bool isCompleted = false;

            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (models) =>
                {
                    obsModels = models;
                    isCompleted = true;
                },
                (error) =>
                {
                    Debug.LogError($"[PopupSettingRepository] GetAreas 실패: {error}");
                    onError?.Invoke(error);
                    isCompleted = true;
                }
            );

            yield return new WaitUntil(() => isCompleted);

            if (obsModels == null || obsModels.Count == 0)
            {
                Debug.LogWarning("[PopupSettingRepository] 관측소 데이터 없음");
                onSuccess?.Invoke(new List<AreaDataModel>());  // ✅ alias 사용
                yield break;
            }

            // 중복 제거하여 지역 목록 생성 (areaType 추가)
            var areas = obsModels
                .GroupBy(o => new { o.AREAIDX, o.AREANM })
                .Select(g => new AreaDataModel(  // ✅ alias 사용
                    g.Key.AREAIDX,                      // areaId
                    g.Key.AREANM,                       // areaName
                    DetermineAreaType(g.Key.AREANM)     // areaType
                ))
                .ToList();

            Debug.Log($"[PopupSettingRepository] GetAreas 성공: {areas.Count}개");
            onSuccess?.Invoke(areas);
        }

        /// <summary>
        /// 특정 지역의 관측소 목록 가져오기 (코루틴)
        /// </summary>
        public IEnumerator GetObsByAreaId(
            int areaId,
            Action<List<ObsDataModel>> onSuccess,  // ✅ alias 사용
            Action<string> onError)
        {
            Debug.Log($"[PopupSettingRepository] GetObsByAreaId({areaId}) 호출");

            List<ObservatoryModel> obsModels = null;
            bool isCompleted = false;

            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (models) =>
                {
                    obsModels = models;
                    isCompleted = true;
                },
                (error) =>
                {
                    Debug.LogError($"[PopupSettingRepository] GetObsByAreaId 실패: {error}");
                    onError?.Invoke(error);
                    isCompleted = true;
                }
            );

            yield return new WaitUntil(() => isCompleted);

            if (obsModels == null)
            {
                onSuccess?.Invoke(new List<ObsDataModel>());  // ✅ alias 사용
                yield break;
            }

            // 해당 지역의 관측소만 필터링
            var obsList = obsModels
                .Where(o => o.AREAIDX == areaId)
                .Select(o => new ObsDataModel(  // ✅ alias 사용
                    o.OBSIDX,           // id
                    o.OBSNM,            // obsName
                    o.AREAIDX,          // areaId
                    o.AREANM,           // areaName
                    o.IN_CCTVURL,       // video1
                    o.OUT_CCTVURL       // video2
                ))
                .ToList();

            Debug.Log($"[PopupSettingRepository] GetObsByAreaId({areaId}) 성공: {obsList.Count}개");
            onSuccess?.Invoke(obsList);
        }

        /// <summary>
        /// 특정 관측소 정보 가져오기 (코루틴)
        /// </summary>
        public IEnumerator GetObs(
            int obsId,
            Action<ObsDataModel> onSuccess,  // ✅ alias 사용
            Action<string> onError)
        {
            Debug.Log($"[PopupSettingRepository] GetObs({obsId}) 호출");

            List<ObservatoryModel> obsModels = null;
            bool isCompleted = false;

            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,
                (models) =>
                {
                    obsModels = models;
                    isCompleted = true;
                },
                (error) =>
                {
                    Debug.LogError($"[PopupSettingRepository] GetObs 실패: {error}");
                    onError?.Invoke(error);
                    isCompleted = true;
                }
            );

            yield return new WaitUntil(() => isCompleted);

            if (obsModels == null)
            {
                onSuccess?.Invoke(null);
                yield break;
            }

            var obsModel = obsModels.FirstOrDefault(o => o.OBSIDX == obsId);
            if (obsModel == null)
            {
                Debug.LogWarning($"[PopupSettingRepository] ObsId={obsId}를 찾을 수 없습니다");
                onSuccess?.Invoke(null);
                yield break;
            }

            var obsData = new ObsDataModel(  // ✅ alias 사용
                obsModel.OBSIDX,           // id
                obsModel.OBSNM,            // obsName
                obsModel.AREAIDX,          // areaId
                obsModel.AREANM,           // areaName
                obsModel.IN_CCTVURL,       // video1
                obsModel.OUT_CCTVURL       // video2
            );

            Debug.Log($"[PopupSettingRepository] GetObs({obsId}) 성공: {obsData.obsName}");
            onSuccess?.Invoke(obsData);
        }

        #endregion

        #region 관측소 설정 데이터

        /// <summary>
        /// 관측소 설정 정보 가져오기 (코루틴)
        /// </summary>
        public IEnumerator GetObsSetting(
            int obsId,
            Action<ObsSettingData> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[PopupSettingRepository] GetObsSetting({obsId}) 호출");

            ObsDataModel obs = null;  // ✅ alias 사용
            List<SensorSettingData> sensors = null;
            bool obsCompleted = false;
            bool sensorsCompleted = false;

            // 1. 관측소 정보 가져오기
            yield return GetObs(obsId,
                (obsData) =>
                {
                    obs = obsData;
                    obsCompleted = true;
                },
                (error) =>
                {
                    onError?.Invoke(error);
                    obsCompleted = true;
                }
            );

            yield return new WaitUntil(() => obsCompleted);

            if (obs == null)
            {
                Debug.LogError($"[PopupSettingRepository] obsId={obsId}에 해당하는 관측소를 찾을 수 없습니다!");
                onError?.Invoke("관측소를 찾을 수 없습니다");
                yield break;
            }

            // 2. 센서 설정 가져오기
            yield return GetSensorSettings(obsId,
                (sensorSettings) =>
                {
                    sensors = sensorSettings;
                    sensorsCompleted = true;
                },
                (error) =>
                {
                    onError?.Invoke(error);
                    sensorsCompleted = true;
                }
            );

            yield return new WaitUntil(() => sensorsCompleted);

            if (sensors == null)
            {
                sensors = new List<SensorSettingData>();
            }

            // 독성도 경보값
            var toxinSensor = sensors.FirstOrDefault(s => s.BoardId == 1);
            float toxinWarning = toxinSensor != null ? toxinSensor.Warning : 0f;

            // 보드 고정 상태 (첫 번째 센서의 fix 상태로 판단)
            var toxinFixed = sensors.Any(s => s.BoardId == 1) ?
                sensors.First(s => s.BoardId == 1).IsFixed : false;
            var chemicalFixed = sensors.Any(s => s.BoardId == 2) ?
                sensors.First(s => s.BoardId == 2).IsFixed : false;
            var qualityFixed = sensors.Any(s => s.BoardId == 3) ?
                sensors.First(s => s.BoardId == 3).IsFixed : false;

            ObsSettingData setting = new ObsSettingData
            {
                ObsId = obs.id,
                AreaId = obs.areaId,
                AreaName = obs.areaName,
                ObsName = obs.obsName,
                ToxinBoardFixed = toxinFixed,
                ChemicalBoardFixed = chemicalFixed,
                QualityBoardFixed = qualityFixed,
                ToxinWarning = toxinWarning,
                CctvEquipmentUrl = obs.src_video1 ?? string.Empty,
                CctvOutdoorUrl = obs.src_video2 ?? string.Empty
            };

            Debug.Log($"[PopupSettingRepository] GetObsSetting 성공: {setting.ObsName}");
            onSuccess?.Invoke(setting);
        }

        #endregion

        #region 센서 설정 데이터

        /// <summary>
        /// 센서 설정 목록 가져오기 (코루틴)
        /// </summary>
        public IEnumerator GetSensorSettings(
            int obsId,
            Action<List<SensorSettingData>> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[PopupSettingRepository] GetSensorSettings({obsId}) 호출");

            List<SensorSettingModel> sensorModels = null;
            bool isCompleted = false;

            var parameters = new Dictionary<string, object>
            {
                { "obsidx", obsId }
            };

            yield return Database.ExecuteProcedure<SensorSettingModel>(
                "GET_SETTING",
                parameters,
                (models) =>
                {
                    sensorModels = models;
                    isCompleted = true;
                },
                (error) =>
                {
                    Debug.LogError($"[PopupSettingRepository] GetSensorSettings 실패: {error}");
                    onError?.Invoke(error);
                    isCompleted = true;
                }
            );

            yield return new WaitUntil(() => isCompleted);

            if (sensorModels == null || sensorModels.Count == 0)
            {
                Debug.LogWarning($"[PopupSettingRepository] ObsId={obsId}의 센서 설정 없음");
                onSuccess?.Invoke(new List<SensorSettingData>());
                yield break;
            }

            // SensorSettingModel을 SensorSettingData로 변환
            var sensors = new List<SensorSettingData>();

            foreach (var model in sensorModels)
            {
                sensors.Add(new SensorSettingData
                {
                    ObsId = model.OBSIDX,
                    BoardId = model.BOARDIDX,
                    HnsId = model.HNSIDX,
                    HnsName = model.HNSNM,
                    IsUsing = model.USEYN == "1",
                    IsFixed = model.INSPECTIONFLAG == "1",
                    Serious = model.ALAHIHIVAL,
                    Warning = model.ALAHIVAL
                });
            }

            Debug.Log($"[PopupSettingRepository] GetSensorSettings 성공: {sensors.Count}개");
            onSuccess?.Invoke(sensors);
        }

        #endregion

        #region 설정 업데이트

        /// <summary>
        /// 보드 고정 상태 업데이트
        /// TODO: DatabaseService를 통한 실제 DB 업데이트 구현 필요
        /// </summary>
        public bool UpdateBoardFixing(int obsId, int boardId, bool isFixing)
        {
            try
            {
                Debug.Log($"[PopupSettingRepository] UpdateBoardFixing - ObsId:{obsId}, BoardId:{boardId}, IsFixing:{isFixing}");

                // TODO: DatabaseService를 통해 직접 DB 업데이트
                // 예상 프로시저: UPDATE_BOARD_FIXING 또는 유사한 업데이트 프로시저
                Debug.LogWarning("[PopupSettingRepository] UpdateBoardFixing - DB 업데이트 프로시저 호출 필요");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateBoardFixing 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 독성도 경보값 업데이트
        /// TODO: DatabaseService를 통한 실제 DB 업데이트 구현 필요
        /// </summary>
        public bool UpdateToxinWarning(int obsId, float warning)
        {
            try
            {
                Debug.Log($"[PopupSettingRepository] UpdateToxinWarning - ObsId:{obsId}, Warning:{warning}");

                // TODO: DatabaseService를 통해 직접 DB 업데이트
                // 예상 프로시저: UPDATE_SETTING 또는 유사한 업데이트 프로시저
                // 파라미터: obsIdx, boardIdx=1, hnsIdx=1, field="ALAHIVAL", value=warning
                Debug.LogWarning("[PopupSettingRepository] UpdateToxinWarning - DB 업데이트 프로시저 호출 필요");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateToxinWarning 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 임계값 업데이트
        /// TODO: DatabaseService를 통한 실제 DB 업데이트 구현 필요
        /// </summary>
        public bool UpdateSensorThreshold(int obsId, int boardId, int hnsId, string field, float value)
        {
            try
            {
                Debug.Log($"[PopupSettingRepository] UpdateSensorThreshold - ObsId:{obsId}, BoardId:{boardId}, HnsId:{hnsId}, Field:{field}, Value:{value}");

                // TODO: DatabaseService를 통해 직접 DB 업데이트
                // 예상 프로시저: UPDATE_SETTING
                // 파라미터: obsIdx, boardIdx, hnsIdx, field, value
                Debug.LogWarning("[PopupSettingRepository] UpdateSensorThreshold - DB 업데이트 프로시저 호출 필요");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateSensorThreshold 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 사용 여부 업데이트
        /// TODO: DatabaseService를 통한 실제 DB 업데이트 구현 필요
        /// </summary>
        public bool UpdateSensorUsing(int obsId, int boardId, int hnsId, bool isUsing)
        {
            try
            {
                Debug.Log($"[PopupSettingRepository] UpdateSensorUsing - ObsId:{obsId}, BoardId:{boardId}, HnsId:{hnsId}, IsUsing:{isUsing}");

                // TODO: DatabaseService를 통해 직접 DB 업데이트
                // 예상 프로시저: UPDATE_SENSOR_USEYN
                // 파라미터: obsIdx, boardIdx, hnsIdx, useYn=(isUsing ? "1" : "0")
                Debug.LogWarning("[PopupSettingRepository] UpdateSensorUsing - DB 업데이트 프로시저 호출 필요");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateSensorUsing 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// CCTV URL 업데이트
        /// TODO: DatabaseService를 통한 실제 DB 업데이트 구현 필요
        /// </summary>
        public bool UpdateCctvUrl(int obsId, CctvType cctvType, string url)
        {
            try
            {
                Debug.Log($"[PopupSettingRepository] UpdateCctvUrl - ObsId:{obsId}, Type:{cctvType}, Url:{url}");

                // TODO: DatabaseService를 통해 직접 DB 업데이트
                // 예상 프로시저: UPDATE_CCTV_URL
                // 파라미터: obsIdx, cctvType (EQUIPMENT=IN_CCTVURL, OUTDOOR=OUT_CCTVURL), url
                Debug.LogWarning("[PopupSettingRepository] UpdateCctvUrl - DB 업데이트 프로시저 호출 필요");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateCctvUrl 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 시스템 설정

        /// <summary>
        /// 시스템 설정 가져오기
        /// </summary>
        public SystemSettingData GetSystemSetting()
        {
            try
            {
                return new SystemSettingData
                {
                    AlarmThreshold = Option.alarmThreshold,
                    DatabaseUrl = Option.url
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] GetSystemSetting 실패: {ex.Message}");
                return new SystemSettingData();
            }
        }

        /// <summary>
        /// 알람 임계값 업데이트 (PlayerPrefs 저장)
        /// </summary>
        public bool UpdateAlarmThreshold(ToxinStatus threshold)
        {
            try
            {
                PlayerPrefs.SetInt("alarmThreshold", (int)threshold);
                Option.alarmThreshold = threshold;

                Debug.Log($"[PopupSettingRepository] UpdateAlarmThreshold - Threshold:{threshold} 저장 완료");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateAlarmThreshold 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 데이터베이스 URL 업데이트 (PlayerPrefs 저장)
        /// </summary>
        public bool UpdateDatabaseUrl(string url)
        {
            try
            {
                PlayerPrefs.SetString("dbAddress", url);
                Option.url = url;

                Debug.Log("[PopupSettingRepository] UpdateDatabaseUrl 저장 완료");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PopupSettingRepository] UpdateDatabaseUrl 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 지역명으로 타입 결정 (해역/발전소)
        /// </summary>
        private AreaDataModel.AreaType DetermineAreaType(string areaName)  // ✅ alias 사용
        {
            // "원자력", "화력" 포함 시 Nuclear, 아니면 Ocean
            if (areaName.Contains("원자력") || areaName.Contains("화력"))
            {
                return AreaDataModel.AreaType.Nuclear;  // ✅ alias 사용
            }
            return AreaDataModel.AreaType.Ocean;  // ✅ alias 사용
        }

        #endregion
    }
}