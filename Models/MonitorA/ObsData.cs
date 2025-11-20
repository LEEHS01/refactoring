using System;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 관측소 데이터
    /// </summary>
    [Serializable]
    public class ObsData
    {
        public int id { get; set; }
        public string obsName { get; set; }
        public int areaId { get; set; }
        public string areaName { get; set; }
        public string src_video1 { get; set; }
        public string src_video2 { get; set; }

        public ObsData()
        {
            id = -1;
            obsName = string.Empty;
            areaId = -1;
            areaName = string.Empty;
            src_video1 = string.Empty;
            src_video2 = string.Empty;
        }

        public ObsData(int id, string obsName, int areaId, string areaName, string video1, string video2)
        {
            this.id = id;
            this.obsName = obsName;
            this.areaId = areaId;
            this.areaName = areaName;
            this.src_video1 = video1;
            this.src_video2 = video2;
        }
    }
}