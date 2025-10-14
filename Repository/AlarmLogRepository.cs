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
    /// 알람 관련 데이터 접근 로직
    /// </summary>
    public class AlarmLogRepository
    {
        private DatabaseService Database => DatabaseService.Instance;

        /// <summary>
        /// 과거 알람 로그 조회
        /// </summary>
        public IEnumerator GetHistoricalAlarmLogs(
            Action<List<AlarmLogModel>> onSuccess,
            Action<string> onError)
        {
            yield return Database.ExecuteProcedure(
                "GET_HISTORICAL_ALARM_LOG",
                null,  // 파라미터 없음
                onSuccess,
                onError
            );
        }
    }
}