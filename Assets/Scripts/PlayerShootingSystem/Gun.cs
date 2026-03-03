using System.Collections;
using PlayerShootingSystem;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GunInfo gunInfo;
    [Header("Gun Recoil")]
    public float recoilAmount = 8f;       // degrees up
    public float recoilSide = 2f;         // random left/right
    public float recoilSpeed = 12f;       // snap speed
    public float returnSpeed = 5f;        // how fast it returns

    [Header("Slide Recoil")]
    public Transform slide;               // Drag your slide GameObject here in Inspector
    public float slideBackAmount = 0.12f; // How far back (local Z, adjust if your model uses X/Y)
    public float slideBackSpeed = 25f;    // How fast it racks back
    public float slideReturnSpeed = 15f;  // How fast it returns forward (slower = more realistic)

    [Header("Sway (optional)")]
    public float swayAmount = 4f;
    public float maxSway = 6f;
    public float swaySmooth = 8f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private Vector3 currentRecoil = Vector3.zero;

    // Slide vars
    private Vector3 originalSlidePos;
    private Vector3 slideTargetPos;
    private bool isSlidingBack = false;
    
    Coroutine slideCoroutine;
    Coroutine recoilCoroutine;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        if (slide != null)
        {
            originalSlidePos = slide.localPosition;
            slideTargetPos = originalSlidePos;
        }
    }

    void Update()
    {
    }

    public void PerformShoot()
    {
        //HandleGunRecoil();
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideRecoilCoroutine());
        if (recoilCoroutine != null) StopCoroutine(recoilCoroutine);
        recoilCoroutine = StartCoroutine(RecoilCoroutine());
    }
    IEnumerator RecoilCoroutine()
    {
        Quaternion restRot = transform.localRotation; // capture NOW, not from Start
        float side = Random.Range(-recoilSide, recoilSide);
        Quaternion recoilTarget = restRot * Quaternion.Euler(recoilAmount, side, 0);

        // Snap up
        while (Quaternion.Angle(transform.localRotation, recoilTarget) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, recoilTarget, Time.deltaTime * recoilSpeed);
            yield return null;
        }
        transform.localRotation = recoilTarget;

        // Return
        while (Quaternion.Angle(transform.localRotation, restRot) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, restRot, Time.deltaTime * returnSpeed);
            yield return null;
        }
        transform.localRotation = restRot;
    }

    IEnumerator SlideRecoilCoroutine()
    {
        if (slide == null) yield break;

        Vector3 backPos = originalSlidePos + Vector3.forward * slideBackAmount;

        // Slide back
        while (Vector3.Distance(slide.localPosition, backPos) > 0.001f)
        {
            slide.localPosition = Vector3.Lerp(slide.localPosition, backPos, Time.deltaTime * slideBackSpeed);
            yield return null;
        }
        slide.localPosition = backPos;

        // Slide forward
        while (Vector3.Distance(slide.localPosition, originalSlidePos) > 0.001f)
        {
            slide.localPosition = Vector3.Lerp(slide.localPosition, originalSlidePos, Time.deltaTime * slideReturnSpeed);
            yield return null;
        }
        slide.localPosition = originalSlidePos;
    }

    void TriggerSlide()
    {
        if (slide == null) return;
        isSlidingBack = true;
        slideTargetPos = originalSlidePos + Vector3.back * slideBackAmount;  // Assumes slide moves -Z
    }
}
