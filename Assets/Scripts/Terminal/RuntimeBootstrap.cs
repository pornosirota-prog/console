using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class RuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureEventSystem();
        var canvas = BuildCanvas();
        var ui = BuildTerminalUI(canvas.transform);
        var controller = CreateController();
        var audio = CreateAudio(controller.transform);
        ui.view.SetAudio(audio);
        controller.Bind(ui.view, ui.input, audio);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Canvas BuildCanvas()
    {
        var canvasObject = new GameObject("TerminalCanvas", typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static (TerminalView view, TMP_InputField input) BuildTerminalUI(Transform parent)
    {
        var root = new GameObject("TerminalRoot", typeof(RectTransform));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 12;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var background = root.AddComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.06f, 1f);

        var header = CreateHeader(rootRect);
        var (scrollRect, outputText) = CreateScrollView(rootRect);
        var inputField = CreateInput(rootRect);

        var view = root.AddComponent<TerminalView>();
        view.Initialize(outputText, scrollRect);

        return (view, inputField);
    }

    private static GameObject CreateHeader(RectTransform parent)
    {
        var header = new GameObject("Header", typeof(RectTransform));
        var rect = header.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(0, 40);

        var text = header.AddComponent<TextMeshProUGUI>();
        text.text = "TERMINAL LINK v0.1";
        text.fontSize = 28;
        text.color = Color.white;

        var layout = header.AddComponent<LayoutElement>();
        layout.minHeight = 40;

        return header;
    }

    private static (ScrollRect scrollRect, TextMeshProUGUI outputText) CreateScrollView(RectTransform parent)
    {
        var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        var rect = scrollObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(0, 60);
        rect.offsetMax = new Vector2(0, -80);

        var background = scrollObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.09f, 1f);

        var mask = scrollObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(scrollObject.transform, false);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(16, 16);
        contentRect.offsetMax = new Vector2(-32, -16);

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.spacing = 8;
        contentLayout.childForceExpandHeight = false;

        var textObject = new GameObject("Output", typeof(RectTransform));
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(content.transform, false);
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var outputText = textObject.AddComponent<TextMeshProUGUI>();
        outputText.text = string.Empty;
        outputText.color = new Color(0.8f, 1f, 0.85f, 1f);
        outputText.fontSize = 22;
        outputText.textWrappingMode = TextWrappingModes.Normal;
        outputText.alignment = TextAlignmentOptions.TopLeft;

        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = rect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 60f;

        var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.SetParent(scrollObject.transform, false);
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(12, 0);
        scrollbarRect.offsetMin = new Vector2(-20, 8);
        scrollbarRect.offsetMax = new Vector2(-8, -8);

        var scrollbarBackground = scrollbarObject.GetComponent<Image>();
        scrollbarBackground.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.SetParent(scrollbarObject.transform, false);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = new Vector2(2, 2);
        handleRect.offsetMax = new Vector2(-2, -2);

        var handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.65f, 0.92f, 0.75f, 0.9f);

        var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -8f;

        var layout = scrollObject.AddComponent<LayoutElement>();
        layout.flexibleHeight = 1f;
        layout.minHeight = 200f;

        return (scrollRect, outputText);
    }

    private static TMP_InputField CreateInput(RectTransform parent)
    {
        var inputObject = new GameObject("CommandInput", typeof(RectTransform), typeof(Image));
        var rect = inputObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(0, 50);

        var background = inputObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        var textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.SetParent(inputObject.transform, false);
        textAreaRect.anchorMin = new Vector2(0, 0);
        textAreaRect.anchorMax = new Vector2(1, 1);
        textAreaRect.offsetMin = new Vector2(12, 10);
        textAreaRect.offsetMax = new Vector2(-12, -10);

        var textComponent = textArea.AddComponent<TextMeshProUGUI>();
        textComponent.text = string.Empty;
        textComponent.fontSize = 22;
        textComponent.color = Color.white;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;

        var placeholderObject = new GameObject("Placeholder", typeof(RectTransform));
        var placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.SetParent(textArea.transform, false);
        placeholderRect.anchorMin = new Vector2(0, 0);
        placeholderRect.anchorMax = new Vector2(1, 1);
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        var placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter command...";
        placeholder.fontSize = 22;
        placeholder.color = new Color(0.6f, 0.65f, 0.7f, 0.8f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        var input = inputObject.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = textComponent;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 256;
        input.transition = Selectable.Transition.None;
        input.customCaretColor = true;
        input.caretColor = new Color(0.65f, 0.95f, 0.75f, 1f);
        input.caretWidth = 4;
        input.caretBlinkRate = 0.75f;

        var layoutElement = inputObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 50;

        return input;
    }

    private static TerminalSfx CreateAudio(Transform parent)
    {
        var audioObject = new GameObject("TerminalAudio");
        audioObject.transform.SetParent(parent, false);
        return audioObject.AddComponent<TerminalSfx>();
    }

    private static TerminalController CreateController()
    {
        var controllerObject = new GameObject("TerminalController", typeof(TerminalController));
        var controller = controllerObject.GetComponent<TerminalController>();
        return controller;
    }
}
