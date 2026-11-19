using BusJam.Core;
using BusJam.Save;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Reusable unscaled celebration and reward transfer.</summary>
    public sealed class CompletePanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button nextButton;
        [SerializeField] private CoinCounter coinCounter;
        [SerializeField] private Image dim;
        [SerializeField] private RectTransform header, rays, pile;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Image[] confetti, sparkles, coins;
        [SerializeField, Min(0)] private int coinReward = 25;
        [SerializeField] private float confettiDuration = 1.4f, dimStart = .15f, dimDuration = .25f;
        [SerializeField] private float headerStart = .3f, headerDuration = .35f;
        [SerializeField] private float pileStart = .7f, pileDuration = .3f, countDuration = .9f;
        [SerializeField] private float buttonStart = 1.1f, buttonDuration = .3f, idleDuration = .7f;
        [SerializeField] private float sparkleDuration = .6f, rayPulseDuration = .9f, rayDegreesPerSecond = 20;
        [SerializeField] private float flightDuration = .6f, flightStagger = .05f, flightArc = 170;
        private Sequence entry, ambient, transfer;
        private int shownReward = -1, startCoins;
        private bool collecting, credited;
        private readonly Vector3[] flightStarts = new Vector3[10];
        private readonly Vector3[] flightEnds = new Vector3[10];

        private void Awake()
        {
            nextButton.onClick.AddListener(Continue);
            Build();
        }
        private Sequence Reusable() => DOTween.Sequence().SetUpdate(true).SetAutoKill(false).Pause();
        private void Build()
        {
            entry = Reusable();
            for (int i = 0; i < confetti.Length; i++)
            {
                var image = confetti[i];
                var rect = image.rectTransform;
                float spread = (i - 19.5f) / 19.5f;
                var apex = new Vector2(spread * 450, 480 + i % 5 * 50);
                var end = new Vector2(spread * 560, -750 - i % 4 * 50);
                var piece = DOTween.Sequence();
                piece.Append(rect.DOAnchorPos(apex, confettiDuration * .35f).SetEase(Ease.OutQuad));
                piece.Append(rect.DOAnchorPos(end, confettiDuration * .65f).SetEase(Ease.InQuad));
                entry.Insert(0, piece);
                entry.Insert(0, rect.DOLocalRotate(new Vector3(0, 0, (i % 2 == 0 ? 1 : -1) * 720), confettiDuration, RotateMode.FastBeyond360));
                entry.Insert(confettiDuration * .5f, image.DOFade(0, confettiDuration * .5f));
            }
            entry.Insert(dimStart, dim.DOFade(.65f, dimDuration));
            entry.Insert(headerStart, header.DOScale(1, headerDuration).SetEase(Ease.OutBack, 1.7f));
            entry.Insert(pileStart, pile.DOScale(1, pileDuration).SetEase(Ease.OutBack));
            entry.Insert(pileStart, DOVirtual.Float(0, coinReward, countDuration, v => SetReward(Mathf.RoundToInt(v))).SetEase(Ease.Linear));
            entry.Insert(buttonStart, nextButton.transform.DOScale(1, buttonDuration).SetEase(Ease.OutBack));
            entry.InsertCallback(pileStart, () => ambient.Restart());
            entry.InsertCallback(buttonStart + buttonDuration, () => { nextButton.interactable = true; buttonIdle.Restart(); });
            ambient = Reusable().SetLoops(-1);
            ambient.AppendInterval(idleDuration * 2);
            buttonIdle = nextButton.transform.DOScale(1.05f, idleDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetAutoKill(false).Pause();
            rayRotation = rays.DOLocalRotate(new Vector3(0, 0, 360), 360 / rayDegreesPerSecond, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetAutoKill(false).Pause();
            rayPulse = rays.GetComponent<Image>().DOFade(.35f, rayPulseDuration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetAutoKill(false).Pause();
            // Rotation uses a separate reusable infinite tween, owned by the same panel.
            for (int i = 0; i < sparkles.Length; i++)
            {
                ambient.Insert(i * .08f, sparkles[i].DOFade(.15f, sparkleDuration).SetLoops(2, LoopType.Yoyo));
            }
            transfer = Reusable();
            for (int i = 0; i < coins.Length; i++)
            {
                int index = i;
                transfer.Insert(i * flightStagger, DOVirtual.Float(0, 1, flightDuration, t =>
                {
                    var point = Vector3.Lerp(flightStarts[index], flightEnds[index], t);
                    point.y += Mathf.Sin(t * Mathf.PI) * flightArc * transform.lossyScale.y;
                    coins[index].rectTransform.position = point;
                    coins[index].gameObject.SetActive(t < 1);
                }).SetEase(Ease.InOutSine).OnComplete(() => coinCounter.Arrive((int)System.Math.Min(int.MaxValue, (long)startCoins + (long)coinReward * (index + 1) / coins.Length))));
            }
            transfer.OnComplete(() =>
            {
                if (!CreditReward()) return;
                gameManager.LoadNext();
            });
        }
        private bool CreditReward()
        {
            if (credited) return false;
            credited = true;
            SaveSystem.AddCoins(coinReward);
            coinCounter.Show(SaveSystem.Coins);
            return true;
        }
        private Tween rayRotation, rayPulse, buttonIdle;
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            ResetPose();
            entry.Restart();
            rayRotation.Restart();
            rayPulse.Restart();
        }
        private void SetReward(int value)
        {
            if (shownReward == value) return;
            shownReward = value;
            rewardText.SetText("+{0}", value);
        }
        private void ResetPose()
        {
            collecting = credited = false;
            nextButton.interactable = false;
            header.localScale = pile.localScale = nextButton.transform.localScale = Vector3.zero;
            dim.color = new Color(26 / 255f, 11 / 255f, 46 / 255f, 0);
            rays.localRotation = Quaternion.identity;
            var rayImage = rays.GetComponent<Image>(); rayImage.color = new Color(1, 1, 1, .7f);
            shownReward = -1; SetReward(0);
            for (int i = 0; i < confetti.Length; i++)
            {
                var image = confetti[i]; var color = image.color; color.a = 1; image.color = color;
                image.rectTransform.anchoredPosition = new Vector2(0, 350);
                image.rectTransform.localRotation = Quaternion.identity;
            }
            foreach (var image in sparkles) image.color = Color.white;
            foreach (var image in coins) image.gameObject.SetActive(false);
        }
        private void Continue()
        {
            if (collecting) return;
            collecting = true; nextButton.interactable = false;
            startCoins = SaveSystem.Coins;
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < coins.Length; i++)
            {
                flightStarts[i] = pile.position;
                flightEnds[i] = coinCounter.transform.position;
                coins[i].rectTransform.position = pile.position;
            }
            transfer.Restart();
        }
        private void OnDisable()
        {
            entry?.Rewind(); ambient?.Rewind(); transfer?.Rewind();
            rayRotation?.Rewind(); rayPulse?.Rewind(); buttonIdle?.Rewind();
            if (confetti != null) ResetPose();
        }
        private void OnDestroy() { entry?.Kill(); ambient?.Kill(); transfer?.Kill(); rayRotation?.Kill(); rayPulse?.Kill(); buttonIdle?.Kill(); }
        public void PreviewFinalPose()
        {
            ResetPose();
            header.localScale = pile.localScale = nextButton.transform.localScale = Vector3.one;
            dim.color = new Color(26 / 255f, 11 / 255f, 46 / 255f, .65f);
            foreach (var image in confetti) { var c = image.color; c.a = 0; image.color = c; }
            SetReward(coinReward); nextButton.interactable = true;
        }
    }
}
