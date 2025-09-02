using UnityEngine;
using Bhaptics.SDK2;

/// <summary>
/// Simple script to test if haptic events are working
/// Attach this to any GameObject and press keys to test
/// </summary>
public class HapticDebugTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) TestHapticEvent("centre_100");
        if (Input.GetKeyDown(KeyCode.W)) TestHapticEvent("left_100");
        if (Input.GetKeyDown(KeyCode.E)) TestHapticEvent("right_100");
        if (Input.GetKeyDown(KeyCode.R)) TestHapticEvent("left_back_100");
        if (Input.GetKeyDown(KeyCode.T)) TestHapticEvent("right_back_100");
        if (Input.GetKeyDown(KeyCode.Y)) TestHapticEvent("centre_leftback_100");
        if (Input.GetKeyDown(KeyCode.U)) TestHapticEvent("centre_rightback_100");
    }
    
    void TestHapticEvent(string eventName)
    {
        Debug.Log($"Testing haptic event: {eventName}");
        
        int result = BhapticsLibrary.Play(eventName, 0, 1.0f, 1.0f, 0f, 0f);
        
        if (result == -1)
        {
            Debug.LogError($"Failed to play haptic event: {eventName}");
            Debug.LogError("Check: 1) Bhaptics device connected, 2) Event exists in Bhaptics Designer, 3) Event names match exactly");
        }
        else
        {
            Debug.Log($"Successfully triggered haptic: {eventName}");
        }
    }
    
    void Start()
    {
        Debug.Log("Haptic Debug Tester started");
        Debug.Log("Press Q/W/E/R/T/Y/U to test different haptic events");
        Debug.Log("Q=centre_100, W=left_100, E=right_100, R=left_back_100, T=right_back_100, Y=centre_leftback_100, U=centre_rightback_100");
    }
}