using UnityEngine;

public class VRHandTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        InstrumentInteractable instrument =
            other.GetComponent<InstrumentInteractable>();

        if (instrument != null)
        {
            instrument.TriggerInstrument(transform.position);

        }
    }
}