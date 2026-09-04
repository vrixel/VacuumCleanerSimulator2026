using UnityEngine;

namespace VCS.Core
{
    /// <summary>
    /// Entry point. The whole game is built from code at runtime, so the scene only has to exist.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }
}
