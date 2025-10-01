using System;

namespace Models
{
    [Serializable]
    public class AlarmLogModel
    {
        public int ALAIDX { get; set; }
        public int OBSIDX { get; set; }
        public int AREAIDX { get; set; }
        public int HNSIDX { get; set; }
        public int BOARDIDX { get; set; }
        public int ALACODE { get; set; }
        public DateTime ALADT { get; set; }
        public DateTime? TURNOFF_DT { get; set; }
        public int TURNOFF_FLAG { get; set; }  
        public string AREANM { get; set; }
        public string OBSNM { get; set; }
        public string HNSNM { get; set; }
        public float CURRVAL { get; set; }  // ¡ç alarmval ¡æ CURRVAL

        public AlarmLogModel()
        {
            ALAIDX = 0;
            OBSIDX = 0;
            AREAIDX = 0;
            HNSIDX = 0;
            BOARDIDX = 0;
            ALACODE = 0;
            ALADT = DateTime.MinValue;
            TURNOFF_DT = null;
            TURNOFF_FLAG = 0;
            AREANM = "";
            OBSNM = "";
            HNSNM = "";
            CURRVAL = 0f;
        }
    }
}