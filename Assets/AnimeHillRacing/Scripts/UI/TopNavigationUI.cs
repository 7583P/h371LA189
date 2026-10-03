using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.UI
{
    public enum TopNavigationSection
    {
        None,
        Adventure,
        Garage,
        Shop
    }

    /// <summary>
    /// Shared navigation for the rebuilt menu scenes. GameSession remains the
    /// single source of truth for whether Garage has an Adventure track pending.
    /// </summary>
    public sealed class TopNavigationUI : MonoBehaviour
    {
        [SerializeField] private TopNavigationSection selectedSection;
        [SerializeField] private bool deriveFromRequestedGarageSection;
        [SerializeField] private GameObject adventureSelected;
        [SerializeField] private GameObject garageSelected;
        [SerializeField] private GameObject shopSelected;

        private void OnEnable()
        {
            if (deriveFromRequestedGarageSection)
                selectedSection = ResolveGarageNavigationSection(GameSession.RequestedGarageSection);
            RefreshSelection();
        }

        private void Start()
        {
            EnsureRacingTabButton();
        }

        private void EnsureRacingTabButton()
        {
            if (GameObject.Find("RACING_3D_NAV_BUTTON") != null) return;

            // Find Adventure button to clone styling
            UnityEngine.UI.Button advBtn = null;
            if (adventureSelected != null)
                advBtn = adventureSelected.GetComponentInParent<UnityEngine.UI.Button>();

            if (advBtn == null)
            {
                var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
                foreach (var b in buttons)
                {
                    if (b.name == "ADVENTURE" || b.name.Contains("Adventure"))
                    {
                        advBtn = b;
                        break;
                    }
                }
            }

            if (advBtn == null) return;

            Transform container = advBtn.transform.parent;
            GameObject racingBtnObj = Instantiate(advBtn.gameObject, container);
            racingBtnObj.name = "RACING_3D_NAV_BUTTON";

            var btn = racingBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Racing);
            }

            var texts = racingBtnObj.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var txt in texts)
            {
                if (txt.text.ToUpperInvariant().Contains("ADVENTURE"))
                {
                    txt.text = "RACING 3D";
                    txt.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
            }

            foreach (Transform child in racingBtnObj.transform)
            {
                if (child.name.ToLowerInvariant().Contains("selected") || child.name.ToLowerInvariant().Contains("active"))
                    child.gameObject.SetActive(false);
            }

            RectTransform advRt = advBtn.GetComponent<RectTransform>();
            RectTransform racingRt = racingBtnObj.GetComponent<RectTransform>();

            RectTransform garageRt = null;
            RectTransform shopRt = null;
            foreach (Transform child in container)
            {
                if (child == advBtn.transform || child == racingBtnObj.transform) continue;
                if (child.name.Contains("GARAGE") || child.name.Contains("Garage")) garageRt = child as RectTransform;
                if (child.name.Contains("SHOP") || child.name.Contains("Shop") || child.name.Contains("Store")) shopRt = child as RectTransform;
            }

            float scale = 0.82f;
            advRt.localScale = new Vector3(scale, scale, 1f);
            racingRt.localScale = new Vector3(scale, scale, 1f);
            if (garageRt != null) garageRt.localScale = new Vector3(scale, scale, 1f);
            if (shopRt != null) shopRt.localScale = new Vector3(scale, scale, 1f);

            advRt.anchoredPosition = new Vector2(-360f, advRt.anchoredPosition.y);
            racingRt.anchoredPosition = new Vector2(-120f, advRt.anchoredPosition.y);
            if (garageRt != null) garageRt.anchoredPosition = new Vector2(120f, advRt.anchoredPosition.y);
            if (shopRt != null) shopRt.anchoredPosition = new Vector2(360f, advRt.anchoredPosition.y);
        }

        public void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();
            SceneManager.LoadScene("PreStart");
        }

        public void Racing()
        {
            GameSession.ClearGarageContext();
            SceneManager.LoadScene("RacingSelect");
        }

        public void Garage()
        {
            // A selected top tab is a no-op. This is especially important in the
            // Adventure preparation Garage: re-requesting a direct Garage would
            // intentionally clear the pending track and make START disappear.
            if (SceneManager.GetActiveScene().name == "Garage" &&
                selectedSection == TopNavigationSection.Garage)
                return;

            GameSession.RequestDirectGarage(GarageSection.Vehicle);
            SceneManager.LoadScene("Garage");
        }

        public void Shop()
        {
            GameSession.RequestDirectGarage(GarageSection.Store);
            SceneManager.LoadScene("Garage");
        }

        public void Settings()
        {
            if (SceneManager.GetActiveScene().name == "Garage")
            {
                GarageUI garage = Object.FindFirstObjectByType<GarageUI>();
                if (garage != null)
                {
                    // GarageUI preserves and restores the current section. Do not call
                    // RequestDirectGarage here because it also clears a pending Adventure track.
                    garage.ShowSettings();
                    return;
                }
            }

            GameSession.RequestDirectGarage(GarageSection.Settings);
            SceneManager.LoadScene("Garage");
        }

        public void SetSelectedSection(TopNavigationSection section)
        {
            if (selectedSection == section)
            {
                RefreshSelection();
                return;
            }

            selectedSection = section;
            RefreshSelection();
        }

        public void Assign(TopNavigationSection section, GameObject adventureMarker,
            GameObject garageMarker, GameObject shopMarker,
            bool followRequestedGarageSection = false)
        {
            selectedSection = section;
            deriveFromRequestedGarageSection = followRequestedGarageSection;
            adventureSelected = adventureMarker;
            garageSelected = garageMarker;
            shopSelected = shopMarker;
            RefreshSelection();
        }

        public void SetGarageSection(GarageSection section)
        {
            selectedSection = ResolveGarageNavigationSection(section);
            RefreshSelection();
        }

        private static TopNavigationSection ResolveGarageNavigationSection(GarageSection section)
        {
            return section == GarageSection.Store
                ? TopNavigationSection.Shop
                : section == GarageSection.Settings
                    ? TopNavigationSection.None
                    : TopNavigationSection.Garage;
        }

        private void RefreshSelection()
        {
            if (adventureSelected != null)
                adventureSelected.SetActive(selectedSection == TopNavigationSection.Adventure);
            if (garageSelected != null)
                garageSelected.SetActive(selectedSection == TopNavigationSection.Garage);
            if (shopSelected != null)
                shopSelected.SetActive(selectedSection == TopNavigationSection.Shop);
        }
    }
}
