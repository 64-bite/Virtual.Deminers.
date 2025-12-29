using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(SphereCollider))] // ����������� ���� ��������, ���� ���� ����
public class MetalDetector : MonoBehaviour
{
    [Header("Surface Settings")]
    public float depthOnGround = 1.0f;
    public float depthOnAsphalt = 0.3f;
    public LayerMask surfaceLayer;

    [Header("Target Settings")]
    public Transform detectionPoint; // �����, ����� ��� ������ ���������
    public string dangerTag = "Mine";
    public string scrapTag = "Scrap";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dangerClip;
    public AudioClip scrapClip;
    public AudioClip calibrateClip;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private SphereCollider detectionCollider; // ��������� �� �������� ������
    private Transform currentTarget; // ��, �� �� ����� "������"
    private float currentMaxDistance; // ��� ���������� �������
    private bool isHeld = false;

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        detectionCollider = GetComponent<SphereCollider>();

        // ������������ ��������� �� �������
        detectionCollider.isTrigger = true;

        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.activated.AddListener(OnCalibrateButton);

        ApplyCalibration(depthOnGround);
    }

    // --- ��ò�� �����в� (� ������� �������) ---
    void OnTriggerEnter(Collider other)
    {
        if (!isHeld) return;

        if (other.CompareTag(dangerTag))
        {
            currentTarget = other.transform;
            PlaySound(dangerClip);
        }
        else if (other.CompareTag(scrapTag))
        {
            currentTarget = other.transform;
            PlaySound(scrapClip);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (currentTarget != null && other.transform == currentTarget)
        {
            currentTarget = null;
            if (audioSource.clip != calibrateClip) audioSource.Stop();
        }
    }
    // ------------------------------------------

    void Update()
    {
        if (isHeld && currentTarget != null)
        {
            float dist = Vector3.Distance(detectionPoint.position, currentTarget.position);

            // ���������� � ������� �������
            float t = Mathf.Clamp01(dist / currentMaxDistance);

            // ��������: ��� ������, ��� ������� (1.0 -> 0.0)
            audioSource.volume = 1f - t;

            // Pitch: ��� ������, ��� ����� ���� (3.0 -> 1.0)
            audioSource.pitch = 1f + (1f - t) * 2f;

            if (!audioSource.isPlaying) audioSource.Play();
        }
        else if (!isHeld)
        {
            if (audioSource.isPlaying && audioSource.clip != calibrateClip) audioSource.Stop();
        }
    }

    private void OnCalibrateButton(ActivateEventArgs args)
    {
        RaycastHit hit;
        if (Physics.Raycast(detectionPoint.position, Vector3.down, out hit, 2.0f, surfaceLayer))
        {
            if (hit.collider.CompareTag("Asphalt"))
            {
                ApplyCalibration(depthOnAsphalt);
                PlayCalibrationSound(1.5f);
            }
            else if (hit.collider.CompareTag("Ground"))
            {
                ApplyCalibration(depthOnGround);
                PlayCalibrationSound(1.0f);
            }
        }
    }

    void ApplyCalibration(float newRadius)
    {
        currentMaxDistance = newRadius;
        // Գ����� ������� ����� �������
        detectionCollider.radius = newRadius;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource.clip == calibrateClip && audioSource.isPlaying) return;

        if (audioSource.clip != clip)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
    void PlayCalibrationSound(float pitch)
    {
        audioSource.Stop();
        audioSource.clip = calibrateClip;
        audioSource.volume = 1f; // ������� �������� ��� ���������
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    private void OnGrab(SelectEnterEventArgs args) => isHeld = true;
    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        currentTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        if (detectionPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(detectionPoint.position, Vector3.down * 2.0f);
        }
        // ����� ��������� ����������� ����������� SphereCollider
    }
}