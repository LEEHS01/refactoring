using UnityEngine;
using HNS.Common.Models;

namespace HNS.MonitorA.Models
{
    /// <summary>
    /// 지도 마커 데이터 (지역별 알람 상태)
    /// </summary>
    public class MapMarkerData
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public HNS.Common.Models.AreaData.AreaType AreaType { get; set; }
        public int Status { get; set; }  // 0=Green, 1=Yellow, 2=Red, 3=Purple
    }
}