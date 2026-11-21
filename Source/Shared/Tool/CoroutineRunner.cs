using System.Collections;
using UnityEngine;

namespace 裁缝之王
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static readonly object lockObject = new();
        private static CoroutineRunner instance;
        private static bool isApplicationQuitting;

        private static CoroutineRunner Instance
        {
            get
            {
                if (isApplicationQuitting)
                {
                    Debug.LogWarning("Application is quitting, cannot create CoroutineRunner");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance) return instance;

                    instance = FindObjectOfType<CoroutineRunner>();
                    if (instance) return instance;

                    var gameObject = new GameObject("CoroutineRunner");
                    instance = gameObject.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(gameObject);
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
                    return instance;
                }
            }
        }

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            lock (lockObject)
            {
                if (!instance)
                {
                    instance = this;
                    hideFlags = HideFlags.HideInHierarchy;
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnApplicationQuit() => isApplicationQuitting = true;

        public static new Coroutine StartCoroutine(IEnumerator routine)
        {
            CoroutineRunner runner = Instance;
            return runner ? runner.StartCoroutineInternal(routine) : null;
        }

        public static new void StopCoroutine(Coroutine routine)
        {
            CoroutineRunner runner = Instance;
            if (runner) runner.StopCoroutineInternal(routine);
        }

        private Coroutine StartCoroutineInternal(IEnumerator routine) => base.StartCoroutine(routine);

        private void StopCoroutineInternal(Coroutine routine) => base.StopCoroutine(routine);
    }
}
