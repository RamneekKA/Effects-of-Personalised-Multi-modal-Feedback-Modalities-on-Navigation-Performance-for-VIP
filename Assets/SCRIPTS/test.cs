using UnityEngine;
using Bhaptics.SDK2;

public class Example : MonoBehaviour
{
    [Header("Haptic Settings")]
    [SerializeField] private string hapticEventName = "left_full_4";
    
    [Header("Haptic Parameters")]
    [SerializeField] private int delayTime = 0;
    [SerializeField, Range(0.1f, 2.0f)] private float intensity = 1.0f;
    [SerializeField, Range(0.1f, 3.0f)] private float duration = 1.0f;
    [SerializeField, Range(0f, 360f)] private float rotationAngle = 0f;
    [SerializeField, Range(-0.5f, 0.5f)] private float verticalOffset = 0f;
    
    [Header("Input")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Space;

    void Update()
    {
        // Check for input
        if (Input.GetKeyDown(triggerKey))
        {
            OnShoot();
        }
    }

    private void OnShoot()
    {
        // Play haptic with current inspector values
        BhapticsLibrary.Play(
            hapticEventName,    // Haptic event name from Developer Portal
            delayTime,          // Delay Time (millisecond)
            intensity,          // Haptic intensity
            duration,           // Haptic duration
            rotationAngle,      // Rotate haptic around global Vector3.up (0f - 360f)
            verticalOffset      // Move haptic up or down (-0.5f - 0.5f)
        );
        
        Debug.Log($"Playing haptic: {hapticEventName} with intensity: {intensity}");
    }
    
    // Optional: Method to play haptic from other scripts
    public void TriggerHaptic()
    {
        OnShoot();
    }
}