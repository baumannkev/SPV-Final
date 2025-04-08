using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles zooming and panning functionality for the main panel.
/// </summary>
public class MainPanelZoomAndPan : MonoBehaviour
{
    public RectTransform mainPanelContent; // Reference to the content of the main panel
    public float zoomSpeed = 0.1f; // Speed of zooming
    public float panSpeed = 1.0f; // Speed of panning
    public float minZoom = 0.5f; // Minimum zoom level
    public float maxZoom = 2.0f; // Maximum zoom level

    public float zoomLevel = 1.5f; // Zoom level when focusing on a task

    [Header("Grid Settings")]
    public Image backgroundImage; // Reference to the background image
    public float gridSize = 20f; // Size of each grid cell
    public Color gridLineColor = new Color(0.9f, 0.9f, 0.9f); // Light gray color for grid lines
    public Color gridBackgroundColor = new Color(1f, 1f, 1f); // White color for grid background

    private Vector2 lastMousePosition; // Stores the last mouse position for panning
    private Texture2D gridTexture; // Texture used for the grid

    /// <summary>
    /// Initializes the grid texture and applies it to the background image.
    /// </summary>
    void Start()
    {
        CreateGridTexture();
        if (backgroundImage != null)
        {
            backgroundImage.sprite = Sprite.Create(
                gridTexture,
                new Rect(0, 0, gridTexture.width, gridTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            // Make the sprite repeat
            backgroundImage.material.mainTextureScale = new Vector2(
                mainPanelContent.rect.width / gridSize,
                mainPanelContent.rect.height / gridSize
            );
        }
    }

    /// <summary>
    /// Creates a tiled grid texture for the background.
    /// </summary>
    void CreateGridTexture()
    {
        // Create a texture that will be tiled
        int textureSize = Mathf.CeilToInt(gridSize);
        gridTexture = new Texture2D(textureSize, textureSize);
        gridTexture.filterMode = FilterMode.Point;

        // Fill with background color
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                gridTexture.SetPixel(x, y, gridBackgroundColor);
            }
        }

        // Draw grid lines
        for (int x = 0; x < textureSize; x++)
        {
            gridTexture.SetPixel(x, 0, gridLineColor); // Bottom line
            gridTexture.SetPixel(x, textureSize - 1, gridLineColor); // Top line
        }
        for (int y = 0; y < textureSize; y++)
        {
            gridTexture.SetPixel(0, y, gridLineColor); // Left line
            gridTexture.SetPixel(textureSize - 1, y, gridLineColor); // Right line
        }

        gridTexture.Apply();
        gridTexture.wrapMode = TextureWrapMode.Repeat;
    }

    /// <summary>
    /// Handles zooming and panning based on user input.
    /// </summary>
    void Update()
    {
        // Zooming
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0.0f)
        {
            // Get mouse position in world space
            Vector2 mouseWorldPosition = Input.mousePosition;
            Vector2 contentPosition = mainPanelContent.position;

            // Calculate the difference between the mouse position and the content's position
            Vector2 offset = mouseWorldPosition - contentPosition;

            // Current scale
            Vector3 currentScale = mainPanelContent.localScale;
            float newScale = Mathf.Clamp(currentScale.x + scrollInput * zoomSpeed, minZoom, maxZoom);

            // Adjust content position to zoom around the mouse
            Vector2 newOffset = offset * (newScale / currentScale.x - 1f);
            mainPanelContent.position -= (Vector3)newOffset;

            // Apply the new scale
            mainPanelContent.localScale = new Vector3(newScale, newScale, 1);

            // Update grid scale with zoom
            if (backgroundImage != null)
            {
                backgroundImage.material.mainTextureScale = new Vector2(
                    mainPanelContent.rect.width / (gridSize * newScale),
                    mainPanelContent.rect.height / (gridSize * newScale)
                );
            }
        }

        // Panning
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(1))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
            mainPanelContent.anchoredPosition += delta * panSpeed;
            lastMousePosition = Input.mousePosition;

            // Update grid position with pan
            if (backgroundImage != null)
            {
                backgroundImage.material.mainTextureOffset = -mainPanelContent.anchoredPosition / (gridSize * mainPanelContent.localScale.x);
            }
        }
    }

    /// <summary>
    /// Cleans up the grid texture when the object is destroyed.
    /// </summary>
    void OnDestroy()
    {
        if (gridTexture != null)
        {
            Destroy(gridTexture);
        }
    }
}