using UnityEngine;
using Bhaptics.SDK2;

public class HapticIntensityController : MonoBehaviour
{
    [Header("Haptic Settings")]
    [Range(0.1f, 2.0f)]
    public float intensity = 1.0f;
    
    [Range(0.5f, 3.0f)]
    public float duration = 1.0f;
    
    [Range(0f, 360f)]
    public float rotation = 0f;
    
    [Range(-0.5f, 0.5f)]
    public float verticalOffset = 0f;

    private string eventName = "left_100";

    void Start()
    {
        // Test the haptic on start
        PlayHapticWithCurrentSettings();
    }

    void Update()
    {
        // Play haptic when spacebar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayHapticWithCurrentSettings();
        }
        
        // Quick intensity controls with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetIntensity(0.25f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetIntensity(0.5f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetIntensity(0.75f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetIntensity(1.0f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetIntensity(1.5f);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetIntensity(2.0f);
    }

    public void PlayHapticWithCurrentSettings()
    {
        // Play the haptic event with current intensity settings
        int requestId = BhapticsLibrary.Play(
            eventName,      // Your event name
            0,              // No delay
            intensity,      // Current intensity multiplier
            duration,       // Current duration multiplier
            rotation,       // Rotation angle
            verticalOffset  // Vertical offset
        );
        
        Debug.Log($"Playing '{eventName}' with intensity: {intensity}, duration: {duration}, rotation: {rotation}°");
        
        if (requestId == -1)
        {
            Debug.LogWarning("Failed to play haptic event. Make sure the event exists and device is connected.");
        }
    }

    public void SetIntensity(float newIntensity)
    {
        intensity = Mathf.Clamp(newIntensity, 0.1f, 2.0f);
        Debug.Log($"Intensity set to: {intensity}");
        PlayHapticWithCurrentSettings();
    }

    public void StopHaptic()
    {
        BhapticsLibrary.StopByEventId(eventName);
        Debug.Log($"Stopped haptic event: {eventName}");
    }

    public void TestIntensityRange()
    {
        // Play the haptic at different intensities in sequence
        StartCoroutine(IntensityTestSequence());
    }

    private System.Collections.IEnumerator IntensityTestSequence()
    {
        float[] testIntensities = { 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f };
        
        foreach (float testIntensity in testIntensities)
        {
            SetIntensity(testIntensity);
            yield return new WaitForSeconds(1.5f);
        }
        
        // Reset to default
        SetIntensity(1.0f);
    }
}