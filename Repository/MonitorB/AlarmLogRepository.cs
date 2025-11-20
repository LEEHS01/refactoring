using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;


namespace Repositories.MonitorB
{
    /// <summary>
    /// 알람 로그 데이터 Repository
    /// - 과거 알람 로그 조회
    /// - 시간 범위 기반 알람 변경사항 조회
    /// </summary>
    public class AlarmLogRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 과거 알람 로그 전체 조회
        /// </summary>
        public IEnumerator GetHistoricalAlarmLogs(
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            yield return Database.ExecuteProcedure<AlarmLogModel>(
                "GET_HISTORICAL_ALARM_LOG",
                null,  // 파라미터 없음
                onSuccess,
                onError
            );
        }

        /// <summary>
        /// 시간 범위 내 알람 변경사항 조회
        /// - 신규 알람 발생
        /// - 알람 해제 (TURNOFF_FLAG 변경)
        /// </summary>
        public IEnumerator GetAlarmLogsChangedInRange(
            DateTime fromDt,
            DateTime toDt,
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            // 파라미터 설정
            var parameters = new Dictionary<string, object>
            {
                { "@fromDt", fromDt.ToString("yyyy-MM-dd HH:mm:ss") },
                { "@toDt", toDt.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            Debug.Log($"[AlarmLogRepository] 알람 변경사항 조회: {fromDt:yyyy-MM-dd HH:mm:ss} ~ {toDt:yyyy-MM-dd HH:mm:ss}");

            // 기존 프로시저 사용
            yield return Database.ExecuteProcedure<AlarmLogModel>(
                "GET_HISTORICAL_ALARM_LOG",
                parameters,
                logs =>
                {
                    // 시간 범위 필터링
                    if (logs != null)
                    {
                        // ⭐ ALADT는 이미 DateTime 타입이므로 직접 비교
                        var filteredLogs = logs.FindAll(log =>
                            log.ALADT >= fromDt && log.ALADT <= toDt
                        );

                        Debug.Log($"[AlarmLogRepository] 조회 결과: {filteredLogs.Count}개 알람");
                        onSuccess?.Invoke(filteredLogs);
                    }
                    else
                    {
                        onSuccess?.Invoke(new List<AlarmLogModel>());
                    }
                },
                onError
            );
        }

        /// <summary>
        /// 특정 관측소의 알람 로그 조회
        /// </summary>
        public IEnumerator GetAlarmLogsByObservatory(
            int obsId,
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@obsId", obsId }
            };

            yield return Database.ExecuteProcedure<AlarmLogModel>(
                "GET_HISTORICAL_ALARM_LOG",
                parameters,
                logs =>
                {
                    // 관측소 ID로 필터링
                    if (logs != null)
                    {
                        var filteredLogs = logs.FindAll(log => log.OBSIDX == obsId);
                        Debug.Log($"[AlarmLogRepository] 관측소 {obsId} 알람: {filteredLogs.Count}개");
                        onSuccess?.Invoke(filteredLogs);
                    }
                    else
                    {
                        onSuccess?.Invoke(new List<AlarmLogModel>());
                    }
                },
                onError
            );
        }

        /// <summary>
        /// 활성 알람만 조회 (해제되지 않은 알람)
        /// </summary>
        public IEnumerator GetActiveAlarmLogs(
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            yield return GetHistoricalAlarmLogs(
                logs =>
                {
                    // TURNOFF_FLAG가 'Y'가 아닌 알람만 필터링
                    if (logs != null)
                    {
                        var activeLogs = logs.FindAll(log =>
                            string.IsNullOrEmpty(log.TURNOFF_FLAG) ||
                            log.TURNOFF_FLAG.Trim() != "Y"
                        );

                        Debug.Log($"[AlarmLogRepository] 활성 알람: {activeLogs.Count}개");
                        onSuccess?.Invoke(activeLogs);
                    }
                    else
                    {
                        onSuccess?.Invoke(new List<AlarmLogModel>());
                    }
                },
                onError
            );
        }

        /// <summary>
        /// 해제된 알람만 조회
        /// </summary>
        public IEnumerator GetCancelledAlarmLogs(
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            yield return GetHistoricalAlarmLogs(
                logs =>
                {
                    // TURNOFF_FLAG가 'Y'인 알람만 필터링
                    if (logs != null)
                    {
                        var cancelledLogs = logs.FindAll(log =>
                            !string.IsNullOrEmpty(log.TURNOFF_FLAG) &&
                            log.TURNOFF_FLAG.Trim() == "Y"
                        );

                        Debug.Log($"[AlarmLogRepository] 해제된 알람: {cancelledLogs.Count}개");
                        onSuccess?.Invoke(cancelledLogs);
                    }
                    else
                    {
                        onSuccess?.Invoke(new List<AlarmLogModel>());
                    }
                },
                onError
            );
        }

        /// <summary>
        /// 특정 상태의 알람 조회
        /// </summary>
        /// <param name="status">0: 설비이상, 1: 경계, 2: 경보</param>
        public IEnumerator GetAlarmLogsByStatus(
            int status,
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            yield return GetHistoricalAlarmLogs(
                logs =>
                {
                    // 상태로 필터링
                    if (logs != null)
                    {
                        var filteredLogs = logs.FindAll(log => log.ALACODE == status);
                        Debug.Log($"[AlarmLogRepository] 상태 {status} 알람: {filteredLogs.Count}개");
                        onSuccess?.Invoke(filteredLogs);
                    }
                    else
                    {
                        onSuccess?.Invoke(new List<AlarmLogModel>());
                    }
                },
                onError
            );
        }
    }
}