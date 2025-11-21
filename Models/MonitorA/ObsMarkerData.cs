using HNS.Common.Models;
using UnityEngine;

namespace Assets.Scripts_refactoring.Models.MonitorA
{
    public class ObsMarkerData
    {
        public int ObsId { get; set; }
        public string ObsName { get; set; }
        public ToxinStatus Status { get; set; }
        public Vector3 LocalPosition { get; set; }
    }
}