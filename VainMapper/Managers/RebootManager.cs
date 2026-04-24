using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VainLib.Scenes;
using VainMapper.Utils;

namespace VainMapper.Managers;

public class RebootManager : ManagerBehaviour<RebootManager>
{
    protected override void PostInit() { }

    private void Start()
    {
        StartCoroutine(Rebooted());
    }

    private IEnumerator Rebooted()
    {
        Debug.Log("Reboot: waiting for dialog box to close...");
        bool waiting = true;
        yield return new WaitForSeconds(0.2f);
        while (waiting)
        {
            waiting = GameObject.Find("Dialog Box Template(Clone)") != null;
            Debug.Log($"Reboot: (isOpen: {waiting})");
            yield return new WaitForSecondsRealtime(0.05f);
        }
        
        Rebooter.HandleReboot();
    }
}