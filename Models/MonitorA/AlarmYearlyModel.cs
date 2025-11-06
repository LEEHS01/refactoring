using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// GET_ALARM_YEARLY 프로시저 결과 모델
    /// </summary>
    [Serializable]
    public class AlarmYearlyModel
    {
        public string areanm;  // 지역명 (AREANM)
        public int ala0;       // 설비이상 개수
        public int ala1;       // 경계 개수
        public int ala2;       // 경보 개수
    }
}