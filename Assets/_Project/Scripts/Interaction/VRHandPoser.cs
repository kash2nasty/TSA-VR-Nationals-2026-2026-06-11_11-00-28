// -----------------------------------------------------------------------------
//  VRHandPoser.cs
//  DECRYPTED - A Walk Through the History of Secret Writing
//
//  Drives a stylized low-poly VR hand the SUPERHOT way: the hand mesh is a pure
//  positional/rotational puppet of the controller (it is parented to the
//  controller, so pose tracking is automatic) and the only animation is a simple
//  two-pose finger curl driven by the grip and trigger inputs:
//
//    * Grip squeezed  -> all fingers curl toward the palm (make a fist).
//    * Trigger pulled -> the index finger extends (pointing), the rest follow grip.
//    * Thumb curls partially with the grip.
//
//  Each finger is a chain of joint transforms (proximal -> middle -> distal); we
//  lerp every joint's local pitch between an open and a closed angle, so the curl
//  compounds down the finger into a natural-looking grip. No rig, no clips.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

namespace Decrypted.Interaction
{
    [DisallowMultipleComponent]
    public class VRHandPoser : MonoBehaviour
    {
        [Header("Inputs (XRI Default Input Actions)")]
        [Tooltip("Grip squeeze value 0..1 (XRI <Hand> Interaction/Select Value).")]
        [SerializeField] private InputActionReference _gripValue;
        [Tooltip("Trigger value 0..1 (XRI <Hand> Interaction/Activate Value).")]
        [SerializeField] private InputActionReference _triggerValue;

        [Header("Finger joints (proximal -> distal per finger)")]
        [SerializeField] private Transform[] _indexJoints;
        [SerializeField] private Transform[] _middleRingPinkyJoints;
        [SerializeField] private Transform[] _thumbJoints;

        [Header("Curl (degrees per joint)")]
        [SerializeField] private float _openPerJoint = 6f;
        [SerializeField] private float _closedPerJoint = 28f;
        [Tooltip("Curl axis sign; flip if fingers bend the wrong way.")]
        [SerializeField] private float _curlSign = 1f;
        [SerializeField] private float _lerpSpeed = 18f;

        private float _curl, _index, _thumb;

        private void OnEnable()
        {
            if (_gripValue != null && _gripValue.action != null) _gripValue.action.Enable();
            if (_triggerValue != null && _triggerValue.action != null) _triggerValue.action.Enable();
        }

        private void Update()
        {
            float grip = (_gripValue != null && _gripValue.action != null) ? _gripValue.action.ReadValue<float>() : 0f;
            float trig = (_triggerValue != null && _triggerValue.action != null) ? _triggerValue.action.ReadValue<float>() : 0f;

            float targetCurl = grip;                       // middle/ring/pinky follow grip
            float targetIndex = Mathf.Lerp(grip, 0f, trig); // trigger extends index (pointing)
            float targetThumb = grip * 0.7f;

            float k = 1f - Mathf.Exp(-_lerpSpeed * Time.deltaTime); // frame-rate independent ease
            _curl = Mathf.Lerp(_curl, targetCurl, k);
            _index = Mathf.Lerp(_index, targetIndex, k);
            _thumb = Mathf.Lerp(_thumb, targetThumb, k);

            Apply(_middleRingPinkyJoints, _curl);
            Apply(_indexJoints, _index);
            Apply(_thumbJoints, _thumb);
        }

        private void Apply(Transform[] joints, float amount)
        {
            if (joints == null) return;
            float a = Mathf.Lerp(_openPerJoint, _closedPerJoint, Mathf.Clamp01(amount)) * _curlSign;
            for (int i = 0; i < joints.Length; i++)
                if (joints[i] != null) joints[i].localRotation = Quaternion.Euler(a, 0f, 0f);
        }
    }
}
