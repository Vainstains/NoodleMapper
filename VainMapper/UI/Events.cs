using System;
using UnityEngine;
using UnityEngine.Events;
using VainLib.UI;
using VainMapper.Utils;

namespace VainMapper.UI;

public static class Events
{
    public static UnityEvent ExtensionButtonClicked = new ();
    public static UnityEvent ExtrasClicked = new ();
}
