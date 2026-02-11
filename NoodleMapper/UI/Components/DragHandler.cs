using System;
using System.Collections.Generic;
using System.Linq;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

public class DragHandler : MonoBehaviour, IDragHandler
{
    private Action<PointerEventData>? m_onDrag;

    public void Init(Action<PointerEventData> onDrag)
    {
        m_onDrag = onDrag;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        m_onDrag?.Invoke(eventData);
    }
}

public class NoodleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerExitHandler
{
    public Color MainColor;

    private Action? m_onClick;
    private Image? m_image;

    public Sprite Sprite
    {
        get => m_image!.sprite;
        set => SetSprite(value);
    }

    private void SetSprite(Sprite? sprite)
    {
        m_image!.sprite = sprite;
        if (sprite == null)
        {
            m_image.type = Image.Type.Simple;
            return;
        }

        m_image!.type = sprite.border.sqrMagnitude > 0.5 ? Image.Type.Sliced : Image.Type.Simple;
    }

    private void Init(Color color, Action onClick)
    {
        MainColor = color;
        m_onClick = onClick;
        m_image = gameObject.AddComponent<Image>();
        SetSprite(StaticAssets.RoundRect);
        m_image.color = MainColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        m_onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor + new Color(0.18f, 0.18f, 0.18f, 0.0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_image != null)
            m_image.color = MainColor;
    }
}

public class NoodleDropdown : MonoBehaviour
{
    private TMP_Dropdown m_dropdown = null!;
    private Action<int>? m_onChange;
    private void Init()
    {
        m_dropdown = gameObject.AddComponent<TMP_Dropdown>();
        var rt = gameObject.RequireComponent<RectTransform>();
        var img = rt.AddImage(StaticAssets.RoundRectBordered, new Color(0.2f, 0.2f, 0.2f, 1f));

        var label = rt.AddChild().InsetLeft(4).AddLabel("option");

        var template = rt.AddChild(RectTransform.Edge.Bottom).ExtendBottom(100);

        template.AddImage(StaticAssets.RoundRectBordered, new Color(0.23f, 0.23f, 0.23f, 1f));

        var templateItem = template.AddChild(RectTransform.Edge.Top).ExtendBottom(20);
        var itemLabel = templateItem.AddChild().InsetLeft(4).AddLabel("item");
        var itemBg = templateItem.AddImage(StaticAssets.RoundRect, new Color(0.25f, 0.4f, 0.8f, 1.0f));
        var toggle = templateItem.RequireComponent<Toggle>();
        toggle.graphic = itemBg;
        toggle.isOn = true;
        toggle.transition = Selectable.Transition.None;
        
        template.gameObject.SetActive(false);

        m_dropdown.template = template;
        m_dropdown.targetGraphic = img;
        m_dropdown.captionText = label;
        m_dropdown.itemText = itemLabel;
    }
    public void SetOptions(IEnumerable<string> options)
    {
        m_dropdown.options.Clear();
        foreach (var option in options)
        {
            m_dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }
    }

    public void SetSelectedOption(int index)
    {
        m_dropdown.SetValueWithoutNotify(index);
    }
}

public class NoodleTextbox : MonoBehaviour
{
    public delegate void Setter(string? s);

    public TMP_InputField InputField = null!;
    public TMP_Text PlaceholderText = null!;

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
    
    public static NoodleTextbox Create(RectTransform parent, bool tall = false)
    {
        var input = parent.AddInputFieldRaw("", "", tall ? 12 : 14);

        var wrapper = input.gameObject.AddComponent<NoodleTextbox>();
        wrapper.Init(input);

        return wrapper;
    }

    private void Init(TMP_InputField field)
    {
        InputField = field;
        PlaceholderText = (TMP_Text)field.placeholder;

        InputField.onSelect.AddListener(_ => StartEditing());

        InputField.onEndEdit.AddListener(s =>
        {
            if (s != _value)
                OnChange?.Invoke(s);
        });

        InputField.onDeselect.AddListener(_ => EndEditing());
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
