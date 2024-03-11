using Common.Enum;
using Common.KeyData;
using Generic.CustomYield;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Generic.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseWindow : MonoBehaviour
    {
        protected enum WindowTransitionState
        {
            None = 0,
            Open = 1,
            Close = 2,
        }

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private bool _isHideOther;
        public bool IsHideOther => _isHideOther;

        [SerializeField]
        private GameObject _firstSelection;
        public GameObject FirstSelection => _firstSelection;

        [SerializeField]
        private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField]
        private WindowType _type;
        public WindowType Type => _type;

        /// <summary>
        /// Normalize time of the animation's end frame
        /// </summary>
        private const float _endFrameNormalizeTime = 1f;

        public event Action<BaseWindow> WhenCreated;
        public event Action<BaseWindow> WhenOpened;
        public event Action<BaseWindow> WhenShown;
        public event Action<BaseWindow> WhenHidden;
        public event Action<BaseWindow> WhenClosed;
        public event Action<BaseWindow> WhenFocusChanged;
        public event Action<BaseWindow> WhenBacked;

        public bool IsFocus { get; protected set; }
        public bool IsTransition { get; protected set; }
        public bool IsHidden { get; protected set; }
        public GameObject LastSelect { get; private set; }


        protected WindowTransitionState TransitionState = WindowTransitionState.None;

        protected Coroutine AnimationRoutine = null;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return obj is BaseWindow window && base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode());
        }

        /// <summary>
        /// Called when this window is created
        /// </summary>
        public void Create()
        {
            CreateProcess();
            WhenCreated?.Invoke(this);
        }

        /// <summary>
        /// Called when this window is opened
        /// </summary>
        public void Open()
        {
            OpenProcess();
            WhenOpened?.Invoke(this);
        }

        /// <summary>
        /// Called when this window is closed
        /// </summary>
        public void Close()
        {
            CloseProcess();
            WhenClosed?.Invoke(this);
        }

        /// <summary>
        /// Called by windowManager befored destroy window<br/>
        /// </summary>
        public virtual void BeforeDestroy()
        {
        }

        /// <summary>
        /// Called when this window starting to show
        /// </summary>
        public void Showing()
        {
            IsHidden = false;
            gameObject.SetActive(true);

            ShowingProcess();
        }

        /// <summary>
        /// IEnumerator function that called by PlayShowAnimationRoutine()<br/>
        /// Call to run show animation routine 
        /// </summary>
        public virtual IEnumerator ShowAnimationRoutine()
        {
            IsTransition = true;

            if (_animator != null)
            {
                PlayShowAnimation();
                yield return new WaitUntilAnimationEnded(_animator);
            }
        }

        /// <summary>
        /// Called by window manager when this window is shown
        /// </summary>
        public void Show()
        {
            ShowProcess();
            WhenShown?.Invoke(this);
        }

        /// <summary>
        /// Called by window manager when this window start to hide
        /// </summary>
        public void Hidding()
        {
            HiddingProcess();
        }

        /// <summary>
        /// IEnumerator function that called by PlayHideAnimationRoutine()<br/>
        /// Call to run hide animation routine 
        /// </summary>
        public virtual IEnumerator HideAnimationRoutine()
        {
            IsTransition = true;

            if (_animator != null)
            {
                PlayHideAnimation();
                yield return new WaitUntilAnimationEnded(_animator);
            }
        }

        /// <summary>
        /// Called by window manager when this window is hidden
        /// </summary>
        public virtual void Hide()
        {
            HideProcess();
            WhenHidden?.Invoke(this);
        }

        /// <summary>
        /// Called by window manager when this window is focus & unfocus<br/>
        /// </summary>
        /// <param name="isFocus">Set focus state of this window</param>
        public void SetFocus(bool isFocus)
        {
            if (IsFocus != isFocus)
            {
                WhenFocusChanged?.Invoke(this);
            }

            FocusProcess(isFocus);
        }

        /// <summary>
        /// Called by window manager when this window is backed
        /// </summary>
        public void Back()
        {
            BackProcess();
            WhenBacked?.Invoke(this);
        }

        /// <summary>
        /// Called by window manager when it need to skip this window animation transition
        /// </summary>
        public void SkipTransition()
        {
            if (!IsTransition)
            {
                return;
            }

            switch (TransitionState)
            {
                case WindowTransitionState.Open:
                    SkipOpenTransition();
                    break;

                case WindowTransitionState.Close:
                    SkipCloseTransition();
                    break;
            }
        }

        /// <summary>
        /// IEnumerator function that called by window manager<br/>
        /// Called to play show animation 
        /// </summary>
        public IEnumerator PlayShowAnimationRoutine()
        {
            if (AnimationRoutine != null)
            {
                StopCoroutine(AnimationRoutine);
            }

            AnimationRoutine = StartCoroutine(ShowAnimationRoutine());

            yield return AnimationRoutine;
        }

        /// <summary>
        /// IEnumerator function that called by window manager<br/>
        /// Called to play hide animation 
        /// </summary>
        public IEnumerator PlayHideAnimationRoutine()
        {
            if (AnimationRoutine != null)
            {
                StopCoroutine(AnimationRoutine);
            }

            AnimationRoutine = StartCoroutine(HideAnimationRoutine());

            yield return AnimationRoutine;
        }

        #region > Processes
        /// <summary>
        /// Called from Create()<br/>
        /// Create process logic will write inside this function
        /// </summary>
        protected virtual void CreateProcess()
        {

        }

        /// <summary>
        /// Called from Open()<br/>
        /// Open process logic will write inside this function
        /// </summary>
        protected virtual void OpenProcess()
        {

        }


        /// <summary>
        /// Called from Close()<br/>
        /// Close process logic will write inside this function
        /// </summary>
        protected virtual void CloseProcess()
        {

        }

        /// <summary>
        /// Called from Showing()<br/>
        /// Showing process logic will write inside this function
        /// </summary>
        protected virtual void ShowingProcess()
        {

        }

        /// <summary>
        /// Called from Hidding()<br/>
        /// Hidding process logic will write inside this function
        /// </summary>
        protected virtual void HiddingProcess()
        {

        }

        /// <summary>
        /// Called from Show()<br/>
        /// Show process logic will write inside this function
        /// </summary>
        protected virtual void ShowProcess()
        {
            if (_animator != null)
            {
                if (AnimationRoutine != null)
                {
                    StopCoroutine(AnimationRoutine);
                }

                SkipToLastFrameAnimation();
            }

            IsTransition = false;
            TransitionState = WindowTransitionState.None;
        }

        /// <summary>
        /// Called from Hide()<br/>
        /// Hide process logic will write inside this function
        /// </summary>
        protected virtual void HideProcess()
        {
            if (_animator != null)
            {
                if (AnimationRoutine != null)
                {
                    StopCoroutine(AnimationRoutine);
                }

                SkipToLastFrameAnimation();
            }

            IsHidden = true;
            IsTransition = false;
            TransitionState = WindowTransitionState.None;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called from Focus()<br/>
        /// Focus process logic will write inside this function
        /// </summary>
        protected virtual void FocusProcess(bool isFocus)
        {
            IsFocus = isFocus;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = isFocus;
            }

            if (IsFocus)
            {
                var target = LastSelect;

                if (FirstSelection != null)
                {
                    target = FirstSelection;
                }

                if (target != null)
                {
                    SetFirstSelection(target);
                }
            }
            else
            {
                SetFirstSelection(null);
            }
        }

        /// <summary>
        /// Called from Back()<br/>
        /// Back process logic will write inside this function
        /// </summary>
        protected virtual void BackProcess()
        {
            WindowManager window = GameController.Instance.WindowManager;
            window.Close(this, true);
        }
        #endregion

        /// <summary>
        /// Called to play show's animation
        /// </summary>
        protected virtual void PlayShowAnimation()
        {
            _animator.SetBool(UIAnimationKey.IsShow, true);
        }

        /// <summary>
        /// Called to play hide's animation
        /// </summary>
        protected virtual void PlayHideAnimation()
        {
            _animator.SetBool(UIAnimationKey.IsShow, false);
        }

        /// <summary>
        /// Called when system lose navigation<br/>
        /// It will called this function to recover select & navigation of this window
        /// </summary>
        protected virtual void RecoveryNavigation()
        {
            //TODO: logic to recovery navigation
        }

        /// <summary>
        /// Called when this window is on focused.<br/>
        /// By default, it will select the lastSelect.
        /// </summary>
        /// <param name="target">first select target</param>
        protected virtual void SetFirstSelection(GameObject target)
        {
            EventSystem.current.SetSelectedGameObject(target);

            if (target != null)
            {
                LastSelect = target;
            }
        }

        /// <summary>
        /// Called by SkipTransition() when this window isTransition of open animation.
        /// </summary>
        protected virtual void SkipOpenTransition()
        {
            IsTransition = false;
            Open();
            Showing();
            Show();
        }

        /// <summary>
        /// Called by SkipTransition() when this window isTransition of close animation.
        /// </summary>
        protected virtual void SkipCloseTransition()
        {
            IsTransition = false;
            Hidding();
            Hide();
            Close();
        }

        /// <summary>
        /// Call to skip current animation clip to the last frame
        /// </summary>
        protected void SkipToLastFrameAnimation()
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(stateInfo.fullPathHash, 0, _endFrameNormalizeTime);
        }
    }
}