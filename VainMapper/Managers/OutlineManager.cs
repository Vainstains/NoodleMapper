using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using VainLib.Scenes;

namespace VainMapper.Managers;

public class OutlineManager : ManagerBehaviour<OutlineManager>
{
    private readonly struct ObjectKey
    {
        // note
        public ObjectKey(ObjectType objectType, float t, int x, int y, int cutDirection, int angleOffset, int color)
        {
            Time = t;
            PosX = x;
            PosY = y;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            ObjectType = objectType;
            Color = color;
        }

        // wall
        public ObjectKey(ObjectType objectType, float t, int x, int y, int wallHeight, int wallWidth, float wallDuration)
        {
            Time = t;
            PosX = x;
            PosY = y;
            WallHeight = wallHeight;
            WallWidth = wallWidth;
            WallDuration = wallDuration;
            ObjectType = objectType;
        }

        // chain
        public ObjectKey(ObjectType objectType, float t, int x, int y, int cutDirection, int tailX, int tailY, float sliderSquishAmount, int sliderLinks, int color)
        {
            Time = t;
            PosX = x;
            PosY = y;
            CutDirection = cutDirection;
            TailX = tailX;
            TailY = tailY;
            SliderSquishAmount = sliderSquishAmount;
            SliderLinks = sliderLinks;
            ObjectType = objectType;
            Color = color;
        }

        // arc
        public ObjectKey(ObjectType objectType, float t, int x, int y, int cutDirection, int headWeight, int tx, int ty, float tailTime, int tailWeight, int color)
        {
            Time = t;
            PosX = x;
            PosY = y;
            CutDirection = cutDirection;
            HeadWeight = headWeight;
            TailX = tx;
            TailY = ty;
            TailTime = tailTime;            
            TailWeight = tailWeight;
            ObjectType = objectType;
            Color = color;
        }

        
        public float Time { get; }
        public int PosX { get; }
        public int PosY { get; }
        public int CutDirection { get; }
        public int AngleOffset { get; }

        public ObjectType ObjectType { get; }
        public int Color { get; }

        public int TailX { get; }
        public int TailY { get; }
        public int TailCutDirection { get; }

        public int HeadWeight { get; }
        public int TailWeight { get; }

        public float TailTime { get; }

        public float SliderSquishAmount { get; }
        public int SliderLinks { get; }

        public int WallHeight { get; }
        public int WallWidth { get; }
        public float WallDuration { get; }
    }

    private const string ContainerSpawnedEventName = "ContainerSpawnedEvent";

    private readonly Dictionary<ObjectKey, Color> m_outlineMap = new();
    private readonly Dictionary<BeatmapObjectContainerCollection, Delegate> m_spawnSubscriptions = new();

    protected override void PostInit()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private static bool TryGetKey(BaseObject obj, out ObjectKey key)
    {
        switch (obj)
        {
            case BaseNote note:
                key = new ObjectKey(note.ObjectType,
                note.JsonTime,
                note.PosX, note.PosY,
                note.CutDirection,
                note.AngleOffset,
                (int)note.Color);
                return true;
            case BaseObstacle wall:
                key = new ObjectKey(wall.ObjectType,
                wall.JsonTime,
                wall.PosX, wall.PosY,
                wall.Height,
                wall.Width,
                wall.Duration);
                return true;
            case BaseChain chain:
                key = new ObjectKey(chain.ObjectType,
                chain.JsonTime,
                chain.PosX, chain.PosY,
                chain.CutDirection,
                chain.TailPosX, chain.TailPosY,
                chain.Squish,
                chain.SliceCount,
                (int)chain.Color);
                return true;
            case BaseArc arc:
                key = new ObjectKey(arc.ObjectType,
                arc.JsonTime,
                arc.PosX, arc.PosY,
                arc.CutDirection,
                (int)(arc.HeadControlPointLengthMultiplier*100), // haha quantize lol
                arc.TailPosX, arc.TailPosY,
                arc.TailJsonTime,
                (int)(arc.TailControlPointLengthMultiplier*100),
                (int)arc.Color);
                return true;
            default:
                key = default;
                return false;
        }
    }

    public void SetOutline(BaseObject obj, Color color)
    {
        if (!TryGetKey(obj, out var key))
            return;

        m_outlineMap[key] = color;
    }

    public void ClearOutline(BaseObject obj)
    {
        if (!TryGetKey(obj, out var key))
            return;

        m_outlineMap.Remove(key);
    }

    public void ClearAllOutlines() => m_outlineMap.Clear();

    public void RefreshAllOutlines()
    {
        foreach (var grid in GetAllGridContainers())
            grid.RefreshPool(true);
    }

    private void OnContainerSpawned(BaseObject obj)
    {
        if (!TryGetKey(obj, out var key))
            return;

        var isSelected = SelectionController.IsObjectSelected(obj);

        Color color = Color.white;
        if (!isSelected && !m_outlineMap.TryGetValue(key, out color))
            return;
        
        if (isSelected)
            color = SelectionController.SelectedColor;

        if (obj is BaseNote note)
            EditorManager.Instance.NoteGridContainer.LoadedContainers[obj].SetOutlineColor(color);
        else if (obj is BaseObstacle obstacle)
            EditorManager.Instance.ObstacleGridContainer.LoadedContainers[obj].SetOutlineColor(color);
        else if (obj is BaseSlider slider)
            EditorManager.Instance.ArcGridContainer.LoadedContainers[obj].SetOutlineColor(color);
        else if (obj is BaseBpmEvent bpm)
            EditorManager.Instance.EventGridContainer.LoadedContainers[obj].SetOutlineColor(color);
    }

    public void Subscribe()
    {
        foreach (var grid in GetAllGridContainers())
        {
            if (grid == null || m_spawnSubscriptions.ContainsKey(grid))
                continue;

            if (TryAddContainerSpawnedHandler(grid, out var handler))
                m_spawnSubscriptions[grid] = handler;
        }

        SelectionController.ObjectWasSelectedEvent += OnObjectWasSelected;
        SelectionController.SelectionChangedEvent += OnSelectionUpdated;
    }

    private HashSet<BaseObject> m_selectedObjects = new();

    private void OnObjectWasSelected(BaseObject obj)
    {
        m_selectedObjects.Add(obj);
        OnContainerSpawned(obj);
    }

    private void OnSelectionUpdated()
    {
        foreach (var obj in m_selectedObjects.ToList())
        {
            if (!SelectionController.IsObjectSelected(obj))
            {
                m_selectedObjects.Remove(obj);
                OnContainerSpawned(obj);
            }
        }
    }

    public void Unsubscribe()
    {
        foreach (var pair in m_spawnSubscriptions)
            RemoveContainerSpawnedHandler(pair.Key, pair.Value);

        m_spawnSubscriptions.Clear();

        SelectionController.ObjectWasSelectedEvent -= OnObjectWasSelected;
        SelectionController.SelectionChangedEvent -= OnSelectionUpdated;
    }

    // my publicizer fucked up and made duplicates for some reason so we're doing this the slow way
    private bool TryAddContainerSpawnedHandler(BeatmapObjectContainerCollection grid, out Delegate handler)
    {
        handler = null!;

        var type = grid.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var eventInfo = type.GetEvent(ContainerSpawnedEventName, flags);
        if (eventInfo != null)
        {
            var createdHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, this, nameof(OnContainerSpawned), false);
            if (createdHandler == null)
                return false;

            eventInfo.AddEventHandler(grid, createdHandler);
            handler = createdHandler;
            return true;
        }

        var fieldInfo = type.GetField(ContainerSpawnedEventName, flags);
        if (fieldInfo == null || !typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
            return false;

        var fieldHandler = Delegate.CreateDelegate(fieldInfo.FieldType, this, nameof(OnContainerSpawned), false);
        if (fieldHandler == null)
            return false;

        var currentValue = fieldInfo.GetValue(grid) as Delegate;
        fieldInfo.SetValue(grid, Delegate.Combine(currentValue, fieldHandler));
        handler = fieldHandler;
        return true;
    }

    private void RemoveContainerSpawnedHandler(BeatmapObjectContainerCollection grid, Delegate handler)
    {
        var type = grid.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var eventInfo = type.GetEvent(ContainerSpawnedEventName, flags);
        if (eventInfo != null)
        {
            eventInfo.RemoveEventHandler(grid, handler);
            return;
        }

        var fieldInfo = type.GetField(ContainerSpawnedEventName, flags);
        if (fieldInfo == null || !typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
            return;

        var currentValue = fieldInfo.GetValue(grid) as Delegate;
        fieldInfo.SetValue(grid, Delegate.Remove(currentValue, handler));
    }

    private static IEnumerable<BeatmapObjectContainerCollection> GetAllGridContainers()
    {
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
        yield return BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
    }
}
