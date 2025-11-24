using UnityEngine;
using UnityEngine.UI;
using HNS.MonitorA.ViewModels;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 기계 정보 버튼 View (장비 Canvas의 Button)
    /// </summary>
    public class MachineInfoButtonView : MonoBehaviour
    {
        private Button btnMachineInfo;

        private void Start()
        {
            // Button 컴포넌트 찾기
            btnMachineInfo = GetComponent<Button>();
            if (btnMachineInfo == null)
            {
                btnMachineInfo = GetComponentInChildren<Button>();
            }

            if (btnMachineInfo != null)
            {
                btnMachineInfo.onClick.AddListener(OnClick);
                Debug.Log("[MachineInfoButtonView] 버튼 이벤트 연결 완료");
            }
            else
            {
                Debug.LogError("[MachineInfoButtonView] Button을 찾을 수 없습니다!");
            }
        }

        private void OnDestroy()
        {
            if (btnMachineInfo != null)
            {
                btnMachineInfo.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            Debug.Log("[MachineInfoButtonView] 🖱️ 기계 정보 버튼 클릭!");

            if (MachineInfoViewModel.Instance != null)
            {
                // 기본값: "00" (정상)
                MachineInfoViewModel.Instance.OpenMachineInfo("00");
            }
            else
            {
                Debug.LogError("[MachineInfoButtonView] MachineInfoViewModel.Instance가 null입니다!");
            }
        }
    }
}
