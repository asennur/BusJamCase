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
        // Confetti bursts across the whole screen first; the texts arrive once the pieces start to fall.
        [SerializeField] private float confettiBurst = .45f, confettiLifetime = 2.4f, dimStart = .1f, dimDuration = .25f;
        [SerializeField] private float headerDelay = .45f, headerDuration = .35f;
        [SerializeField] private float pileDelay = .8f, pileDuration = .3f;
        [SerializeField] private float buttonDelay = 1.25f, buttonDuration = .3f, idleDuration = .7f;
        [SerializeField] private float sparkleDuration = .6f, rayPulseDuration = .9f, rayDegreesPerSecond = 20;
        [SerializeField] private float flightDuration = .6f, flightStagger = .05f, flightArc = 170;
        private Sequence entry, ambient, transfer;
        private int shownReward = -1;
        private bool collecting, credited;
        private readonly Vector3[] flightStarts = new Vector3[10];
        private readonly Vector3[] flightEnds = new Vector3[10];
        private Vector2[] confettiStarts;

        private void Awake()
        {
            nextButton.onClick.AddListener(Continue);
            PlaceConfettiBehindHeader();
            Build();
        }
        // Texts must read on top of the falling pieces: confetti sits just below the header in draw order.
        private void PlaceConfettiBehindHeader()
        {
            if (confetti == null || confetti.Length == 0) return;
            Transform parent = confetti[0].transform.parent;
            if (parent == header.parent)
            {
                for (int i = 0; i < confetti.Length; i++)
                    confetti[i].transform.SetSiblingIndex(header.GetSiblingIndex());
            }
            else if (parent.parent == header.parent)
                parent.SetSiblingIndex(header.GetSiblingIndex());
        }

        private Sequence Reusable() => DOTween.Sequence().SetUpdate(true).SetAutoKill(false).Pause();
        private void Build()
        {
            entry = Reusable();
            // Burst points cover the whole panel; positions are fixed once so replays allocate nothing.
            Vector2 size = ((RectTransform)transform).rect.size;
            if (size.x < 10f || size.y < 10f) size = new Vector2(1080f, 1920f);
            confettiStarts = new Vector2[confetti.Length];
            float fall = Mathf.Max(.1f, confettiLifetime - confettiBurst);
            for (int i = 0; i < confetti.Length; i++)
            {
                var image = confetti[i];
                var rect = image.rectTransform;
                var start = new Vector2(Random.Range(-.45f, .45f) * size.x, Random.Range(-.3f, .4f) * size.y);
                confettiStarts[i] = start;
                var burst = start + Random.insideUnitCircle.normalized * Random.Range(140f, 280f);
                var end = new Vector2(burst.x + Random.Range(-160f, 160f), -.6f * size.y - Random.Range(0f, 200f));
                var piece = DOTween.Sequence();
                piece.Append(rect.DOAnchorPos(burst, confettiBurst).SetEase(Ease.OutCubic));
                piece.Append(rect.DOAnchorPos(end, fall).SetEase(Ease.InQuad));
                entry.Insert(0, piece);
                entry.Insert(0, rect.DOScale(Random.Range(.8f, 1.25f), confettiBurst * .6f).SetEase(Ease.OutBack));
                entry.Insert(0, rect.DOLocalRotate(new Vector3(0, 0, (i % 2 == 0 ? 1 : -1) * Random.Range(540f, 900f)), confettiLifetime, RotateMode.FastBeyond360));
                entry.Insert(confettiLifetime * .65f, image.DOFade(0, confettiLifetime * .35f));
            }
            entry.Insert(dimStart, dim.DOFade(.65f, dimDuration));
            entry.Insert(headerDelay, header.DOScale(1, headerDuration).SetEase(Ease.OutBack, 1.7f));
            entry.Insert(pileDelay, pile.DOScale(1, pileDuration).SetEase(Ease.OutBack));
            entry.Insert(pileDelay, rewardText.transform.DOScale(1, pileDuration).SetEase(Ease.OutBack));
            entry.Insert(buttonDelay, nextButton.transform.DOScale(1, buttonDuration).SetEase(Ease.OutBack));
            entry.InsertCallback(pileDelay, () => ambient.Restart());
            entry.InsertCallback(buttonDelay + buttonDuration, () => { nextButton.interactable = true; buttonIdle.Restart(); });
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
                }).SetEase(Ease.InOutSine));
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
            coinCounter.Arrive(SaveSystem.Coins);
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
            shownReward = -1; SetReward(coinReward);
            rewardText.transform.localScale = Vector3.zero;
            for (int i = 0; i < confetti.Length; i++)
            {
                var image = confetti[i]; var color = image.color; color.a = 1; image.color = color;
                image.rectTransform.anchoredPosition = confettiStarts != null ? confettiStarts[i] : Vector2.zero;
                image.rectTransform.localRotation = Quaternion.identity;
                image.rectTransform.localScale = Vector3.zero;
            }
            foreach (var image in sparkles) image.color = Color.white;
            foreach (var image in coins) image.gameObject.SetActive(false);
        }
        private void Continue()
        {
            if (collecting) return;
            collecting = true; nextButton.interactable = false;
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
            rewardText.transform.localScale = Vector3.one;
        }
    }
}
