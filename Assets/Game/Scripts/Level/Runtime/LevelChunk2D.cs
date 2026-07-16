using UnityEngine;
using UnityEngine.Tilemaps;

namespace SnowbreakFan.Level
{
    public sealed class LevelChunk2D : MonoBehaviour
    {
        [SerializeField] private string chunkId;
        [SerializeField] private Tilemap gameplayTilemap;
        [SerializeField] private Tilemap terrainArtTilemap;
        [SerializeField] private Collider2D cameraBoundary;
        [SerializeField] private Transform defaultSpawn;

        public string ChunkId => chunkId;
        public Tilemap GameplayTilemap => gameplayTilemap;
        public Tilemap TerrainArtTilemap => terrainArtTilemap;
        public Collider2D CameraBoundary => cameraBoundary;
        public Transform DefaultSpawn => defaultSpawn;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(chunkId)) Debug.LogError("LevelChunk2D needs a chunk id.", this);
            if (gameplayTilemap == null) Debug.LogError($"{chunkId}: missing gameplay Tilemap.", this);
            if (terrainArtTilemap == null) Debug.LogError($"{chunkId}: missing terrain art Tilemap.", this);
            if (cameraBoundary == null) Debug.LogError($"{chunkId}: missing camera boundary.", this);
            if (defaultSpawn == null) Debug.LogError($"{chunkId}: missing default spawn.", this);
        }
    }
}
