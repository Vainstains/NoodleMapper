using System;
using System.Linq;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoodleMapper.UI.Components;

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

        m_outline = rt.AddChild().AddImage(Globals.Assets.RoundRectBorderOnly, OutlineColorIdle).DisableRaycasts();
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