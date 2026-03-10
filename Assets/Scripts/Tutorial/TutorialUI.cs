using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Displays tutorial pages with text, a looping video, and a Continue button.
/// Attach to the tutorial panel GameObject.
///
/// Required hierarchy:
///   TutorialPanel (this component, starts disabled)
///     ├── Background (Image, semi-transparent fullscreen overlay)
///     ├── ContentPanel
///     │   ├── VideoDisplay (RawImage — assign to videoImage)
///     │   ├── TutorialText (TMP_Text — assign to tutorialText)
///     │   └── ContinueButton (Button — assign to continueButton)
///     └── VideoPlayer (VideoPlayer component — assign to videoPlayer)
///
/// The VideoPlayer renders to a RenderTexture which is assigned to the RawImage.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture videoRenderTexture;

    [Header("Page Indicator (optional)")]
    [SerializeField] private TMP_Text pageIndicatorText;

    void Awake()
    {
        // Subscribe in Awake so we're listening before TutorialManager.Start() fires
        TutorialManager.OnShowPage += ShowPage;
        TutorialManager.OnTutorialClosed += HidePanel;
    }

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void OnDestroy()
    {
        TutorialManager.OnShowPage -= ShowPage;
        TutorialManager.OnTutorialClosed -= HidePanel;
    }

    void ShowPage(TutorialPage page)
    {
        if (page == null) return;

        // Show panel
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Set text
        if (tutorialText != null)
            tutorialText.text = page.tutorialText;

        // Set up video
        if (videoPlayer != null && page.videoClip != null)
        {
            // Clear the render texture before playing a new clip
            if (videoRenderTexture != null)
            {
                RenderTexture current = RenderTexture.active;
                RenderTexture.active = videoRenderTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = current;
            }

            videoPlayer.clip = page.videoClip;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.Play();

            if (videoImage != null)
            {
                videoImage.texture = videoRenderTexture;
                videoImage.gameObject.SetActive(true);
            }
        }
        else
        {
            // No video — hide the video area
            if (videoPlayer != null)
                videoPlayer.Stop();
            if (videoImage != null)
                videoImage.gameObject.SetActive(false);
        }

        // Update continue button text
        UpdateContinueButton();

        // Update page indicator
        UpdatePageIndicator();
    }

    void HidePanel()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void OnContinueClicked()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnContinuePressed();
    }

    void UpdateContinueButton()
    {
        if (continueButtonText == null) return;

        if (TutorialManager.Instance == null)
        {
            continueButtonText.text = "Close";
            return;
        }

        // Check if there are more pages in the active sequence
        bool hasMorePages = TutorialManager.Instance.HasMorePages();
        continueButtonText.text = hasMorePages ? "Continue" : "Got it!";
    }

    void UpdatePageIndicator()
    {
        if (pageIndicatorText == null) return;

        if (TutorialManager.Instance == null)
        {
            pageIndicatorText.gameObject.SetActive(false);
            return;
        }

        int current = TutorialManager.Instance.CurrentPageNumber;
        int total = TutorialManager.Instance.TotalPageCount;

        if (total > 1)
        {
            pageIndicatorText.gameObject.SetActive(true);
            pageIndicatorText.text = $"{current} / {total}";
        }
        else
        {
            pageIndicatorText.gameObject.SetActive(false);
        }
    }
}