using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Generic
{
    public abstract class PrefabSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance => GetInstance();
        public static bool HasInstance => _instance != null;

        private static T _instance = null;
        private static bool _isDestroying = false;

        /// <summary>
        /// The path format of this prefab singleton</br>
        /// {0} = this prefab singleton's class name
        /// </summary>
        private const string _pathFormat = "Prefabs/Common/{0}";

        public virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = GetComponent<T>();

            _isDestroying = false;
            DontDestroyOnLoad(this);
            Init();
        }

        public virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isDestroying = true;
            }
        }

        private static T GetInstance()
        {
            if (_isDestroying)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<T>();

                if (FindObjectsOfType(typeof(T)).Length > 1)
                {
                    return _instance;
                }

                if (_instance == null)
                {
                    Debug.Assert(Application.isPlaying, $"Must be call on runtime");

                    string className = typeof(T).ToString();
                    string path = string.Format(_pathFormat, className);
                    Debug.LogAssertion($"{typeof(T)} does not exist, try to load from [{path}]");

                    T singleton = Instantiate(Resources.Load<T>(path));
                    Debug.Assert(singleton != null, $"prefab {typeof(T)} not found");

                    _instance = singleton;
                    singleton.name = _instance.GetType().Name;
                }
            }

            return _instance;
        }

        protected virtual void Init() { }
    }
}
