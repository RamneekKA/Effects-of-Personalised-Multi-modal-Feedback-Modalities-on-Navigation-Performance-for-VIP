using UnityEngine;

/// <summary>
/// Simple body zone detection using basic collision and body proportions
/// </summary>
public class SimpleBodyZoneDetector : MonoBehaviour
{
    [Header("Body Zone Settings")]
    [Tooltip("Height of the character (total height from feet to head)")]
    public float characterHeight = 1.8f;
    
    [Tooltip("Width of character at shoulders (for left/right detection)")]
    public float characterWidth = 0.6f;
    
    [Tooltip("Depth of character (front to back)")]
    public float characterDepth = 0.3f;
    
    [Header("Zone Proportions (0-1 from bottom to top)")]
    public float footZoneTop = 0.2f;       // 0-20% = feet
    public float hipZoneTop = 0.45f;       // 20-45% = hips
    public float torsoZoneTop = 0.9f;      // 45-90% = torso/arms (increased torso area)
    public float headZoneTop = 1.0f;       // 90-100% = head
    
    private FCG.CharacterControl characterController;
    private Vector3 characterBase; // Bottom of character (feet level)
    
    void Start()
    {
        characterController = GetComponent<FCG.CharacterControl>();
        
        // Character base is at the CharacterController's bottom
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            characterBase = transform.position - Vector3.up * (charController.height * 0.5f);
        }
        else
        {
            characterBase = transform.position - Vector3.up * (characterHeight * 0.5f);
        }
    }
    
    void Update()
    {
        // Update character base position as player moves
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            characterBase = transform.position - Vector3.up * (charController.height * 0.5f);
        }
        else
        {
            characterBase = transform.position - Vector3.up * (characterHeight * 0.5f);
        }
    }
    
    /// <summary>
    /// Determine which body part was involved in collision based on collision point
    /// Returns: Head, Torso, Left Arm, Right Arm, Left Hip, Right Hip, Left Foot, Right Foot
    /// </summary>
    public string DetermineBodyPartFromCollision(Vector3 collisionPoint)
    {
        // Convert collision point to local space relative to character
        Vector3 localCollision = transform.InverseTransformPoint(collisionPoint);
        
        // Calculate height ratio (0 = feet, 1 = head)
        float heightFromBase = collisionPoint.y - characterBase.y;
        float heightRatio = Mathf.Clamp01(heightFromBase / characterHeight);
        
        // Determine which body part based on height and horizontal position
        if (heightRatio <= footZoneTop)
        {
            // Feet zone - always left or right, never center
            string side = GetHorizontalSide(localCollision.x);
            return side + " Foot";
        }
        else if (heightRatio <= hipZoneTop)
        {
            // Hip zone - always left or right, never center
            string side = GetHorizontalSide(localCollision.x);
            return side + " Hip";
        }
        else if (heightRatio <= torsoZoneTop)
        {
            // Torso/Arms zone - can be center (torso) or sides (arms)
            string side = GetHorizontalSideForTorso(localCollision.x);
            if (side == "Center")
            {
                return "Torso"; // Front or back collision
            }
            else
            {
                return side + " Arm"; // Side collision = arm
            }
        }
        else
        {
            // Head zone
            return "Head";
        }
    }
    
    string GetHorizontalSide(float localX)
    {
        // Negative X = Left, Positive X = Right (Unity's coordinate system)
        if (localX < 0)
        {
            return "Left";
        }
        else
        {
            return "Right";
        }
    }
    
    string GetHorizontalSideForTorso(float localX)
    {
        // Only torso can be "Center" - for front/back collisions
        float threshold = characterWidth * 0.2f; // 20% of width for center zone
        
        if (Mathf.Abs(localX) < threshold)
        {
            return "Center"; // Close to center line = torso
        }
        else if (localX < 0)
        {
            return "Left";
        }
        else
        {
            return "Right";
        }
    }
    
    /// <summary>
    /// Enhanced method that uses collision normal for better torso vs arm detection
    /// Returns: Head, Torso, Left Arm, Right Arm, Left Hip, Right Hip, Left Foot, Right Foot
    /// </summary>
    public string DetermineBodyPartFromCollisionAdvanced(Vector3 collisionPoint, Vector3 collisionNormal)
    {
        // Calculate height ratio (0 = feet, 1 = head)
        float heightFromBase = collisionPoint.y - characterBase.y;
        float heightRatio = Mathf.Clamp01(heightFromBase / characterHeight);
        
        Vector3 localCollision = transform.InverseTransformPoint(collisionPoint);
        Vector3 localNormal = transform.InverseTransformDirection(collisionNormal);
        
        // Determine body part based on height zones
        if (heightRatio <= footZoneTop)
        {
            // Feet zone
            string side = GetHorizontalSide(localCollision.x);
            return side + " Foot";
        }
        else if (heightRatio <= hipZoneTop)
        {
            // Hip zone
            string side = GetHorizontalSide(localCollision.x);
            return side + " Hip";
        }
        else if (heightRatio <= torsoZoneTop)
        {
            // Torso/Arms zone - use collision normal to distinguish
            // If collision came from front/back (high Z component), it's torso
            if (Mathf.Abs(localNormal.z) > 0.6f)
            {
                return "Torso";
            }
            // If collision came from sides (high X component), it's arms
            else if (Mathf.Abs(localNormal.x) > 0.6f)
            {
                string side = localNormal.x < 0 ? "Right" : "Left"; // Collision from left hits right side
                return side + " Arm";
            }
            else
            {
                // Fallback to position-based detection
                string side = GetHorizontalSide(localCollision.x);
                if (side == "Center")
                    return "Torso";
                else
                    return side + " Arm";
            }
        }
        else
        {
            // Head zone
            return "Head";
        }
    }
    
    /// <summary>
    /// Debug method to visualize body zones
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Vector3 center = transform.position;
        
        // Draw body zones as colored boxes
        Gizmos.color = Color.red;
        float footHeight = characterHeight * footZoneTop;
        Gizmos.DrawWireCube(center - Vector3.up * (characterHeight * 0.5f - footHeight * 0.5f), 
                           new Vector3(characterWidth, footHeight, characterDepth));
        
        Gizmos.color = Color.blue;
        float hipHeight = characterHeight * (hipZoneTop - footZoneTop);
        float hipCenter = characterHeight * 0.5f - characterHeight * footZoneTop - hipHeight * 0.5f;
        Gizmos.DrawWireCube(center - Vector3.up * hipCenter, 
                           new Vector3(characterWidth, hipHeight, characterDepth));
        
        Gizmos.color = Color.green;
        float torsoHeight = characterHeight * (torsoZoneTop - hipZoneTop);
        float torsoCenter = characterHeight * 0.5f - characterHeight * hipZoneTop - torsoHeight * 0.5f;
        Gizmos.DrawWireCube(center - Vector3.up * torsoCenter, 
                           new Vector3(characterWidth, torsoHeight, characterDepth));
        
        Gizmos.color = Color.yellow;
        float headHeight = characterHeight * (headZoneTop - torsoZoneTop);
        float headCenter = characterHeight * 0.5f - characterHeight * torsoZoneTop - headHeight * 0.5f;
        Gizmos.DrawWireCube(center - Vector3.up * headCenter, 
                           new Vector3(characterWidth, headHeight, characterDepth));
        
        // Draw character base reference
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(characterBase, 0.1f);
    }
}
