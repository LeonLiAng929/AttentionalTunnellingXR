using UnityEngine;
using TMPro; // Standard for Unity UI text
using System.Collections;

public class HelperScript : MonoBehaviour
{
    public static HelperScript Instance;
    [SerializeField] private TextMeshProUGUI notificationText;
    
    [Header("Settings")]
    [SerializeField] private float displayDuration = 3.0f;

    private Coroutine displayCoroutine;

    private void Start()
    {
        Instance = this;
        // Ensure the text is hidden on startup
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Call this method from any other script to show a message.
    /// Example: myNotificationDisplay.ShowNotification("Level Complete!");
    /// </summary>
    /// <param name="message">The text to display.</param>
    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        // If a notification is already showing, stop its timer
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        // Update the text and start a fresh timer
        notificationText.text = message;
        displayCoroutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        notificationText.gameObject.SetActive(true);
        
        // Wait for the specified duration
        yield return new WaitForSeconds(displayDuration);
        
        notificationText.gameObject.SetActive(false);
        displayCoroutine = null;
    }}
