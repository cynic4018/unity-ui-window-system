using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Generic.CustomYield
{
    public class WaitUntilAnimationEnded : CustomYieldInstruction
    {
        /// <summary>
        /// Target animator
        /// </summary>
        private readonly Animator _animator;
        /// <summary>
        /// Animation clip's layer index
        /// </summary>
        private readonly int _layerIndex;

        private float _normalizeTime;

        public WaitUntilAnimationEnded(Animator animator, int layerIndex = 0)
        {
            _normalizeTime = 0;
            _animator = animator;
            _layerIndex = layerIndex;
        }

        public override bool keepWaiting 
        {
            get
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_layerIndex);

                if (_animator.IsInTransition(_layerIndex))
                {
                    return true;
                }

                _normalizeTime += Time.unscaledDeltaTime / stateInfo.length;

                return _normalizeTime < 1;
            }
        }
    }
}
