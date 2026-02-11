using Beatmap.Helper;
using UnityEngine;

namespace NoodleMapper;

public class NoodleMapperManager : MonoBehaviour
{
    private static NoodleMapperManager? s_instance;
        
    public static NoodleMapperManager? Instance => s_instance;

    private void Init()
    {
        if (s_instance)
            Destroy(s_instance!.gameObject);
            
        s_instance = this;
        
        var map = BeatSaberSongContainer.Instance.Map;
        if (!map.CustomData.HasKey("noodleMapper"))
        {
            
        }
    }
        
        

    public void OnExtensionButtonClicked()
    {
        var map = BeatSaberSongContainer.Instance.Map;
        Debug.LogError(map.DirectoryAndFile);

        var notes = map.Notes.ToArray();
        
        foreach (var o in notes) {
            var orig = BeatmapFactory.Clone(o);
            
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(o.ObjectType);
					
            collection.DeleteObject(o, false, false, "", true, false);
            
            o.CustomData[o.CustomKeyNoteJumpMovementSpeed] = 69;
            o.RefreshCustom();
					
            collection.SpawnObject(o, false, true);
        }
        
        BeatmapObjectContainerCollection.RefreshAllPools();
    }
}