using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using VainLib.Utils;

namespace VainLib.UI.Components;

public class NoodleTextbox : MonoBehaviour
{
    private static readonly Color OutlineColorIdle = new Color(0.33f, 0.33f, 0.33f);
    private static readonly Color OutlineColorEditing = new Color(0.3f, 0.4f, 0.6f);
    public delegate void Setter(string? s);

    public TMP_InputField InputField = null!;
    public TMP_Text PlaceholderText = null!;
    private Image m_outline = null!;

    public Setter? OnChange;

    private string _value = "";

    public string Value
    {
        get => InputField.text;
        set
        {
            _value = value ?? "";
            InputField.SetTextWithoutNotify(_value);
        }
    }

    public string Placeholder
    {
        set => PlaceholderText.text = value;
    }

    public bool Modified => InputField.text != _value;
    
    public static NoodleTextbox Create(RectTransform parent)
    {
        var input = parent.AddInputFieldRaw("", "");

        var wrapper = input.gameObject.AddComponent<NoodleTextbox>();
        wrapper.Init(input);

        return wrapper;
    }

    private void Init(TMP_InputField field)
    {
        InputField = field;
        
        var text = InputField.textComponent;
        PlaceholderText = (TMP_Text)field.placeholder;

        text.alignment = PlaceholderText.alignment = TextAlignmentOptions.Left;
        var margin = text.margin;
        margin.x += 4;
        text.margin = margin;
        
        margin = PlaceholderText.margin;
        margin.x += 4;
        PlaceholderText.margin = margin;

        InputField.onSelect.AddListener(_ => StartEditing());

        InputField.onEndEdit.AddListener(s =>
        {
            if (s != _value)
                OnChange?.Invoke(s);
        });

        InputField.onDeselect.AddListener(_ => EndEditing());
        
        var rt = GetComponent<RectTransform>();

        m_outline = rt.AddChild()
            .AddImage(DefaultResources.LoadSprite("Resources/RoundRectBorderOnly.png"), OutlineColorIdle)
            .DisableRaycasts();
    }

    public NoodleTextbox Set(string? value, bool mixed, Setter setter)
    {
        Value = value ?? "";
        OnChange = setter;
        Placeholder = mixed ? "Mixed" : "Empty";
        return this;
    }

    public void Select()
    {
        InputField.ActivateInputField();
    }

    public NoodleTextbox SetOnChange(Setter callback)
    {
        OnChange = callback;
        return this;
    }

    public NoodleTextbox SetValue(string value)
    {
        Value = value;
        return this;
    }

    private void StartEditing()
    {
        last_selected = this;

        CMInputCallbackInstaller.DisableActionMaps(
            typeof(NoodleTextbox),
            new[] { typeof(CMInput.INodeEditorActions) }
        );

        CMInputCallbackInstaller.DisableActionMaps(
            typeof(NoodleTextbox),
            ActionMapsDisabled
        );

        m_outline.color = OutlineColorEditing;
    }

    private void EndEditing()
    {
        if (last_selected == this)
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(
                typeof(NoodleTextbox),
                new[] { typeof(CMInput.INodeEditorActions) }
            );

            CMInputCallbackInstaller.ClearDisabledActionMaps(
                typeof(NoodleTextbox),
                ActionMapsDisabled
            );
        }
        
        m_outline.color = OutlineColorIdle;
    }

    private void OnDestroy()
    {
        EndEditing();
    }

    private bool disabledCheck()
    {
        if (this && isActiveAndEnabled) return false;
        EndEditing();
        return true;
    }

    
    private static readonly Type[] actionMapsEnabledWhenEditing =
    {
        typeof(CMInput.ICameraActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INodeEditorActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.ITimelineActions)
    };

    private static Type[] ActionMapsDisabled =>
        typeof(CMInput).GetNestedTypes()
            .Where(x => x.IsInterface && !actionMapsEnabledWhenEditing.Contains(x))
            .ToArray();

    private static InputAction? tab_next;
    private static InputAction? tab_back;
    private static NoodleTextbox? last_selected;

    public static void InitInputActions(InputAction next, InputAction back)
    {
        tab_next = next;
        tab_back = back;
    }
}

/// <summary>
/// A dedicated multimodal input for numbers.
/// Dragging left/right will change the value, clicking will prompt directly via a popup.
/// </summary>
public class NoodleValueInput : MonoBehaviour, IPointerClickHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color BackgroundColor = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color BackgroundColorHover = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color BackgroundColorDrag = new Color(0.3f, 0.4f, 0.6f);
    private static readonly Color TextColor = Color.white;

    private const string BackgroundSprite = "Resources/RoundRectBordered.png";

    private Image m_image = null!;
    private TextMeshProUGUI m_text = null!;

    private float m_default = 0;

    private float m_min = float.MinValue;
    private float m_max = float.MaxValue;
    private float m_step = 0;

    private float m_currentValue = 0;
    private float m_queuedValue = 0;
    private float m_dragStartValue;
    private Vector2 m_dragStartPointer;

    private bool m_isDragging = false;
    private bool m_isHovering = false;

    private Action<float>? m_onChange;

    private void Init()
    {
        var rt = GetComponent<RectTransform>();
        m_image = rt.AddImage(DefaultResources.LoadSprite(BackgroundSprite), BackgroundColor);
        m_text = rt.AddChild().AddLabel("<value>", TextAlignmentOptions.Center, 17, TextColor, TextOverflowModes.Overflow);
        m_text.enableWordWrapping = false;
        m_text.raycastTarget = false;
    }

    private float SnapToStep(float value)
    {
        if (value < m_min)
            return m_min;
        if (value > m_max)
            return m_max;
        
        if (m_step <= 0.00001f)
            return value;
        
        return Mathf.Round(value / m_step) * m_step;
    }
    
    public NoodleValueInput SetValue(float value)
    {
        m_currentValue = value;
        m_queuedValue = value;
        m_text.text = value.ToString("F2", CultureInfo.InvariantCulture);
        return this;
    }

    private void SetValueAndNotify(float value)
    {
        m_currentValue = value;
        m_queuedValue = value;
        m_text.text = value.ToString("F2", CultureInfo.InvariantCulture);
        m_onChange?.Invoke(value);
    }

    public NoodleValueInput SetMinMax(float min = float.MinValue, float max = float.MaxValue)
    {
        m_min = min;
        m_max = max;
        return this;
    }

    public NoodleValueInput SetStep(float step = 0)
    {
        m_step = step;
        return this;
    }

    public NoodleValueInput SetOnChange(Action<float> onChange)
    {
        m_onChange = onChange;
        return this;
    }

    public NoodleValueInput SetDefault(float value)
    {
        m_default = value;
        return this;
    }

    private void UpdateColors()
    {
        if (m_isDragging)
            m_image.color = BackgroundColorDrag;
        else if (m_isHovering)
            m_image.color = BackgroundColorHover;
        else
            m_image.color = BackgroundColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {   
        if (m_isDragging)
            return;

        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            SetValueAndNotify(m_default);
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PersistentUI.Instance.DoShowInputBox("Enter a number", res =>
        {
            if (float.TryParse(res, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                SetValueAndNotify(value);
            }
        }, m_currentValue.ToString(CultureInfo.InvariantCulture));

        UpdateColors();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_isDragging)
        {
            m_isDragging = true;
            UpdateColors();

            m_dragStartValue = m_currentValue;
            m_dragStartPointer = eventData.position;
        }

        float deltaX = eventData.position.x - m_dragStartPointer.x;

        const float pixelsPerStep = 20f;

        float value = m_dragStartValue + (deltaX / pixelsPerStep);

        m_queuedValue = value;

        var snappedValue = SnapToStep(value);

        m_text.text = snappedValue.ToString("F2", CultureInfo.InvariantCulture);
        
        Debug.Log($"[{nameof(NoodleValueInput)}] Drag event. startValue: {m_dragStartValue}, startX: {m_dragStartPointer.x}, deltaX: {deltaX}, value: {value}, snappedValue: {snappedValue}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_isDragging = false;
        UpdateColors();

        SetValueAndNotify(SnapToStep(m_queuedValue));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_isHovering = true;
        UpdateColors();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_isHovering = false;
        UpdateColors();
    }
}