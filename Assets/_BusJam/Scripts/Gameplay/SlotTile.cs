using TMPro;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Tek bekleme slotunun görünümü: açık karo ya da kilitli karo + açılacağı level yazısı.</summary>
    public class SlotTile : MonoBehaviour
    {
        [SerializeField] private GameObject openVisual;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private TMP_Text unlockLevelText;

        private int shownUnlockLevel = -1;

        public void SetLocked(bool locked, int unlockLevel)
        {
            openVisual.SetActive(!locked);
            lockedVisual.SetActive(locked);

            if (!locked || unlockLevelText == null || unlockLevel == shownUnlockLevel)
                return;

            shownUnlockLevel = unlockLevel;
            unlockLevelText.text = "LVL " + unlockLevel;
        }
    }
}
