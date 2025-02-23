using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuthorTimeHunting.UI;

public class UIBuilder
{
    private GameObject currentObject;
    private RectTransform currentRectTransform;

    public static UIBuilder Begin()
    {
        return new UIBuilder();
    }

    public UIBuilder Canvas(string name = "Canvas", RenderMode renderMode = RenderMode.ScreenSpaceOverlay)
    {
        currentObject = new GameObject(name);
        Canvas canvas = currentObject.AddComponent<Canvas>();
        canvas.renderMode = renderMode;
        currentObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        currentObject.AddComponent<GraphicRaycaster>();
        currentRectTransform = currentObject.GetComponent<RectTransform>();

        return this;
    }

    public UIBuilder Panel(string name = "Panel", Color? backgroundColor = null, Vector2 size = default)
    {
        GameObject panelGO = new GameObject(name);
        panelGO.transform.SetParent(currentObject.transform, false);

        currentRectTransform = panelGO.AddComponent<RectTransform>();
        currentRectTransform.sizeDelta = size == default ? new Vector2(400, 200) : size;
        currentRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        currentRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        currentRectTransform.pivot = new Vector2(0.5f, 0.5f);
        currentRectTransform.anchoredPosition = Vector2.zero;

        Image image = panelGO.AddComponent<Image>();
        image.color = backgroundColor ?? Color.gray;

        currentObject = panelGO;
        return this;
    }

    public UIBuilder Button(string name = "Button", string text = "Click Me", Color? buttonColor = null, Vector2 size = default, Action onClick = null)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(currentObject.transform, false);

        currentRectTransform = buttonGO.AddComponent<RectTransform>();
        currentRectTransform.sizeDelta = size == default ? new Vector2(200, 50) : size;
        currentRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        currentRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        currentRectTransform.pivot = new Vector2(0.5f, 0.5f);
        currentRectTransform.anchoredPosition = Vector2.zero;

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonGO.AddComponent<Image>();
        button.GetComponent<Image>().color = buttonColor ?? Color.green;

        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.text = text;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.color = Color.black;
        buttonText.resizeTextForBestFit = true;

        currentObject = buttonGO;
        return this;
    }

    public UIBuilder MakeDraggable(Action<Vector2> onDragFinished = null)
    {
        UIDragHandler dragHandler = currentObject.AddComponent<UIDragHandler>();
        dragHandler.OnEndDrag = onDragFinished;
        return this;
    }

    public UIBuilder SetPosition(Vector2 position)
    {
        currentRectTransform.anchoredPosition = position;
        return this;
    }

    public GameObject Build()
    {
        return currentObject;
    }
}