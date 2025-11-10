using System;

namespace Assets.Scripts_refactoring.Models.MonitorA
{
    /// <summary>
    /// GET_ALARM_SUMMARY 프로시저 결과 모델
    /// DB에서 직접 받아오는 원본 데이터
    /// </summary>
    [Serializable]
    public class AlarmSummaryDbModel
    {
        public int OBSIDX;   // 관측소 인덱스
        public int YEAR;     // 년도
        public int MONTH;    // 월
        public int CNT;      // 알람 개수
    }
}