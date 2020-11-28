using System.Collections.Generic;
using UnityEngine;

// author   : jave.lin
// active、deactive 的工具管理类
// 避免 GameObject 过于频繁的 active、deactive
public class ActiveUtil : MonoBehaviour
{
    public class DeactiveInfo
    {
        public int instance_id;
        public GameObject go;
        public Transform trans;
        public Vector3 src_pos;
        public float time;

        public void Clear()
        {
            go = null;
            trans = null;
        }
    }
    // 不可见的位置
    private static readonly Vector3 _invisible_pos = new Vector3(99999.0f, 0.0f, 0.0f);
    // 多少秒后设置为 deactive
    private const float apply_active_after_dur = 5.0f;
    // 每帧最多 deactive 多少个，避免并发 deactive
    private const int deactive_max_count_per_frame = 2;
    // 单例
    private static ActiveUtil _inst;
    public static ActiveUtil Inst
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject($"{typeof(ActiveUtil).Name}");
                go.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<ActiveUtil>();
            }
            return _inst;
        }
    }
    private Dictionary<int, DeactiveInfo> _TFQI_dict = new Dictionary<int, DeactiveInfo>();
    private List<DeactiveInfo> _TFQI_list = new List<DeactiveInfo>();
    private Stack<DeactiveInfo> _TFQI_pool = new Stack<DeactiveInfo>();
    private void Awake()
    {
        if (_inst != null)
        {
            Debug.LogError($"On Awake, {typeof(ActiveUtil).Name}.Inst != null");
        }
        _inst = this;
    }
    private void Update()
    {
        var count = _TFQI_list.Count;
        if (count > 0)
        {
            var handle_count = 0;
            for (int i = 0; i < count; i++)
            {
                var item = _TFQI_list[i];
                if (item.go == null)
                { // GameObject 外部销毁了
                    _TFQI_list.RemoveAt(i);
                    --i;
                    --count;
                    continue;
                }
                if ((Time.realtimeSinceStartup - item.time) > apply_active_after_dur)
                {
                    // 到时
                    // Debug.Log($"after {apply_active_after_dur}s, set active : false");
                    item.go.SetActive(false);
                    // 出队
                    _TFQI_list.RemoveAt(i);
                    // 回收
                    ToPool(item);
                    --i;
                    --count;
                    // 每帧 deactive 的数量有限制
                    if (++handle_count > deactive_max_count_per_frame)
                    {
                        break;
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        _inst = null;
        if (_TFQI_dict != null)
        {
            _TFQI_dict.Clear();
            _TFQI_dict = null;
        }
        if (_TFQI_list != null)
        {
            _TFQI_list.Clear();
            _TFQI_list = null;
        }
        if (_TFQI_pool != null)
        {
            _TFQI_pool.Clear();
            _TFQI_pool = null;
        }
    }
    private DeactiveInfo FromPool(GameObject go)
    {
        var ret = _TFQI_pool.Count > 0 ? _TFQI_pool.Pop() : new DeactiveInfo();
        ret.instance_id = go.GetInstanceID();
        ret.go = go;
        ret.trans = go.transform;
        ret.src_pos = ret.trans.position;
        return ret;
    }
    private void ToPool(DeactiveInfo info)
    {
        _TFQI_dict.Remove(info.instance_id);
        info.Clear();
        _TFQI_pool.Push(info);
    }
    public void Active(GameObject go, bool recovery_src_pos = true)
    {
        if (go == null)
        {
            return;
        }

        if (!go.activeSelf)
        {
            go.SetActive(true);
        }

        if (_TFQI_dict.TryGetValue(go.GetInstanceID(), out DeactiveInfo info))
        {
            if (recovery_src_pos && info.trans != null)
            {
                info.trans.position = info.src_pos;
                info.Clear();
            }
            ToPool(info);
        }
    }
    public void Deactive(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        _TFQI_dict.TryGetValue(go.GetInstanceID(), out DeactiveInfo info);
        if (info == null)
        {
            info = FromPool(go);
            _TFQI_dict[go.GetInstanceID()] = info;
            _TFQI_list.Add(info);
            info.time = Time.realtimeSinceStartup;
        }
        else
        {
            info.go = go;
            info.trans = go.transform;
            info.src_pos = info.trans.position;
        }
        info.trans.position = _invisible_pos;
    }
    public void Clear()
    {
        foreach (var item in _TFQI_list)
        {
            ToPool(item);
        }
        _TFQI_dict.Clear();
        _TFQI_list.Clear();
    }
}
