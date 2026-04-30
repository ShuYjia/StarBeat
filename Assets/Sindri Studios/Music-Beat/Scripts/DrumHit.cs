using UnityEngine;

public class DrumHit : MonoBehaviour
{

    public AudioController audioController;

    public AudioController.DrumType drumType;

    [Header("Hit Settings")]

    public float minVelocity = 0.2f;

    public float hitCooldown = 0.05f;

    float lastHitTime;


    void Start()
    {
        if (audioController == null)
        {
            audioController =
                FindObjectOfType<AudioController>();
        }
    }



    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("DrumStick"))
            return;

        Rigidbody rb =
            other.GetComponent<Rigidbody>();

        if (rb == null)
            return;

        float velocity =
            rb.velocity.magnitude;

        if (velocity < minVelocity)
            return;

        if (Time.time - lastHitTime < hitCooldown)
            return;

        lastHitTime = Time.time;

        audioController.PlayDrum(
            drumType,
            velocity
        );
    }
}