using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace 裁缝之王
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner Instance
        {
            get
            {
                if (CoroutineRunner.isApplicationQuitting)
                {
                    Debug.LogWarning("Application is quitting, cannot create CoroutineRunner");
                    return null;
                }
                object obj = CoroutineRunner.lockObject;
                CoroutineRunner result;
                lock (obj)
                {
                    if (CoroutineRunner.instance != null)
                    {
                        result = CoroutineRunner.instance;
                    }
                    else
                    {
                        CoroutineRunner.instance = UnityEngine.Object.FindObjectOfType<CoroutineRunner>();
                        if (CoroutineRunner.instance != null)
                        {
                            result = CoroutineRunner.instance;
                        }
                        else
                        {
                            GameObject gameObject = new GameObject("CoroutineRunner");
                            CoroutineRunner.instance = gameObject.AddComponent<CoroutineRunner>();
                            UnityEngine.Object.DontDestroyOnLoad(gameObject);
                            gameObject.hideFlags = HideFlags.HideInHierarchy;
                            result = CoroutineRunner.instance;
                        }
                    }
                }
                return result;
            }
        }

        private void Awake()
        {
            if (CoroutineRunner.instance != null && CoroutineRunner.instance != this)
            {
                UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            object obj = CoroutineRunner.lockObject;
            lock (obj)
            {
                if (!(CoroutineRunner.instance != null))
                {
                    CoroutineRunner.instance = this;
                    base.hideFlags = HideFlags.HideInHierarchy;
                }
            }
        }

        private void OnDestroy()
        {
            if (CoroutineRunner.instance == this)
            {
                CoroutineRunner.instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            CoroutineRunner.isApplicationQuitting = true;
        }

        public static new Coroutine StartCoroutine(IEnumerator routine)
        {
            CoroutineRunner coroutineRunner = CoroutineRunner.Instance;
            if (!(coroutineRunner != null))
            {
                return null;
            }
            return coroutineRunner.StartCoroutineInternal(routine);
        }

        public static new void StopCoroutine(Coroutine routine)
        {
            CoroutineRunner coroutineRunner = CoroutineRunner.Instance;
            if (coroutineRunner == null)
            {
                return;
            }
            coroutineRunner.StopCoroutineInternal(routine);
        }

        private Coroutine StartCoroutineInternal(IEnumerator routine)
        {
            return base.StartCoroutine(routine);
        }

        private void StopCoroutineInternal(Coroutine routine)
        {
            base.StopCoroutine(routine);
        }

        private static CoroutineRunner instance;

        private static readonly object lockObject = new object();

        private static bool isApplicationQuitting;
    }
}
