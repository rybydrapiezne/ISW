using System.Collections;
using UnityEngine;

namespace PlayerShootingSystem
{
    public class Gun : MonoBehaviour
    {
        public GunInfo gunInfo;
        [Header("Gun Recoil")]
        public float recoilAmount = 8f;       
        public float recoilSide = 2f;         
        public float recoilSpeed = 12f;       
        public float returnSpeed = 5f;        

        [Header("Slide Recoil")]
        public Transform slide;
        public float slideBackAmount = 0.12f;
        public float slideBackSpeed = 25f;
        public float slideReturnSpeed = 15f;
        
        Vector3 _originalSlidePos;
        Quaternion _restRot;
    
        Coroutine _slideCoroutine;
        Coroutine _recoilCoroutine;

        void Start()
        {
            if (slide != null)
            {
                _originalSlidePos = slide.localPosition;
            }
        }

        public void PickedUp()
        {
            _restRot = transform.localRotation;
        }
        public void PerformShoot()
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRecoilCoroutine());
            if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
            _recoilCoroutine = StartCoroutine(RecoilCoroutine());
        }
        IEnumerator RecoilCoroutine()
        {
            float side = Random.Range(-recoilSide, recoilSide);
            Quaternion recoilTarget = _restRot * Quaternion.Euler(recoilAmount, side, 0);

            while (Quaternion.Angle(transform.localRotation, recoilTarget) > 0.1f)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTarget, Time.deltaTime * recoilSpeed);
                yield return null;
            }
            transform.localRotation = recoilTarget;

            while (Quaternion.Angle(transform.localRotation, _restRot) > 0.1f)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, _restRot, Time.deltaTime * returnSpeed);
                yield return null;
            }
            transform.localRotation = _restRot;
        }

        IEnumerator SlideRecoilCoroutine()
        {
            if (!slide) yield break;

            Vector3 backPos = _originalSlidePos + Vector3.forward * slideBackAmount;

            while (Vector3.Distance(slide.localPosition, backPos) > 0.001f)
            {
                slide.localPosition = Vector3.Lerp(slide.localPosition, backPos, Time.deltaTime * slideBackSpeed);
                yield return null;
            }
            slide.localPosition = backPos;

            while (Vector3.Distance(slide.localPosition, _originalSlidePos) > 0.001f)
            {
                slide.localPosition = Vector3.Lerp(slide.localPosition, _originalSlidePos, Time.deltaTime * slideReturnSpeed);
                yield return null;
            }
            slide.localPosition = _originalSlidePos;
        }
        
    }
}
