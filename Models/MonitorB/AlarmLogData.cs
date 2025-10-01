using System;

namespace Models
{
    [Serializable]
    public class AlarmLogData
    {
        public int logId;
        public int obsId;
        public int areaId;
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

        public AlarmLogData()
        {
            logId = 0;
            obsId = 0;
            areaId = 0;
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
        }

        public static AlarmLogData FromModel(AlarmLogModel model)
        {
            return new AlarmLogData
            {
                logId = model.ALAIDX,
                obsId = model.OBSIDX,
                areaId = model.AREAIDX,
                sensorId = model.HNSIDX,
                boardId = model.BOARDIDX,
                status = model.ALACODE,
                time = model.ALADT,
                cancelTime = model.TURNOFF_DT,
                isCancelled = model.TURNOFF_FLAG == 1,
                areaName = model.AREANM ?? "",
                obsName = model.OBSNM ?? "",
                sensorName = model.HNSNM ?? "",
                alarmValue = model.CURRVAL
            };
        }
    }
}