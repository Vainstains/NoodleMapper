using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

namespace VainMapper.Managers.Windows;

public static class BaseObjectHelper
{
    public class BaseObjectCollection : IEnumerable<BaseObject>
    {
        private readonly IEnumerable<BaseObject> m_objects;
        public BaseObjectCollection(IEnumerable<BaseObject> objects) => m_objects = objects;

        public IEnumerator<BaseObject> GetEnumerator()
        {
            return m_objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_objects).GetEnumerator();
        }

        public IEnumerable<BaseNote> ColorNotes => m_objects.Where(o => o.IsColorNote()).Cast<BaseNote>();
        public IEnumerable<BaseNote> Bombs => m_objects.Where(o => o.IsBombNote()).Cast<BaseNote>();
        public IEnumerable<BaseArc> Arcs => m_objects.Where(o => o.IsArc()).Cast<BaseArc>();
        public IEnumerable<BaseChain> Chains => m_objects.Where(o => o.IsChain()).Cast<BaseChain>();
        public IEnumerable<BaseObstacle> Walls => m_objects.Where(o => o.IsWall()).Cast<BaseObstacle>();
        public IEnumerable<BaseEvent> Events => m_objects.Where(o => o.IsEvent()).Cast<BaseEvent>();
    }

    public static BaseObjectCollection Selected => new (SelectionController.SelectedObjects);
    public static BaseObjectCollection Editable => 
        (EditorManager.Instance != null ? new(EditorManager.Instance.EnumerateEditableObjects()) : new([]));
}