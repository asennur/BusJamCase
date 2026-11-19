using BusJam.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Shows the failure reason and retries without charging another life.</summary>
    public sealed class FailPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text reasonText, levelText;
        [SerializeField] private Button retryButton, closeButton;
        [SerializeField] private Image dim, reasonIcon;
        [SerializeField] private UIArt art;
        [SerializeField] private RectTransform panel;
        [SerializeField] private GameObject minusOne;
        [SerializeField] private float dimDuration = .2f, entryDuration = .25f, punchDuration = .35f;
        private Sequence entry;
        private void Awake()
        {
            retryButton.onClick.AddListener(gameManager.Retry);
            closeButton.onClick.AddListener(gameManager.Retry);
        }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            Show(gameManager.FailReason, gameManager.CurrentLevelNumber);
            panel.localScale = Vector3.one * .8f;
            var c = dim.color; c.a = 0; dim.color = c;
            entry = DOTween.Sequence().SetUpdate(true);
            entry.Insert(0, dim.DOFade(.65f, dimDuration));
            entry.Insert(0, panel.DOScale(1, entryDuration).SetEase(Ease.OutBack));
            entry.Insert(entryDuration, reasonIcon.transform.DOPunchScale(Vector3.one * .2f, punchDuration, 3));
        }
        public void Show(FailReason reason, int level)
        {
            reasonText.text = reason == FailReason.OutOfTime ? "Time's up!" : reason == FailReason.NoMoves ? "No moves left!" : "No space left!";
            levelText.SetText("Level {0}", level);
            reasonIcon.sprite = reason == FailReason.OutOfTime ? art.icon_clock : art.icon_heart_broken;
            minusOne.SetActive(reason != FailReason.OutOfTime);
            panel.localScale = reasonIcon.transform.localScale = Vector3.one;
            dim.color = new Color(26 / 255f, 11 / 255f, 46 / 255f, .65f);
        }
        private void OnDisable() { entry?.Kill(); if (reasonIcon != null) reasonIcon.transform.localScale = Vector3.one; }
    }
}
