using System;

namespace Models.MonitorA
{
    /// <summary>
    /// GET_SENSOR_STEP 프로시저 결과 모델
    /// </summary>
    [Serializable]
    public class SensorStepModel
    {
        public int obsid;
        public string toxistep;
        public string chemistep;
    }
}