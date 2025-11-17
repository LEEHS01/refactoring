using HNS.MonitorA.ViewModels;
using UnityEngine;
using UnityEngine.UI;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// MonitorCanvasA > Image (배경)에 부착
    /// 3D 화면 전환 시 배경 투명/복원 제어
    /// </summary>
    public class BackgroundController : MonoBehaviour
    {
        private Image _backgroundImage;

        private void Awake()
        {
            _backgroundImage = GetComponent<Image>();

            if (_backgroundImage == null)
            {
                Debug.LogError("[BackgroundController] Image 컴포넌트를 찾을 수 없습니다!");
            }
        }

        private void OnEnable()
        {
            // ViewModel 이벤트 구독
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
            }
        }

        private void OnDisable()
        {
            // ViewModel 이벤트 구독 해제
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }
        }

        /// <summary>
        /// 3D 화면 표시 - 배경 투명
        /// </summary>
        private void OnObservatoryLoaded(int obsId)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = new Color(1f, 1f, 1f, 0f);
                Debug.Log("[BackgroundController] 배경 투명 처리");
            }
        }

        /// <summary>
        /// 지도 화면 복귀 - 배경 복원
        /// </summary>
        private void OnObservatoryClosed()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.white;
                Debug.Log("[BackgroundController] 배경 복원");
            }
        }
    }
}