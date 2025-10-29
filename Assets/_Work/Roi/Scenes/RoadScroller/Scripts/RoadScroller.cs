using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    [Header("Texture Scrolling Settings")]
    [SerializeField] private GameObject quadObject; // Reference to the quad with texture
    [SerializeField] private float scrollSpeed = 1.0f; // Speed of texture scrolling
    
    [Header("Scroll Direction")]
    [SerializeField] private ScrollDirection scrollDirection = ScrollDirection.Down;
    
    private Material quadMaterial; // Material component of the quad
    private Vector2 offset = Vector2.zero; // Current texture offset
    
    public enum ScrollDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    void Start()
    {
        // Get the material from the quad's renderer
        if (quadObject != null)
        {
            Renderer quadRenderer = quadObject.GetComponent<Renderer>();
            if (quadRenderer != null)
            {
                // Create a new material instance to avoid modifying the original asset
                quadMaterial = new Material(quadRenderer.material);
                quadRenderer.material = quadMaterial;
            }
            else
            {
                Debug.LogError("RoadScroller: No Renderer component found on the quad object!");
            }
        }
        else
        {
            Debug.LogError("RoadScroller: No quad object assigned!");
        }
    }

    void Update()
    {
        if (quadMaterial != null)
        {
            // Calculate the offset based on time and scroll speed
            Vector2 scrollOffset = GetScrollOffset() * scrollSpeed * Time.time;
            
            // Apply the offset to the material's main texture
            quadMaterial.mainTextureOffset = scrollOffset;
        }
    }
    
    private Vector2 GetScrollOffset()
    {
        switch (scrollDirection)
        {
            case ScrollDirection.Up:
                return new Vector2(0, 1);
            case ScrollDirection.Down:
                return new Vector2(0, -1);
            case ScrollDirection.Left:
                return new Vector2(-1, 0);
            case ScrollDirection.Right:
                return new Vector2(1, 0);
            default:
                return Vector2.up;
        }
    }
    
    // Public methods to control scrolling at runtime
    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }
    
    public void SetScrollDirection(ScrollDirection newDirection)
    {
        scrollDirection = newDirection;
    }
    
    public void StopScrolling()
    {
        scrollSpeed = 0f;
    }
    
    void OnDestroy()
    {
        // Clean up the material instance when the object is destroyed
        if (quadMaterial != null)
        {
            DestroyImmediate(quadMaterial);
        }
    }
}
