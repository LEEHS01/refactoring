using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// GET_CURRENT_ALARM_LOG 프로시저 반환 모델
    /// </summary>
    [Serializable]
    public class AlarmLogModel
    {
        public int ALAIDX { get; set; }          // 알람 ID
        public string ALADT { get; set; }        // 알람 발생시간
        public string OBSNM { get; set; }        // 관측소명
        public string AREANM { get; set; }       // 지역명
        public int OBSIDX { get; set; }          // 관측소 ID
        public int BOARDIDX { get; set; }        // 보드 ID
        public int HNSIDX { get; set; }          // 센서 ID
        public float? CURRVAL { get; set; }      // 현재값
        public float? ALAHIHIVAL { get; set; }   // 경보 임계값
        public float? ALAHIVAL { get; set; }     // 경계 임계값
        public string HNSNM { get; set; }        // 센서명
        public int ALACODE { get; set; }         // 알람 코드 (0:설비이상, 1:경계, 2:경보)
        public string TURNOFF_FLAG { get; set; } // 해제 여부

        public AlarmLogModel()
        {
            ALAIDX = 0;
            ALADT = "";
            OBSNM = "";
            AREANM = "";
            OBSIDX = 0;
            BOARDIDX = 0;
            HNSIDX = 0;
            CURRVAL = null;
            ALAHIHIVAL = null;
            ALAHIVAL = null;
            HNSNM = "";
            ALACODE = 0;
            TURNOFF_FLAG = null;
        }
    }
}