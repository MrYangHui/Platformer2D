using System.Collections.Generic;
using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Infrastructure
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Settings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        [SerializeField] private string levelScene = "10_Level_Prototype";
        [SerializeField] private string uiScene = "20_UI_Gameplay";

        public IReadOnlyList<string> BuildLoadPlan() => SceneLoadPlan.Create(levelScene, uiScene);
    }
}
