using UnityEngine;
using HNS.MonitorA.Views;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Components
{
    /// <summary>
    /// Sensors 오브젝트 클릭 처리
    /// </summary>
    public class SensorsClickHandler : MonoBehaviour
    {
        private ObsSensorStepAnimator _animator;
        private SensorStepPopupView _sensorStepPopupView;
        private MachineInfoPopupView _machineInfoPopupView;

        private void Start()
        {
            _animator = GetComponent<ObsSensorStepAnimator>();
            _sensorStepPopupView = FindObjectOfType<SensorStepPopupView>();
            _machineInfoPopupView = FindObjectOfType<MachineInfoPopupView>();

            if (_animator == null)
            {
                Debug.LogWarning("[SensorsClickHandler] ObsSensorStepAnimator를 찾을 수 없습니다!");
            }
        }

        private void OnMouseDown()
        {
            Debug.Log($"[SensorsClickHandler] 클릭됨");

            if (XrayViewModel.Instance == null)
            {
                Debug.LogError("[SensorsClickHandler] XrayViewModel.Instance가 null입니다!");
                return;
            }

            bool buildingOn = XrayViewModel.Instance.IsStructureXrayActive;
            bool equipmentOn = XrayViewModel.Instance.IsEquipmentXrayActive;

            Debug.Log($"[SensorsClickHandler] X-Ray 상태: 건물={buildingOn}, 장비={equipmentOn}");

            // ⭐ 둘 다 활성화 → 센서 단계 팝업
            if (buildingOn && equipmentOn)
            {
                if (_sensorStepPopupView != null)
                {
                    _sensorStepPopupView.ShowCurrentStepPopup();
                    Debug.Log("👆 Sensors 클릭 - 센서 단계 팝업 표시");
                }
            }
            // ⭐ 건물만 활성화 → 장비 정보 팝업
            else if (buildingOn && !equipmentOn)
            {
                if (MachineInfoViewModel.Instance != null)
                {
                    MachineInfoViewModel.Instance.OpenMachineInfo("00");
                    Debug.Log("👆 Sensors 클릭 - 장비 정보 팝업 표시");
                }
            }
            // ⭐ 둘 다 비활성화
            else
            {
                Debug.Log("❌ X-Ray가 활성화되지 않아 팝업을 표시할 수 없습니다.");
            }
        }
    }
}
