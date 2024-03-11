using Common.Enum;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generic.UI
{
    public class WindowManager : BaseManager
    {
        [Header("Windows")]
        [SerializeField]
        private BaseWindow[] _windowPrefabs;

        [Header("Root")]
        [SerializeField]
        private Transform _normalRoot;

        [SerializeField]
        private Transform _modalRoot;

        [Header("Block Raycast")]
        [SerializeField]
        private GameObject _blockRaycastPanel;

        [Header("Visible")]
        [SerializeField]
        private CanvasGroup _visibleCanvasGroup;

        /// <summary>
        /// Minimum alpha value for window manager canvas group when HideUICanvas()
        /// </summary>
        private const float _minAlphaCanvasGroup = 0f;
        /// <summary>
        /// Maximum alpha value for window manager canvas group when ShowUICanvas()
        /// </summary>
        private const float _maxAlphaCanvasGroup = 1f;
        /// <summary>
        /// Lerp duration for canvas group
        /// </summary>
        private const float _canvasGroupLerpDuration = 0.25f;

        public event Action<BaseWindow> WhenWindowCreated;
        public event Action<BaseWindow> WhenWindowOpened;
        public event Action<BaseWindow> WhenWindowShown;
        public event Action<BaseWindow> WhenWindowHidden;
        public event Action<BaseWindow> WhenWindowClosed;
        public event Action<BaseWindow> WhenWindowFocused;
        public event Action<BaseWindow> WhenWindowBacked;
        public event Action WhenClosedAll;

        public BaseWindow FocusedWindow { get; private set; }
        public bool IsCanvasVisible { get; private set; }

        private bool _isInitialized = false;
        private List<BaseWindow> _windows;
        private Coroutine _windowRoutine;
        private Coroutine _closeAllRoutine;
        private Coroutine _canvasGroupRoutine;

        /// <summary>
        /// Initialize function to setup window manager at first
        /// </summary>
        public override void Init()
        {
            Debug.Assert(!_isInitialized, $"{gameObject.name} is already init");

            if (!_isInitialized)
            {
                _isInitialized = true;
                _windows = new List<BaseWindow>();
                FocusedWindow = null;
                IsCanvasVisible = true;
                SetBlockRaycast(true);
                DontDestroyOnLoad(this);
            }
        }

        /// <summary>
        /// Function to return amount of current window in window list
        /// </summary>
        /// <returns>count of windows as int</returns>
        public int GetWindowCount()
        {
            return _windows.Count;
        }

        /// <summary>
        /// Function to tell that T type of window is exist in window list or not
        /// </summary>
        /// <typeparam name="T">target type of window</typeparam>
        /// <returns>bool that tell T is exist in windows or not</returns>
        public bool IsWindowExist<T>() where T : BaseWindow
        {
            bool result = _windows.Any(x => x.GetType() == typeof(T));

            return result;
        }

        /// <summary>
        /// Function to get the window with same type as T
        /// </summary>
        /// <typeparam name="T">target type of window</typeparam>
        /// <returns>window with same type as T if that window is exist in window list</returns>
        public T GetWindow<T>() where T : BaseWindow
        {
            var window = _windows.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
            
            Debug.Assert(window != null, $"Window {typeof(T).Name} not found");

            return window;
        }

        /// <summary>
        /// Function to get the topmost window that's not hidden
        /// </summary>
        /// <returns>the topmost window</returns>
        public BaseWindow GetTopWindow()
        {
            BaseWindow result = null;

            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (!_windows[i].IsHidden)
                {
                    result = _windows[i];
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Function to create window<br/>
        /// T is Type of window<br/>
        /// </summary>
        /// <return>
        /// T as window with T type
        /// </return>
        public T Create<T>() where T : BaseWindow
        {
            BaseWindow prefab = _windowPrefabs.FirstOrDefault(x => x.GetType() == typeof(T));

            if (prefab == null)
            {
                Debug.LogAssertion($"Prefab of type {typeof(T)} not found in window prefabs");
                return null;
            }

            T window = Instantiate(prefab) as T;
            window.gameObject.SetActive(false);
            window.Create();
            WhenWindowCreated?.Invoke(window);

            if (IsWindowExist<T>())
            {
                Debug.LogAssertion($"Create duplicated type. This type already in window list");
            }

            return window as T;
        }

        /// <summary>
        /// Wrap up function to GET or CREATE window<br/>
        /// GET when that type is exist in window list otherwise CREATE it.<br/>
        /// </summary>
        /// <typeparam name="T">type of window you want to get</typeparam>
        /// <returns>window with T type</returns>
        public T GetOrCreate<T>() where T : BaseWindow
        {
            BaseWindow window = IsWindowExist<T>() ? GetWindow<T>() : Create<T>();

            Debug.Assert(window != null, $"cannot get or create window with {typeof(T)} type");

            return window as T;
        }

        /// <summary>
        /// Function to open window<br/>
        /// Assign window to list then add to canvas
        /// </summary>
        /// <param name="window">target window</param>
        /// <param name="isPlayAnimation">parameter to tell is play animation or not</param>
        public void Open<T>(T window, bool isPlayAnimation = true) where T : BaseWindow
        {
            if (window == null)
            {
                return;
            }

            if (IsWindowExist<T>())
            {
                Debug.LogAssertion($"Already have this {window.GetType()} type");

                return;
            }

            if (_windowRoutine != null)
            {
                SkipWindowsTransition();
                StopCoroutine(_windowRoutine);
                _windowRoutine = null;
            }

            UnfocusWindow();
            _windowRoutine = StartCoroutine(OpenRoutine(window, isPlayAnimation));
        }

        /// <summary>
        /// Function to close target window<br/>
        /// Target window must exist in window list 
        /// </summary>
        /// <param name="window">target window</param>
        /// <param name="isPlayAnimation">parameter to tell is play animation or not</param>
        public void Close<T>(T window, bool isPlayAnimation = true) where T : BaseWindow
        {
            if (window == null)
            {
                return;
            }

            if (!_windows.Contains(window) && !IsWindowExist<T>())
            {
                Debug.LogAssertion($"Window list don't have {window}");
                return;
            }

            if (_windowRoutine != null)
            {
                SkipWindowsTransition();
                StopCoroutine(_windowRoutine);
                _windowRoutine = null;
            }

            UnfocusWindow();
            _windowRoutine = StartCoroutine(CloseRoutine(window, isPlayAnimation));
        }

        /// <summary>
        /// Close all window in window list
        /// </summary>
        public void CloseAll()
        {
            if (_closeAllRoutine != null)
            {
                return;
            }

            _closeAllRoutine = StartCoroutine(CloseAllRoutine());
        }

        /// <summary>
        /// Back button to call the focus window's Back()
        /// </summary>
        public void HardwareBackButton()
        {
            if (FocusedWindow != null)
            {
                FocusedWindow.Back();
                WhenWindowBacked?.Invoke(FocusedWindow);
            }
        }

        /// <summary>
        /// Set block raycast panel active state to block raycast from mouse click
        /// </summary>
        public void SetBlockRaycast(bool isBlocking)
        {
            _blockRaycastPanel.SetActive(isBlocking);
        }

        /// <summary>
        /// Function to hide UI canvas
        /// </summary>
        public void HideUICanvas()
        {
            if (!IsCanvasVisible)
            {
                return;
            }

            IsCanvasVisible = false;

            if (_canvasGroupRoutine != null)
            {
                StopCoroutine(_canvasGroupRoutine);
            }

            _canvasGroupRoutine = StartCoroutine(LerpCanvasGroupRoutine(_maxAlphaCanvasGroup, _minAlphaCanvasGroup));
        }

        /// <summary>
        /// Function to show UI canvas
        /// </summary>
        public void ShowUICanvas()
        {
            if (IsCanvasVisible)
            {
                return;
            }

            IsCanvasVisible = true;

            if (_canvasGroupRoutine != null)
            {
                StopCoroutine(_canvasGroupRoutine);
            }

            _canvasGroupRoutine = StartCoroutine(LerpCanvasGroupRoutine(_minAlphaCanvasGroup, _maxAlphaCanvasGroup));
        }

        /// <summary>
        /// IEnumerator function to lerp canvas group from current alpha to target alpha 
        /// </summary>
        /// <param name="startAlpha">start value of canvas group's alpha</param>
        /// <param name="targetAlpha">target canvas group's alpha</param>
        private IEnumerator LerpCanvasGroupRoutine(float startAlpha, float targetAlpha)
        {
            float elapsedTime = 0f;
            _visibleCanvasGroup.alpha = startAlpha;

            while (elapsedTime <= _canvasGroupLerpDuration)
            {
                _visibleCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / _canvasGroupLerpDuration);
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            _visibleCanvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// Show other window from top until the window IsHideOther
        /// </summary>
        private IEnumerator ShowFromTop()
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                _windows[i].Showing();
                _windows[i].Show();
                WhenWindowShown?.Invoke(_windows[i]);

                if (_windows[i].IsHideOther)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Hide other window from top until the window IsHideOther or Hidden
        /// </summary>
        private IEnumerator HideFromTop()
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                _windows[i].Hidding();
                _windows[i].Hide();
                WhenWindowHidden?.Invoke(_windows[i]);

                if (_windows[i].IsHideOther)
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Bring the target window to the topmost
        /// </summary>
        /// <param name="window">target window</param>
        private void BringToTop(BaseWindow window)
        {
            window.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Get the window holder by window type
        /// </summary>
        /// <param name="windowType">target window type</param>
        /// <returns>Holder transform follow its type</returns>
        private Transform GetWindowHolder(WindowType windowType)
        {
            return windowType == WindowType.Normal ? _normalRoot : _modalRoot;
        }

        /// <summary>
        /// IEnumerator function for open window sequence<br/>
        /// This function will called by Open()
        /// </summary>
        /// <param name="window">target window</param>
        /// <param name="isPlayAnimation">parameter to tell is play animation or not</param>
        private IEnumerator OpenRoutine(BaseWindow window, bool isPlayAnimation)
        {
            SetBlockRaycast(true);

            if (window.IsHideOther)
            {
                yield return HideFromTop();
            }

            _windows.Add(window);
            Transform holder = GetWindowHolder(window.Type);
            window.gameObject.transform.SetParent(holder, false);
            BringToTop(window);

            window.Open();
            WhenWindowOpened?.Invoke(window);

            window.Showing();

            if (isPlayAnimation)
            {
                yield return window.PlayShowAnimationRoutine();
            }

            window.Show();
            WhenWindowShown?.Invoke(window);

            SetFocusWindow(window);
            _windowRoutine = null;

            SetBlockRaycast(false);
        }

        /// <summary>
        /// IEnumerator function for close window sequence<br/>
        /// This function will called by Close()
        /// </summary>
        /// <param name="window">target window</param>
        /// <param name="isPlayAnimation">parameter to tell is play animation or not</param>
        private IEnumerator CloseRoutine(BaseWindow window, bool isPlayAnimation)
        {
            SetBlockRaycast(true);

            _windows.Remove(window);

            if (window.IsHideOther)
            {
                yield return ShowFromTop();
            }

            window.Hidding();

            if (isPlayAnimation)
            {
                yield return window.PlayHideAnimationRoutine();
            }

            window.Hide();
            WhenWindowHidden?.Invoke(window);

            window.Close();
            WhenWindowClosed?.Invoke(window);

            var topmostWindow = GetTopWindow();

            if (topmostWindow != null)
            {
                SetFocusWindow(topmostWindow);
            }

            _windowRoutine = null;
            window.gameObject.transform.SetParent(null, false);
            window.BeforeDestroy();
            Destroy(window.gameObject);

            SetBlockRaycast(false);
        }

        /// <summary>
        /// IEnumerator function for close all windows sequence<br/>
        /// This function will called by CloseAll()
        /// </summary>
        private IEnumerator CloseAllRoutine()
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (!_windows[i].IsHidden)
                {
                    Close(_windows[i], false);
                }

                yield return null;
            }

            _closeAllRoutine = null;
            WhenClosedAll?.Invoke();
        }

        /// <summary>
        /// Function to skip all windows transition animation<br/> 
        /// when open or close while previous one is in transition
        /// </summary>
        private void SkipWindowsTransition()
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i].IsTransition)
                {
                    _windows[i].SkipTransition();
                }
            }
        }

        /// <summary>
        /// Unfocus current focus window
        /// </summary>
        private void UnfocusWindow()
        {
            if (FocusedWindow == null)
            {
                return;
            }

            if (!FocusedWindow.IsFocus)
            {
                Debug.LogAssertion("Focus window is not focus");

                return;
            }

            FocusedWindow.SetFocus(false);
            FocusedWindow = null;
        }

        /// <summary>
        /// Set target window to focus window
        /// </summary>
        /// <param name="window">target window</param>
        private void SetFocusWindow(BaseWindow window)
        {
            if (!_windows.Contains(window))
            {
                Debug.LogAssertion($"Cannot get {window} window");

                return;
            }

            if (window.IsHidden)
            {
                Debug.LogAssertion($"{window} is hidden");

                return;
            }

            UnfocusWindow();

            window.SetFocus(true);
            FocusedWindow = window;
            WhenWindowFocused?.Invoke(window);
        }
    }
}
