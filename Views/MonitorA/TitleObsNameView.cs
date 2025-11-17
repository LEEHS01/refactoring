using HNS.MonitorA.ViewModels;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    /// <summary>
    /// 3D 관측소 화면 - 관측소 이름 표시
    /// "지역1", "지역2" 등 표시
    /// </summary>
    public class TitleObsNameView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text lblObsName;
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SubscribeToViewModel();
            HideTitle();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        private void InitializeComponents()
        {
            if (lblObsName == null)
            {
                lblObsName = GetComponentInChildren<TMP_Text>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    Debug.Log("[TitleObsNameView] CanvasGroup 자동 추가");
                }
            }
        }

        #region ViewModel 이벤트 구독

        private void SubscribeToViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.AddListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.AddListener(OnObservatoryClosed);
                Debug.Log("[TitleObsNameView] Area3DViewModel 이벤트 구독");
            }
            else
            {
                Debug.LogWarning("[TitleObsNameView] Area3DViewModel.Instance가 null!");
            }
        }

        private void UnsubscribeFromViewModel()
        {
            if (Area3DViewModel.Instance != null)
            {
                Area3DViewModel.Instance.OnObservatoryLoaded.RemoveListener(OnObservatoryLoaded);
                Area3DViewModel.Instance.OnObservatoryClosed.RemoveListener(OnObservatoryClosed);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void OnObservatoryLoaded(int obsId)
        {
            // 관측소 이름 설정: "지역1", "지역2", ...
            if (lblObsName != null)
            {
                lblObsName.text = $"지역{obsId}";
                Debug.Log($"[TitleObsNameView] 관측소 이름 표시: 지역{obsId}");
            }

            ShowTitle();
        }

        private void OnObservatoryClosed()
        {
            HideTitle();
            Debug.Log("[TitleObsNameView] 관측소 이름 숨김");
        }

        #endregion

        #region 표시/숨김

        private void ShowTitle()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void HideTitle()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        #endregion
    }
}