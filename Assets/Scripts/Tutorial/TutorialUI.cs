using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Displays tutorial pages with selectable layouts, supporting images and videos.
/// Each layout panel has two media slots for single or dual-media display.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;
    [SerializeField] private TMP_Text pageIndicatorText;

    [Header("Layout Panels")]
    [SerializeField] private LayoutPanel[] layoutPanels;

    [Header("Video Playback")]
    [SerializeField] private VideoPlayer videoPlayer1;
    [SerializeField] private RenderTexture videoRenderTexture1;
    [SerializeField] private VideoPlayer videoPlayer2;
    [SerializeField] private RenderTexture videoRenderTexture2;

    private LayoutPanel activeLayout;

    /// <summary>
    /// Maps TutorialLayout enum to a panel with dual media slots.
    /// </summary>
    [System.Serializable]
    public class LayoutPanel
    {
        public TutorialLayout layoutType;
        public GameObject panelRoot;
        public TMP_Text textDisplay;

        [Header("Media Slot 1")]
        public GameObject media1Container;
        public RawImage video1Display;
        public Image image1Display;
        public TMP_Text caption1Text;

        [Header("Media Slot 2")]
        public GameObject media2Container;
        public RawImage video2Display;
        public Image image2Display;
        public TMP_Text caption2Text;
    }

    void Awake()
    {
        TutorialManager.OnShowPage += ShowPage;
        TutorialManager.OnTutorialClosed += HidePanel;
    }

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        foreach (var lp in layoutPanels)
        {
            if (lp.panelRoot != null)
                lp.panelRoot.SetActive(false);
        }
    }

    void OnDestroy()
    {
        TutorialManager.OnShowPage -= ShowPage;
        TutorialManager.OnTutorialClosed -= HidePanel;
    }

    void ShowPage(TutorialPage page)
    {
        if (page == null) return;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ActivateLayout(page.layout);

        // Set text
        if (activeLayout?.textDisplay != null)
            activeLayout.textDisplay.text = page.tutorialText ?? "";

        // Stop any playing videos
        StopAllVideos();

        // Display media slots
        DisplayMediaSlot(page.media1, 
            activeLayout?.media1Container,
            activeLayout?.image1Display, 
            activeLayout?.video1Display, 
            activeLayout?.caption1Text,
            videoPlayer1, videoRenderTexture1);

        DisplayMediaSlot(page.media2, 
            activeLayout?.media2Container,
            activeLayout?.image2Display, 
            activeLayout?.video2Display, 
            activeLayout?.caption2Text,
            videoPlayer2, videoRenderTexture2);

        UpdateContinueButton();
        UpdatePageIndicator();
    }

    void ActivateLayout(TutorialLayout layout)
    {
        foreach (var lp in layoutPanels)
        {
            if (lp.panelRoot != null)
                lp.panelRoot.SetActive(false);
        }

        activeLayout = null;
        foreach (var lp in layoutPanels)
        {
            if (lp.layoutType == layout)
            {
                activeLayout = lp;
                if (lp.panelRoot != null)
                    lp.panelRoot.SetActive(true);
                break;
            }
        }

        if (activeLayout == null && layoutPanels.Length > 0)
        {
            activeLayout = layoutPanels[0];
            if (activeLayout.panelRoot != null)
                activeLayout.panelRoot.SetActive(true);
        }
    }

    void DisplayMediaSlot(TutorialMediaItem item, GameObject container, 
        Image imageDisplay, RawImage videoDisplay, TMP_Text captionText,
        VideoPlayer player, RenderTexture renderTex)
    {
        bool hasContent = item != null && item.HasContent;

        // Show/hide the entire container
        if (container != null)
            container.SetActive(hasContent);

        if (!hasContent)
        {
            if (imageDisplay != null) imageDisplay.gameObject.SetActive(false);
            if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
            if (captionText != null) captionText.gameObject.SetActive(false);
            return;
        }

        if (item.type == TutorialMediaItem.MediaType.Image)
        {
            if (imageDisplay != null)
            {
                imageDisplay.sprite = item.image;
                imageDisplay.gameObject.SetActive(true);

                // Set aspect ratio from image dimensions
                var fitter = imageDisplay.GetComponent<AspectRatioFitter>();
                if (fitter != null && item.image != null && item.image.texture != null)
                {
                    fitter.aspectRatio = (float)item.image.texture.width / item.image.texture.height;
                }
            }
            if (videoDisplay != null)
                videoDisplay.gameObject.SetActive(false);
        }
        else // Video
        {
            if (imageDisplay != null)
                imageDisplay.gameObject.SetActive(false);

            if (player != null && item.video != null && renderTex != null)
            {
                ClearRenderTexture(renderTex);
                player.clip = item.video;
                player.isLooping = true;
                player.renderMode = VideoRenderMode.RenderTexture;
                player.targetTexture = renderTex;
                player.Play();

                if (videoDisplay != null)
                {
                    videoDisplay.texture = renderTex;
                    videoDisplay.gameObject.SetActive(true);

                    // Set aspect ratio from video dimensions
                    var fitter = videoDisplay.GetComponent<AspectRatioFitter>();
                    if (fitter != null)
                    {
                        fitter.aspectRatio = (float)item.video.width / item.video.height;
                    }
                }
            }
        }

        if (captionText != null)
        {
            captionText.text = item.caption ?? "";
            captionText.gameObject.SetActive(!string.IsNullOrEmpty(item.caption));
        }
    }

    void ClearRenderTexture(RenderTexture rt)
    {
        if (rt == null) return;
        RenderTexture current = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = current;
    }

    void StopAllVideos()
    {
        if (videoPlayer1 != null) videoPlayer1.Stop();
        if (videoPlayer2 != null) videoPlayer2.Stop();
    }

    void HidePanel()
    {
        StopAllVideos();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void OnContinueClicked()
    {
        // Clear selection so button returns to normal color
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

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

        bool hasMore = TutorialManager.Instance.HasMorePages();
        continueButtonText.text = hasMore ? "Continue" : "Got it!";
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