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

        public void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();
            SceneManager.LoadScene("PreStart");
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
