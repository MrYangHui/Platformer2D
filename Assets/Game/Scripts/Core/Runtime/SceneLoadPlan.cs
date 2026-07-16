using System;
using System.Collections.Generic;

namespace SnowbreakFan.Core
{
    public static class SceneLoadPlan
    {
        public static IReadOnlyList<string> Create(string levelScene, string uiScene)
        {
            if (string.IsNullOrWhiteSpace(levelScene))
                throw new ArgumentException("Level scene name is required.", nameof(levelScene));
            if (string.IsNullOrWhiteSpace(uiScene))
                throw new ArgumentException("UI scene name is required.", nameof(uiScene));
            if (string.Equals(levelScene, uiScene, StringComparison.Ordinal))
                throw new ArgumentException("Level and UI scenes must be different.");

            return new[] { levelScene, uiScene };
        }
    }
}
