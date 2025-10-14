using System;
using UnityEngine;

namespace Models.MonitorB
{
    [Serializable]
    public class ChartDataPoint
    {
        [SerializeField] private string OBSDT;
        [SerializeField] private int HNSIDX;
        [SerializeField] private int BOARDIDX;
        [SerializeField] private int OBSIDX;
        [SerializeField] private float VAL;
        [SerializeField] private float AIVAL;

        public DateTime time
        {
            get
            {
                if (DateTime.TryParse(OBSDT, out DateTime result))
                    return result;
                return DateTime.Now;
            }
        }

        public int hnsIdx => HNSIDX;
        public int boardIdx => BOARDIDX;
        public int obsIdx => OBSIDX;
        public float value => VAL;
        public float aiValue => AIVAL;
    }
}