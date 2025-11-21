using HNS.Common.Models;
using Newtonsoft.Json;
using Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models.MonitorB;
using ObservatoryModel = Models.MonitorB.ObservatoryModel;

namespace HNS.Common.Repositories
{
    /// <summary>
    /// 설정 데이터 Repository
    /// </summary>
    public class SettingsRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        #region 관측소 설정 조회

        /// <summary>
        /// 관측소 설정 데이터 조회 (GET_SETTING)
        /// </summary>
        public IEnumerator GetObservatorySettings(
            int obsId,
            Action<ObservatorySettingData> onSuccess,
            Action<string> onError)
        {
            var parameters = new Dictionary<string, object>
            {
                { "obsidx", obsId }
            };

            bool isComplete = false;
            ObservatorySettingData result = null;
            string errorMsg = null;

            yield return Database.ExecuteProcedure<SettingModel>(
                "GET_SETTING",
                parameters,
                (models) =>
                {
                    if (models != null && models.Count > 0)
                    {
                        result = ParseObservatorySettings(models, obsId);
                    }
                    isComplete = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            if (!string.IsNullOrEmpty(errorMsg))
            {
                onError?.Invoke(errorMsg);
            }
            else if (result != null)
            {
                onSuccess?.Invoke(result);
            }
            else
            {
                onError?.Invoke("설정 데이터를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 모든 지역 목록 조회 (TB_AREA 테이블 - 직접 쿼리)
        /// </summary>
        public IEnumerator GetAllAreas(
            Action<List<AreaData>> onSuccess,
            Action<string> onError)
        {
            // ⭐ 직접 SELECT 쿼리 (프로시저 아님!)
            string query = "SELECT * FROM TB_AREA;";

            bool isComplete = false;
            List<AreaData> result = null;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) =>
                {
                    try
                    {
                        var models = JsonConvert.DeserializeObject<List<AreaModel>>(response);
                        if (models != null && models.Count > 0)
                        {
                            result = ParseAreas(models);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMsg = $"지역 데이터 파싱 실패: {ex.Message}";
                    }
                    isComplete = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            if (!string.IsNullOrEmpty(errorMsg))
            {
                onError?.Invoke(errorMsg);
            }
            else if (result != null && result.Count > 0)
            {
                onSuccess?.Invoke(result);
            }
            else
            {
                onError?.Invoke("지역 데이터를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 특정 지역의 관측소 목록 조회 (TB_OBS 테이블 - 직접 쿼리)
        /// </summary>
        public IEnumerator GetObservatoriesByArea(
            int areaId,
            Action<List<ObservatoryData>> onSuccess,
            Action<string> onError)
        {
            // ⭐ 직접 SELECT 쿼리 (프로시저 아님!)
            string query = $"SELECT * FROM TB_OBS WHERE AREAIDX = {areaId};";

            bool isComplete = false;
            List<ObservatoryData> result = null;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) =>
                {
                    try
                    {
                        var models = JsonConvert.DeserializeObject<List<ObservatoryModel>>(response);
                        if (models != null && models.Count > 0)
                        {
                            result = ParseObservatories(models, areaId);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMsg = $"관측소 데이터 파싱 실패: {ex.Message}";
                    }
                    isComplete = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            if (!string.IsNullOrEmpty(errorMsg))
            {
                onError?.Invoke(errorMsg);
            }
            else if (result != null && result.Count > 0)
            {
                onSuccess?.Invoke(result);
            }
            else
            {
                onError?.Invoke("관측소 데이터를 찾을 수 없습니다.");
            }
        }

        #endregion

        #region 설정 업데이트
        /// <summary>
        /// 관측소 정보 (CCTV URL 포함) 조회 - GET_OBS 프로시저 사용
        /// </summary>
        public IEnumerator GetObservatoryWithCctv(
            int obsId,
            Action<ObservatoryModel> onSuccess,
            Action<string> onError)
        {
            bool isComplete = false;
            ObservatoryModel result = null;
            string errorMsg = null;

            // ⭐ 원본처럼 GET_OBS 사용
            yield return Database.ExecuteProcedure<ObservatoryModel>(
                "GET_OBS",
                null,  // GET_OBS는 파라미터 없음
                (models) =>
                {
                    if (models != null && models.Count > 0)
                    {
                        // 해당 ObsId의 관측소 찾기
                        result = models.Find(m => m.OBSIDX == obsId);

                        if (result != null)
                        {
                            Debug.Log($"[SettingsRepository] GET_OBS 성공 - ObsId={obsId}");
                            Debug.Log($"  - IN_CCTVURL: {result.IN_CCTVURL}");
                            Debug.Log($"  - OUT_CCTVURL: {result.OUT_CCTVURL}");
                        }
                    }
                    isComplete = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            if (!string.IsNullOrEmpty(errorMsg))
            {
                onError?.Invoke(errorMsg);
            }
            else if (result != null)
            {
                onSuccess?.Invoke(result);
            }
            else
            {
                onError?.Invoke($"관측소 {obsId}를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 독성도 경고값 업데이트 (UPDATE TB_HNS)
        /// </summary>
        public IEnumerator UpdateToxinWarning(
            int obsId,
            float warningValue,
            Action onSuccess,
            Action<string> onError)
        {
            // ✅ 원본처럼 직접 UPDATE 쿼리 사용
            string query = $@"UPDATE TB_HNS 
                SET ALAHIHIVAL = {warningValue}
                WHERE OBSIDX = {obsId} AND BOARDIDX = 1 AND HNSIDX = 1;";

            bool isComplete = false;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) => { isComplete = true; },
                (error) => { errorMsg = error; isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        /// <summary>
        /// 보드 고정 상태 업데이트 (SET_BOARD_ISFIXING)
        /// </summary>
        public IEnumerator UpdateBoardFixing(
            int obsId,
            int boardId,
            bool isFixed,
            Action onSuccess,
            Action<string> onError)
        {
            var parameters = new Dictionary<string, object>
            {
                { "obsIdx", obsId },
                { "boardIdx", boardId },
                { "isFixing", isFixed ? 1 : 0 }
            };

            bool isComplete = false;
            string errorMsg = null;

            yield return Database.ExecuteProcedure<EmptyModel>(
                "SET_BOARD_ISFIXING",  // ✅ 원본 프로시저명
                parameters,
                (models) => { isComplete = true; },
                (error) => { errorMsg = error; isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        /// <summary>
        /// 센서 사용 여부 업데이트 (UPDATE TB_HNS)
        /// </summary>
        public IEnumerator UpdateSensorUsing(
            int obsId,
            int boardId,
            int sensorId,
            bool isUsing,
            Action onSuccess,
            Action<string> onError)
        {
            // ✅ 원본처럼 직접 UPDATE 쿼리 사용
            string query = $@"UPDATE TB_HNS
                SET USEYN = CASE WHEN {(isUsing ? "1" : "0")} = 1 THEN '1' ELSE '0' END
                WHERE OBSIDX = {obsId} AND BOARDIDX = {boardId} AND HNSIDX = {sensorId};";

            bool isComplete = false;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) => { isComplete = true; },
                (error) => { errorMsg = error; isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        /// <summary>
        /// CCTV URL 업데이트 (UPDATE TB_OBS_CCTV)
        /// </summary>
        public IEnumerator UpdateCctvUrl(
            int obsId,
            CctvType cctvType,
            string url,
            Action onSuccess,
            Action<string> onError)
        {
            // ✅ 원본처럼 직접 UPDATE 쿼리 사용
            string columnName = cctvType == CctvType.Equipment ? "IN_CCTVURL" : "OUT_CCTVURL";
            string query = $@"UPDATE TB_OBS_CCTV
                SET {columnName} = '{url}'
                WHERE OBSIDX = {obsId};";

            bool isComplete = false;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) => { isComplete = true; },
                (error) => { errorMsg = error; isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        /// <summary>
        /// 센서 임계값 업데이트 (경계값/경보값)
        /// </summary>
        public IEnumerator UpdateSensorThreshold(
            int obsId,
            int boardId,
            int sensorId,
            string columnName, // "ALAHIVAL" 또는 "ALAHIHIVAL"
            float value,
            Action onSuccess,
            Action<string> onError)
        {
            string query = $@"UPDATE TB_HNS 
                SET {columnName} = {value}
                WHERE OBSIDX = {obsId} AND BOARDIDX = {boardId} AND HNSIDX = {sensorId};";

            bool isComplete = false;
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) => { isComplete = true; },
                (error) => { errorMsg = error; isComplete = true; }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        /// <summary>
        /// 센서 USEYN 조회 (알람 필터링용)
        /// </summary>
        public IEnumerator GetSensorUseyn(
            int obsId,
            int boardId,
            int sensorId,
            Action<bool> onSuccess,
            Action<string> onError)
        {
            string query = $@"
        SELECT USEYN 
        FROM TB_HNS 
        WHERE OBSIDX = {obsId} 
          AND BOARDIDX = {boardId} 
          AND HNSIDX = {sensorId};";

            bool isComplete = false;
            bool isUsing = true;  // 기본값: 사용 중
            string errorMsg = null;

            Database.ExecuteQuery(query,
                (response) =>
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                        if (result != null && result.Count > 0)
                        {
                            string useyn = result[0]["USEYN"]?.ToString().Trim();
                            isUsing = (useyn == "1");
                            Debug.Log($"[SettingsRepository] USEYN 조회: ObsId={obsId}, BoardId={boardId}, SensorId={sensorId} → {useyn}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMsg = $"USEYN 파싱 실패: {ex.Message}";
                    }
                    isComplete = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    isComplete = true;
                }
            );

            while (!isComplete) yield return null;

            if (string.IsNullOrEmpty(errorMsg))
            {
                onSuccess?.Invoke(isUsing);
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
        }

        #endregion

        #region 파싱 헬퍼

        private ObservatorySettingData ParseObservatorySettings(List<SettingModel> models, int obsId)
        {
            var setting = new ObservatorySettingData
            {
                ObsId = obsId
            };

            foreach (var model in models)
            {
                int boardId = model.BOARDIDX;

                if (boardId == 1) // 독성도
                {
                    setting.ToxinWarningValue = model.ALAHIHIVAL ?? 0f;
                    setting.ToxinBoardFixed = model.INSPECTIONFLAG?.Trim() == "1";  // ⭐ Trim() 추가
                }
                else if (boardId == 2) // 화학물질
                {
                    if (model.HNSIDX <= 19)
                    {
                        setting.ChemicalSensors.Add(new SensorSettingData
                        {
                            SensorId = model.HNSIDX,
                            BoardId = boardId,
                            SensorName = model.HNSNM,
                            IsUsing = model.USEYN?.Trim() == "1",  // ⭐ Trim() 추가!
                            WarningValue = model.ALAHIHIVAL ?? 0f,
                            SeriousValue = model.ALAHIVAL ?? 0f
                        });
                    }
                }
                else if (boardId == 3) // 수질
                {
                    if (model.HNSIDX <= 7)
                    {
                        setting.QualitySensors.Add(new SensorSettingData
                        {
                            SensorId = model.HNSIDX,
                            BoardId = boardId,
                            SensorName = model.HNSNM,
                            IsUsing = model.USEYN?.Trim() == "1",  // ⭐ Trim() 추가!
                            WarningValue = model.ALAHIHIVAL ?? 0f,
                            SeriousValue = model.ALAHIVAL ?? 0f
                        });
                    }
                }
            }

            // 보드 고정 상태도 Trim() 추가
            var chemBoard = models.Find(m => m.BOARDIDX == 2);
            if (chemBoard != null)
                setting.ChemicalBoardFixed = chemBoard.INSPECTIONFLAG?.Trim() == "1";  // ⭐ Trim()

            var qualityBoard = models.Find(m => m.BOARDIDX == 3);
            if (qualityBoard != null)
                setting.QualityBoardFixed = qualityBoard.INSPECTIONFLAG?.Trim() == "1";  // ⭐ Trim()

            return setting;
        }

        private List<AreaData> ParseAreas(List<AreaModel> models)
        {
            var areas = new List<AreaData>();

            foreach (var model in models)
            {
                areas.Add(new AreaData
                {
                    areaId = model.AREAIDX,
                    areaName = model.AREANM,
                    areaType = (AreaData.AreaType)model.AREATYPE
                });
            }

            return areas;
        }

        private List<ObservatoryData> ParseObservatories(List<ObservatoryModel> models, int areaId)
        {
            var observatories = new List<ObservatoryData>();

            foreach (var model in models)
            {
                if (model.AREAIDX == areaId)
                {
                    observatories.Add(new ObservatoryData
                    {
                        ObsId = model.OBSIDX,
                        ObsName = model.OBSNM,
                        AreaId = model.AREAIDX
                    });
                }
            }

            return observatories;
        }

        #endregion
    }
}