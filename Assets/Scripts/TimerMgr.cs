//#define __TIMER_MGR_PROFILE__
// author       : jave.lin
// description  : �޲��� Timer
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

// author       : jave.lin
// description  : �޲��� Timer
public class TimerMgr
{
    internal class TimerMonoBehaviour : MonoBehaviour
    {
        internal TimerMgr timer;

        private void Update()
        {
            if (timer != null)
            {
                timer.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    private static TimerMgr _inst;
    public static TimerMgr Inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = new TimerMgr();

                var go = new GameObject($"{typeof(TimerMgr).GetType().Name}");
                var timer_mono_behaviour = go.AddComponent<TimerMonoBehaviour>();
                timer_mono_behaviour.timer = _inst;
                go.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }
    // ����
    internal class TimerInfo
    {
        public int instID = -1;         // Timer ��ʵ�� ID
        public Action on_update;        // ÿ�ֻص��ķ������÷��������� boxing
        public Action on_complete;      // ��ɻص��ķ������÷��������� boxing
        public float interval;          // ִ�����ڣ��룩
        public int repeat;              // <=0 ���޴�
        public int act_times;           // ִ�й��Ĵ���
        public bool remove;             // �Ƿ���Ҫɾ���ı�ʾ
        public float elapsed;           // �ѹ�ʱ��
        public bool with_time_scale;    // �Ƿ�Ӧ�� time scale

        public void Clear()
        {
            on_update = null;
            on_complete = null;
        }
    }

    private static List<TimerInfo> timer_list = new List<TimerInfo>(1000);      // Ԥ���� 1000 �� timer ָ��ռ�
    private static List<TimerInfo> adding_list = new List<TimerInfo>(100);      // Ԥ���� 100 �� timer ָ��ռ�

    private static Stack<TimerInfo> timer_pool = new Stack<TimerInfo>();

    private static int _s_inst_id = 0;

    // ���� Timer ʵ�� ID
    public int AddTimer(
        Action on_update,
        Action on_complete = null,
        float interval = 1.0f, int repeat = 1, bool with_time_scale = false)
    {
        TimerInfo timer = timer_pool.Count > 0 ? timer_pool.Pop() : new TimerInfo();
        timer.instID = timer.instID == -1 ? ++_s_inst_id : timer.instID;
        timer.on_update = on_update;
        timer.on_complete = on_complete;
        timer.interval = interval;
        timer.repeat = repeat;
        timer.act_times = 0;
        timer.with_time_scale = with_time_scale;
        timer.remove = false;
        adding_list.Add(timer);
        return timer.instID;
    }

    // �Ƴ� callback ��ͬʵ���ĵ��� Timer���ɹ����� True�����򷵻� false
    public bool RemoveFirstTimerByCallback(Action callback)
    {
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].on_update == callback)
            {
                timer_list[i].remove = true;
                return true;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].on_update == callback)
            {
                adding_list[i].remove = true;
                return true;
            }
        }
        return false;
    }

    // �Ƴ� callback ��ͬʵ���Ķ�� Timer���Ƴ��ɹ������� > 0 ���Ƴ� Timer �����������򷵻� 0
    public int RemoveAllTimerByCallback(Action callback)
    {
        var remove_count = 0;
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].on_update == callback)
            {
                timer_list[i].remove = true;
                ++remove_count;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].on_update == callback)
            {
                adding_list[i].remove = true;
                ++remove_count;
            }
        }
        return remove_count;
    }

    // �Ƴ�ָ�� ʵ�� ID �� timer���ɹ����� True�����򷵻� false
    public bool RemoveTimerById(int id)
    {
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].instID == id)
            {
                timer_list[i].remove = true;
                return true;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].instID == id)
            {
                adding_list[i].remove = true;
                return true;
            }
        }
        return false;
    }
    private void Update(float deltaTime_with_timescale, float deltaTime_without_timescale)
    {
#if __TIMER_MGR_PROFILE__
        Profiler.BeginSample("TimerMgr.Update 111");
#endif
        // add
        if (adding_list.Count > 0)
        {
#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr.Update 222");
#endif
            var len = adding_list.Count;
            for (int i = 0; i < len; i++)
            {
                var timer = adding_list[i];
                if (timer.remove) // ��û��ӽ���֮ǰ���ֱ�ɾ����
                {
                    timer.Clear();
                    timer_pool.Push(timer);
                }
                else // ���������Ч�� timer ����ӵ������б���
                {
                    timer_list.Add(timer);
                }
            }
            adding_list.Clear();
#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif
        }

        int count = timer_list.Count;
        if (count > 0)
        {
#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr.Update 333");
#endif
            // update
            for (int i = 0; i < count; i++)
            {
                var timer = timer_list[i];
                if (timer.remove)
                {
                    continue;
                }
                if (timer.repeat > 0)
                {
                    if (timer.act_times >= timer.repeat)
                    {
                        timer.remove = true;
                        timer.on_complete?.Invoke();
                        continue;
                    }
                }

                var apply_time = timer.with_time_scale ? deltaTime_with_timescale : deltaTime_without_timescale;

                timer.elapsed += apply_time;
                if (timer.elapsed >= timer.interval)
                { // ������ʱ������֡����
                    timer.elapsed = timer.elapsed % timer.interval;
                    timer.on_update.Invoke();
                    ++timer.act_times;
                }
            }

#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif

#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr.Update 444");
#endif
            // remove
            var idx = -1;
            for (int i = 0; i < count; i++)
            {
                var timer = timer_list[i];
                if (timer.remove)
                {
                    timer.Clear();
                    timer_pool.Push(timer);
                    continue;
                }
                else
                {
                    ++idx;
                }
                timer_list[idx] = timer;
            }
            idx += 1;
            if (idx < count)
            {
                timer_list.RemoveRange(idx, count - idx);
            }
#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif
        }

#if __TIMER_MGR_PROFILE__
        Profiler.EndSample();
#endif
    }
}

public interface ITimerUpdate
{
    void Update(float deltaTime_with_timescale, float deltaTime_without_timescale);
}
internal class ITimerUpdateMono : MonoBehaviour
{
    internal ITimerUpdate timer;

    private void Update()
    {
        if (timer != null)
        {
            timer.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}

// author       : jave.lin
// description  : �в��� Timer��Ҳ����ʹ�� object ���͵Ĳ��������� object Ƶ���� boxing/unboxing �ᵼ�¹��� GC.Alloc ������ GC.Collect
public class TimerMgr<T> : ITimerUpdate
{
    private static TimerMgr<T> _inst;
    public static TimerMgr<T> Inst
    {
        get
        {
            if (_inst == null)
            {
                _inst = new TimerMgr<T>();

                var go = new GameObject($"{typeof(TimerMgr<T>).GetType().Name}");
                var timer_mono_behaviour = go.AddComponent<ITimerUpdateMono>();
                timer_mono_behaviour.timer = _inst;
                go.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }
    // ����
    internal class TimerInfo
    {
        public int instID = -1;         // Timer ��ʵ�� ID
        public Action<T> on_update;     // ÿ�ֻص��ķ������÷��������� boxing
        public T on_update_arg;         // ÿ�ֻص��ķ�������
        public Action<T> on_complete;   // ��ɻص��ķ������÷��������� boxing
        public T on_complete_arg;       // ��ɻص��ķ�������
        public float interval;          // ִ�����ڣ��룩
        public int repeat;              // <=0 ���޴�
        public int act_times;           // ִ�й��Ĵ���
        public bool remove;             // �Ƿ���Ҫɾ���ı�ʾ
        public float elapsed;           // �ѹ�ʱ��
        public bool with_time_scale;    // �Ƿ�Ӧ�� time scale

        public void Clear()
        {
            on_update = null;
            on_update_arg = default(T);
            on_complete = null;
            on_complete_arg = default(T);
        }
    }

    private static List<TimerInfo> timer_list = new List<TimerInfo>(1000);      // Ԥ���� 1000 �� timer ָ��ռ�
    private static List<TimerInfo> adding_list = new List<TimerInfo>(100);      // Ԥ���� 100 �� timer ָ��ռ�

    private static Stack<TimerInfo> timer_pool = new Stack<TimerInfo>();

    private static int _s_inst_id = 0;

    // ���� Timer ʵ�� ID
    // �����ʹ�÷����޲����� TimerMgr����������ʹ�� TimerMgr<T> ����ΪAction<T> callback ����������� GC.Alloc
    // ����޷������£�ֻ��ʹ�� TimerMgr<T> �Ļ����ڵ��ò�Ƶ��������ֻ�����һ�εĵط�������ʹ�� lambda �������������Ļ��Ԥ�ȶ���ĺ�������һЩ���ر��� IL2CPP��
    // �ر�ע����ǣ������� TimerMgr����his TimerMgr<T>���� AddTimer ʱ��������ʹ�ñհ�
    // ���ȷ��һ�����������Ǳհ������������������ú��������ʱ����
    // ��˼�ǣ�
    // - �����Ԥ���巽�������� GC�����£�
    // Ԥ���壺         private void OnUpdate<T>(T arg){ } 
    // ����Ԥ���壺     TimerMgr<T>.Inst.AddTimer(OnUpdate);
    // - ���������������ע�ⲻ�Ǳհ�������û GC�����£�
    // ��������������   TimerMgr<T>.Inst.AddTimer(arg => { });
    // �ɲο���https://docs.unity3d.com/cn/current/Manual/BestPracticeUnderstandingPerformanceInUnity4-1.html �µ� ��IL2CPP �µ�����������
    public int AddTimer(
        Action<T> on_update, T on_update_arg = default(T),
        Action<T> on_complete = null, T on_complete_arg = default(T),
        float interval = 1.0f, int repeat = 1, bool with_time_scale = false)
    {
        TimerInfo timer = timer_pool.Count > 0 ? timer_pool.Pop() : new TimerInfo();
        timer.instID = timer.instID == -1 ? ++_s_inst_id : timer.instID;
        timer.on_update = on_update;
        timer.on_update_arg = on_update_arg;
        timer.on_complete = on_complete;
        timer.on_complete_arg = on_complete_arg;
        timer.interval = interval;
        timer.repeat = repeat;
        timer.act_times = 0;
        timer.with_time_scale = with_time_scale;
        timer.remove = false;
        adding_list.Add(timer);
        return timer.instID;
    }

    // �Ƴ� callback ��ͬʵ���ĵ��� Timer���ɹ����� True�����򷵻� false
    public bool RemoveFirstTimerByCallback(Action<T> callback)
    {
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].on_update == callback)
            {
                timer_list[i].remove = true;
                return true;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].on_update == callback)
            {
                adding_list[i].remove = true;
                return true;
            }
        }
        return false;
    }

    // �Ƴ� callback ��ͬʵ���Ķ�� Timer���Ƴ��ɹ������� > 0 ���Ƴ� Timer �����������򷵻� 0
    public int RemoveAllTimerByCallback(Action<T> callback)
    {
        var remove_count = 0;
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].on_update == callback)
            {
                timer_list[i].remove = true;
                ++remove_count;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].on_update == callback)
            {
                adding_list[i].remove = true;
                ++remove_count;
            }
        }
        return remove_count;
    }

    // �Ƴ�ָ�� ʵ�� ID �� timer���ɹ����� True�����򷵻� false
    public bool RemoveTimerById(int id)
    {
        var count = timer_list.Count;
        // �����е��б�
        for (int i = 0; i < count; i++)
        {
            if (timer_list[i].instID == id)
            {
                timer_list[i].remove = true;
                return true;
            }
        }

        // ��Ӷ��е��б�
        count = adding_list.Count;
        for (int i = 0; i < count; i++)
        {
            if (adding_list[i].instID == id)
            {
                adding_list[i].remove = true;
                return true;
            }
        }
        return false;
    }
    public void Update(float deltaTime_with_timescale, float deltaTime_without_timescale)
    {
#if __TIMER_MGR_PROFILE__
        Profiler.BeginSample("TimerMgr<T>.Update 111");
#endif
        // add
        if (adding_list.Count > 0)
        {
#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr<T>.Update 222");
#endif
            var len = adding_list.Count;
            for (int i = 0; i < len; i++)
            {
                var timer = adding_list[i];
                if (timer.remove) // ��û��ӽ���֮ǰ���ֱ�ɾ����
                {
                    timer.Clear();
                    timer_pool.Push(timer);
                }
                else // ���������Ч�� timer ����ӵ������б���
                {
                    timer_list.Add(timer);
                }
            }
            adding_list.Clear();
#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif
        }

        int count = timer_list.Count;
        if (count > 0)
        {
#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr<T>.Update 333");
#endif
            // update
            for (int i = 0; i < count; i++)
            {
                var timer = timer_list[i];
                if (timer.remove)
                {
                    continue;
                }
                if (timer.repeat > 0)
                {
                    if (timer.act_times >= timer.repeat)
                    {
#if __TIMER_MGR_PROFILE__
                        Profiler.BeginSample("TimerMgr<T>.Update 333.111");
#endif
                        timer.remove = true;
                        timer.on_complete?.Invoke(timer.on_complete_arg);
#if __TIMER_MGR_PROFILE__
                        Profiler.EndSample();
#endif
                        continue;
                    }
                }

                var apply_time = timer.with_time_scale ? deltaTime_with_timescale : deltaTime_without_timescale;

                timer.elapsed += apply_time;
                if (timer.elapsed >= timer.interval)
                { // ������ʱ������֡����
#if __TIMER_MGR_PROFILE__
                    // ���� Profile ������ GC����Ϊ��ͷ�Ļص��������� GC ����
                    Profiler.BeginSample("TimerMgr<T>.Update 333.222");
#endif
                    timer.elapsed = timer.elapsed % timer.interval;
                    timer.on_update.Invoke(timer.on_update_arg);
                    ++timer.act_times;
#if __TIMER_MGR_PROFILE__
                    Profiler.EndSample();
#endif
                }
            }
#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif

#if __TIMER_MGR_PROFILE__
            Profiler.BeginSample("TimerMgr<T>.Update 444");
#endif
            // remove
            var idx = -1;
            for (int i = 0; i < count; i++)
            {
                var timer = timer_list[i];
                if (timer.remove)
                {
                    timer.Clear();
                    timer_pool.Push(timer);
                    continue;
                }
                else
                {
                    ++idx;
                }
                timer_list[idx] = timer;
            }
            idx += 1;
            if (idx < count)
            {
                timer_list.RemoveRange(idx, count - idx);
            }
#if __TIMER_MGR_PROFILE__
            Profiler.EndSample();
#endif
        }
#if __TIMER_MGR_PROFILE__
        Profiler.EndSample();
#endif
    }
}