using UnityEngine;

namespace Jingzhou.Battle.Presentation
{
    public static class BattleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<BattlePresenter>() != null)
                return;
#else
            if (Object.FindObjectOfType<BattlePresenter>() != null)
                return;
#endif
            GameObject host = new GameObject("Jingzhou Battle");
            host.AddComponent<BattlePresenter>();
        }
    }
}
