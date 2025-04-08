using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a visualized task item in the grid layout.
/// </summary>
public class TaskItem : MonoBehaviour
{
    public int sizeWidth = 1; // Width of the task item in grid units
    public int sizeHeight = 1; // Height of the task item in grid units
    public TaskData taskData; // Reference to the associated TaskData
    private CanvasGroup canvasGroup; // Used for fading and visibility control

    /// <summary>
    /// Initializes the CanvasGroup component for the task item.
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Ensures the task starts fully visible and resets its position.
    /// </summary>
    public void InitializeAlpha()
    {
        canvasGroup.alpha = 1; // Start with full opacity
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0); // Reset position
    }

    /// <summary>
    /// Animates the task item to "shoot" upward and fade out.
    /// </summary>
    /// <param name="duration">Duration of the animation.</param>
    public void ShootIntoSpace(float duration)
    {
        StartCoroutine(ShootRoutine(duration));
    }

    /// <summary>
    /// Coroutine to handle the upward movement and fade-out animation.
    /// </summary>
    /// <param name="duration">Duration of the animation.</param>
    private IEnumerator ShootRoutine(float duration)
    {
        float time = 0;
        float startAlpha = canvasGroup.alpha;
        Vector3 initialPosition = transform.localPosition;
        Vector3 targetPosition = initialPosition + new Vector3(0, 200, 0); // Move 200 units upward

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Gradually fade out and move upward
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);
            transform.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);

            yield return null;
        }

        // Ensure the task is fully faded and hidden
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
}