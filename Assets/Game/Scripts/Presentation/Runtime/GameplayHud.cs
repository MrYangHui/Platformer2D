using SnowbreakFan.Collectibles;
using SnowbreakFan.Level;
using TMPro;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private CollectibleCountChannel countChannel;
        [SerializeField] private LevelCompletedChannel completedChannel;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject completedPanel;

        private void OnEnable()
        {
            countChannel.Changed += RenderCount;
            completedChannel.Raised += ShowCompleted;
            RenderCount(countChannel.Current, countChannel.Total);
            completedPanel.SetActive(false);
        }

        private void OnDisable()
        {
            countChannel.Changed -= RenderCount;
            completedChannel.Raised -= ShowCompleted;
        }

        private void RenderCount(int current, int total) => countText.text = $"样本 {current}/{total}";
        private void ShowCompleted() => completedPanel.SetActive(true);
    }
}
