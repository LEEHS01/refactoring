using System;

namespace Models
{
    [Serializable]
    public class AlarmLogData
    {
        public int logId;
        public int obsId;
        public int sensorId;
        public int boardId;
        public int status;
        public DateTime time;
        public DateTime? cancelTime;
        public bool isCancelled;
        public string areaName;
        public string obsName;
        public string sensorName;
        public float? alarmValue;
        public float? warningThreshold;   // 경계 임계값 (ALAHIVAL)
        public float? criticalThreshold;  // 경보 임계값 (ALAHIHIVAL)

        public AlarmLogData()
        {
            logId = 0;
            obsId = 0;
            sensorId = 0;
            boardId = 0;
            status = 0;
            time = DateTime.MinValue;
            cancelTime = null;
            isCancelled = false;
            areaName = "";
            obsName = "";
            sensorName = "";
            alarmValue = null;
            warningThreshold = null;  
            criticalThreshold = null;
        }

        public static AlarmLogData FromModel(AlarmLogModel model)
        {
            return new AlarmLogData
            {
                logId = model.ALAIDX,
                obsId = model.OBSIDX,
                sensorId = model.HNSIDX,
                boardId = model.BOARDIDX,
                status = model.ALACODE,
                time = model.ALADT,
                cancelTime = model.TURNOFF_DT,
                isCancelled = !string.IsNullOrEmpty(model.TURNOFF_FLAG) && model.TURNOFF_FLAG.Trim() == "Y",
                areaName = model.AREANM ?? "",
                obsName = model.OBSNM ?? "",
                sensorName = model.HNSNM ?? "",
                alarmValue = model.CURRVAL,
                warningThreshold = model.ALAHIVAL,      // ⭐ 추가
                criticalThreshold = model.ALAHIHIVAL    // ⭐ 추가
            };
        }

    }
}