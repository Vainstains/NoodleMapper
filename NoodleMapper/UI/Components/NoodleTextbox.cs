using System;
using System.Linq;
using NoodleMapper.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoodleMapper.UI.Components;

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
        var input = parent.AddInputFieldRaw("", "");

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