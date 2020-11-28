using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

/*

author          :   jave.lin
description     :

测试中常见的 GC 问题
- 什么叫 GC?
 GC 是 Garbage Collector 的缩写，意思是：垃圾收集器
- 为何要有 GC?
 因为以往我们在写C/C++ 等相对低级的语言中，程序员可以只对堆内存的分配和释放
 但是由于内存对象的管理异常复杂，特别是业务逻辑繁杂的更加难以管理，时不时就会出现一个：“0xXXXXXX 内存不可访问”、“Out of Memory”，之类的错误提示
- 所以在 C#.Net 中，CLR（Common Language Runtime）底层就封装了对托管的内存对象的管理，免得出现类似上面的错误提示
     - 上面说了有托管对象，那么对应的就有非托管对象不是 CLR 中管理的，这些对象需要手动释放
- GC.Collect 什么时候触发?
 CLR 底层会有检测当前托管堆内存可用大小是否小于下限，当到达下限，就会触发 GC.Collect，也就是我也检测的 GC
 而导致托管堆可用大小变小的无非就是我们调用了 GC.Alloc 来分配了托管堆内存，CLR 库中的类对象基本都是托管的，所以我们在各种 new、CreateActivor() 之类的接口来创建对象时，底层都会调用 GC.Alloc
- 为何要避免 GC（就上面说的GC.Collect）?
 因为 GC 需要底层需要处理的消耗比较大，具体可自行百度，这个与 GC 的检测机制有关，Unity 还可以在 PlayerSetting 中设置 GC 模式，是：Generation 还是 Increatement 方式
- 为何要控制 GC 频率?
 上面说了，GC 会导致 CPU 消耗大，如果托管对象多而小，导致的一些内存碎片过细，会更加消耗 CPU
 因此，我们尽可能的将托管对象缓存起来，反复使用，这样在运行过程中的 GC.Alloc 操作
 然后在切换场景时 GC 一下
- 如何实现 0 GC?
 如果你想这么做，就不要用 Unity，用 UE，就是用那些没有内置：内存分配统计、内存自动回收，运行库的语言，因为 .net 的底层很多都有 GC 问题，特别是字符串处理，因此很难做到 0 GC


下面时自行测试的内容，想要了解 .net、Unity 中其他 API 是否有 GC，可以留言告知我一下，我会测试后，更新到 blog

下面的测试，都使用 Unity 的 Profiler

如果你想快速入门，其实可以自己打开一下这个 Profiler Window 就打开知道怎么用了
如果还是看不动，就看官方教程也行（但是是英文的）：Fixing Performance Problems - 2019.3 https://learn.unity.com/tutorial/fixing-performance-problems-2019-3?uv=2019.3#

如果你英文看不懂，那就百度吧，要多少有多少

*/
public class CheckGC : MonoBehaviour
{
    public GameObject go;
    public Text txt1;
    public Text txt2;
    public Text txt3;
    public Text txt4;
    public Text txt5;
    private Vector3 txt3_src_pos;

    public Image img1;
    public Image img2;
    public Image img3;
    private Vector3 img3_src_pos;

    public RawImage raw_img1;
    public RawImage raw_img2;
    public RawImage raw_img3;
    public RawImage raw_img4;
    private Vector3 raw_img3_src_pos;
    private bool raw_img4_active = true;

    public MeshRenderer cube_renderer;
    public MeshRenderer sphere_renderer;
    public MeshRenderer capsule_renderer;
    private Vector3 capsule_renderer_src_pos;


    private void Start()
    {
        txt3_src_pos = txt3.transform.position;
        img3_src_pos = img3.transform.position;
        raw_img3_src_pos = raw_img3.transform.position;
        capsule_renderer_src_pos = capsule_renderer.transform.position;
    }

    private void Update()
    {
        Check_NewDotNetManagedObj();
        Check_GetComponentAndTryGetComponent();
        Check_GetComponentsInChildren();
        Check_ReturnRefOrValue();
        Check_ToString_Concat_Trim();
        Check_EnumToString();
        Check_StringEquals();
        Check_StringToArg();
        Check_NewString();
        Check_ToLowerString();
        Check_ReplaceString();
        Check_GameObject_get_name_or_get_tag();
        Check_GetTransform();
        Check_BoxingOrUnBoxing();
        Check_Enumerator();
        Check_EnumeratorGCSize();
        Check_Task_Delay_TimerMgr();
        Check_PassCallbackWhichHasGenericType();
        Check_List();
        Check_Using();
        Check_ReuseCoroutinue();
        Check_EnumGetValues();
        Check_Lambda();
        Check_LayerMaskGetMask();
        Check_ParamsToArg();
        Check_UGUI_TextToggleOrUpdate();
        Check_UGUI_ImageToggle();
        Check_UGUI_RawImageToggle();
        Check_MeshRenderToggle();
    }

    #region Check_NewDotNetManagedObj
    public class DotNetManagedObj
    {
        public int a1, a2, a3, a4, a5, a6;
    }
    private void Check_NewDotNetManagedObj()
    {
        Profiler.BeginSample("Check_NewDotNetManagedObj");
        new DotNetManagedObj();
        Profiler.EndSample();

        // Proflie 结果
        // 直接 new .net 的托管对象，都会有 GC.Alloc，当不断的 GC.Alloc，就会让 ManagedHeap.UnusedSize 越来越小，小到一定程度，就会触发 GC.Collect 回收垃圾
        // 而 GC.Collect 是很耗時的，所以尽力避免不必要的 GC.Alloc
    }
    #endregion

    #region Check_GetComponentAndTryGetComponent
    private void Check_GetComponentAndTryGetComponent()
    {
        Profiler.BeginSample("Check_GetComponentAndTryGetComponent");
        Profiler.BeginSample("1");
        {
            var checker = go.GetComponent<CheckGC>();
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        {
            go.TryGetComponent(out CheckGC checker);
        }
        Profiler.EndSample();
        Profiler.EndSample();

        // Profile 结果
        //  1 方式[没] GC
        //  2 方式[没] GC
        // 两种方式都没有，但是再以前有人发现 go.GetComponent 时，在 Editor 下才有 GC，真机上不会有
        //      具体可参考：https://zhuanlan.zhihu.com/p/26763624
        // 但现在我再 Editor 下测试也是没有的，有可能 Unity 做了优化，我的 Unity 版本是 2019.3.8f1
        // 但是 GetComponent 会消耗 CPU，因为原理上是 for 遍历 GameObject 下的所有 MonoBehaviour 组件
        // 建议尽可能将 GetComponent 的对象缓存起来，便于后续直接访问
    }
    #endregion

    #region Check_GetComponentsInChildren
    private List<MeshRenderer> mesh_render_list = new List<MeshRenderer>();
    public static class ListPoolT<T>
    {
        private static Stack<List<T>> pool = new Stack<List<T>>();
        public static List<T> FromPool()
        {
            return pool.Count > 0 ? pool.Pop() : new List<T>();
        }
        public static void ToPool(List<T> list)
        {
            list.Clear();
            pool.Push(list);
        }
    }
    private void Check_GetComponentsInChildren()
    {
        Profiler.BeginSample("Check_GetComponentsInChildren");

        Profiler.BeginSample("1");
        foreach (var item in go.GetComponentsInChildren<MeshRenderer>())
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        mesh_render_list.Clear();
        go.GetComponentsInChildren<MeshRenderer>(false, mesh_render_list);
        foreach (var item in mesh_render_list)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("3");
        var list = ListPoolT<MeshRenderer>.FromPool();
        go.GetComponentsInChildren<MeshRenderer>(false, list);
        foreach (var item in list)
        {

        }
        ListPoolT<MeshRenderer>.ToPool(list);
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        //  1 方式[有] GC
        //  2 方式[没] GC
        //  3 方式[没] GC（本质上和 2 方式一样，只不过，这里我将一些简单的封装方式给大家参考）
    }
    #endregion

    #region Check_ReturnRefOrValue
    private TestingCls ReturnCls()
    {
        return new TestingCls();
    }
    private TestingStruct ReturnStruct()
    {
        return new TestingStruct();
    }
    private void Check_ReturnRefOrValue()
    {
        Profiler.BeginSample("Check_ReturnRefOrValue");


        Profiler.BeginSample("1");
        ReturnCls();
        Profiler.EndSample();


        Profiler.BeginSample("2");
        ReturnStruct();
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        //  1 方式返回的是引用类型对象，创建对象的内存是在托管堆的，所以有 GC
        //  2 方式返回的是值类型的对象，创建对象的内存是再线程执行栈中的数据，再函数声明时入栈，返回时出栈，所以没有托管退管理，也就没有 GC
    }
    #endregion

    #region Check_String
    private StringBuilder sb = new StringBuilder();
    private string testing_str = " abc ";
    private void Check_ToString_Concat_Trim()
    {
        Profiler.BeginSample("Check_ToString_Concat_Trim");

        Profiler.BeginSample("Check_ToString");

        {
            const int LOOP_MAX = 100;

            Profiler.BeginSample("1");
            {
                for (int i = 0; i < LOOP_MAX; i++)
                {
                    var str = i.ToString();
                    str = (i + 1).ToString();
                    str = (i + 2).ToString();
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("2");
            {
                for (int i = 0; i < LOOP_MAX; i++)
                {
                    var value = 999;
                    sb.Clear();
                    sb.Append(value);
                    sb.Append(value);
                    sb.Append(value);
                    var str = sb.ToString();
                }
            }
            Profiler.EndSample();

            // Profile 结果
            //  1 方式 GC LOOP_MAX * 3 次
            //  2 方式 GC LOOP_MAX * 3 + LOOP_MAX 次
            //      多少次 Append 不是字符串的，GC就会调用多少次；
            //      另外，ToString() 也一样，因此，如果需要混合很多原本比是字符串的数据，GC会很多，但是字符串的话，
            // 在单帧的 StringBuilder.Append(NotAStringData.ToString()) 操作比较多的话，建议使用 + 号来拼接，减少 Append 完后：最后一次的 ToString()
        }

        Profiler.EndSample();

        Profiler.BeginSample("Check_ConcatenateString");

        {
            const int LOOP_MAX = 100;
            {
                Profiler.BeginSample("1");
                {
                    var str = "";
                    for (int i = 0; i < LOOP_MAX; i++)
                    {
                        str += "8";
                        str += "8";
                        str += "8";
                    }
                }
                Profiler.EndSample();
            }

            {
                Profiler.BeginSample("2");
                {
                    sb.Clear();
                    for (int i = 0; i < LOOP_MAX; i++)
                    {
                        sb.Append("8");
                        sb.Append("8");
                        sb.Append("8");
                    }
                    var str = sb.ToString();
                }
                Profiler.EndSample();
            }

            // Profile 结果
            //  1 方式 GC LOOP_MAX * 3次
            //  2 方式 GC 1 次
            //      多少次 Append 不是字符串的，GC就会调用多少次；
            //      但是如果 Append 的本身就是字符串内容的话，就不需要 ToString()，内部会遍历将本身为 String 的内容逐个字符的 Copy 到 StringBuilder 中的 Buffer 里
            //      因为最后才调用 sb.ToString() ，因此 GC 只有 1 次
            // 在单帧的 StringBuilder.Append(IsAStringData) 操作比较多的话，建议使用 StringBuilder 来 Append，最后再 ToString() 即可
        }

        Profiler.EndSample();

        Profiler.BeginSample("Check_Trim");

        {
            var str = testing_str.Trim();

            // Profile 结果
            //  Trim 有 GC
        }

        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        //  1 方式 GC LOOP_MAX 次
        //  2 方式 GC 1 次
        // 在单帧的字符拼接操作比较多的，建议使用 StringBuilder 来处理
    }
    #endregion

    #region Cehck_EnumToString

    private enum eEnumToStr
    {
        One, Two, Three
    }

    private Dictionary<eEnumToStr, string> eEnumToStr_cach_dic;
    
    private void Check_EnumToString()
    {
        if (eEnumToStr_cach_dic == null)
        {
            eEnumToStr_cach_dic = new Dictionary<eEnumToStr, string>();
            eEnumToStr_cach_dic[eEnumToStr.One] = eEnumToStr.One.ToString();
            eEnumToStr_cach_dic[eEnumToStr.Two] = eEnumToStr.Two.ToString();
            eEnumToStr_cach_dic[eEnumToStr.Three] = eEnumToStr.Three.ToString();
        }

        Profiler.BeginSample("Check_EnumToString");

        Profiler.BeginSample("1");
        {
            var str = eEnumToStr.One.ToString();
            str = eEnumToStr.Two.ToString();
            str = eEnumToStr.Three.ToString();
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        {
            var str = eEnumToStr_cach_dic[eEnumToStr.One];
            str = eEnumToStr_cach_dic[eEnumToStr.Two];
            str = eEnumToStr_cach_dic[eEnumToStr.Three];
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // C#.net 中的 .ToString() 基本都有 GC，非常自己 override ToString() 写了个没有 GC 的处理
        // 所以我们尽可能的不用 ToString()
        // 必要的时候，我们可以使用缓存，像上面，我们可以使用 Dictionary<Type, String> 来缓存起来，而不用每次 ToString()
    }

    #endregion

    #region Check_StringEquals

    private void Check_StringEquals()
    {
        Profiler.BeginSample("Check_StringEquals");

        var str1 = "test1";
        var str2 = "test2";

        Profiler.BeginSample("1");
        if (str1 == str2)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        if (str1.Equals(str2))
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (gameObject.name == str1)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        if (gameObject.name.Equals(str1))
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("5");
        if (gameObject.tag == str1)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("6");
        if (gameObject.tag.Equals(str1))
        {

        }
        Profiler.EndSample();

        Profiler.EndSample();

        // 1, 2 方式没有 GC
        // 但是 unity 的 gameObject.name、tag 直接 getter 是有 GC.Alloc 的
        // 下面会有 Check_GameObject_get_name_or_get_tag 测试
        //  GameObject.name 有 GC，但是如果比较 name 字符串，没有替代的方法，所以我们只好缓存起来
        //  GameObject.tag 有 GC，但是如果比较 tag 字符串，可以使用 GameObject.CompareTag(string) 来处理，0 GC

    }

    #endregion

    #region Check_StringToArg

    private void _inner_Check_StringToArg(string str)
    {

    }
    private void Check_StringToArg()
    {
        Profiler.BeginSample("Check_StringToArg");

        Profiler.BeginSample("1");
        _inner_Check_StringToArg("test"); // 明文字符串是没有 GC的，这在编译之后再 ELF 或是 EXE 中的字符常量区的数据中
        Profiler.EndSample();

        Profiler.BeginSample("2");
        _inner_Check_StringToArg("test" + "1"); // 但是一旦有拼接，就会有 GC，这是这里有编译优化，所以没有 GC
        Profiler.EndSample();

        Profiler.BeginSample("3");
        var str1 = "test1";
        var str2 = "1";
        _inner_Check_StringToArg(str1 + str2); // 但是一旦有拼接，就会有 GC，把这些明文字符设置到字符变量，再拼接，这样躲过编译优化，就可以看到字符拼接的 GC
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 明文字符串是没有 GC的，这在编译之后再 ELF 或是 EXE 中的字符常量区的数据中
        // 但是一旦有拼接，就会有 GC
    }

    #endregion

    #region Check_NewString

    //public unsafe struct MyStrWrap
    //{
    //    public char* ptr;
    //    public int len;

    //    public static explicit operator MyStrWrap (string str)
    //    {
    //        fixed (char* temp_ptr = str)
    //        {
    //            var ret = new MyStrWrap()
    //            {
    //                ptr = temp_ptr,
    //                len = str.Length,
    //            };
    //            return ret;
    //        }
    //    }
    //}

    //private unsafe void WatchStrToCharPtr(ref string str)
    //{
    //    // jave.lin : c sharp 中的 char 是双字节的
    //    // 使用 byte 来遍历
    //    fixed (char* char_ptr = str)
    //    {
    //        byte* byte_ptr = (byte*)char_ptr;
    //        var count = str.Length * 2;
    //        for (int idx = 0; idx < count; idx += 2)
    //        {
    //            byte cb1 = *(byte_ptr + idx + 0);           // 单字节宽放入一个 byte 显示的值
    //            byte cb2 = *(byte_ptr + idx + 1);           // 单字节宽放入一个 byte 显示的值
    //            char cc1 = (char)cb1;                       // 单字节宽放入一个 WORD 显示的值
    //            char cc2 = (char)cb2;                       // 单字节宽放入一个 WORD 显示的值
    //            char wc = (char)(cb1 + (cb2 << 8));         // 双字节宽放入一直 WORD 显示的值

    //            ; // break point here, watching the vars
    //        }
    //    }
    //}

    //private unsafe void Mod(ref MyStrWrap from, ref MyStrWrap to)
    //{
    //    var min_len = Math.Min(from.len, to.len);
    //    for (int i = 0; i < min_len; i++)
    //    {
    //        *(to.ptr + i) = *(from.ptr + i);
    //    }
    //}

    private void Check_NewString()
    {
        Profiler.BeginSample("Check_NewString");

        var chars = new char[11];
        var ret = "";

        Profiler.BeginSample("1");
        {
            var str = new string(chars); // 使用 chars 只是当做是一个数据的备份
            ret = str;
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 会有 GC，因为 C#.net 底层调用了：String.FastAllocateString()，都会有 GC
        // 调用函数层级：String.ctor()/String.CreateString()/String.CtoCharArray()/String.FastAllocateString()/GC.Alloc
    }

    #endregion

    #region Check_ToLowerString

    private void Check_ToLowerString()
    {
        Profiler.BeginSample("Check_ToLowerString");
        var str = "AAbbCC";
        str = str.ToLower();
        Profiler.EndSample();

        // Profile 结果
        // 有 GC，因为 C#.net 底层调用了：String.FastAllocateString()，都会有 GC
        // 调用函数层级：String.ToLower()/TextInfo.ToLower()/TextInfo.ToLowerInternal()/String.FastAllocateString()/GC.Alloc
    }

    #endregion

    #region Check_ReplaceString

    private void Check_ReplaceString()
    {
        Profiler.BeginSample("Check_ReplaceString");
        var str = "AAbbCC";
        str = str.Replace("bb", "BB");
        Profiler.EndSample();

        // Profile 结果
        // 有 GC，因为 C#.net 底层调用了：String.FastAllocateString()，都会有 GC
        // 调用函数层级：String.Replace()/String.ReplaceInternal()/String.ReplaceUnchecked()/String.FastAllocateString()/GC.Alloc
    }

    #endregion

    #region Check_GameObject_get_name_or_get_tag

    private void Check_GameObject_get_name_or_get_tag()
    {
        Profiler.BeginSample("Check_GameObject_get_name_or_get_tag");

        var str1 = "Player";

        Profiler.BeginSample("1");
        if (gameObject.name == str1)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        if (gameObject.name.Equals(str1)) // 但是 Name 的没有类似 CompareTag 无 GC 的API，所以使用时需要注意
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (gameObject.tag == str1)
        {

        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        if (gameObject.CompareTag(str1)) // Unity 提供的无 GC API
        {

        }
        Profiler.EndSample();

        Profiler.EndSample();

        // 1,2,3,4 方式，即：GameObject::get_name，GameObject::get_tag 再底层都有 GC 问题
        // 如果要比较 tag 的话，可以使用 GameObject::CompareTag(string) 接口，但是 name 的没有对应的接口来比较
        // get_name, get_tag 都会 GC，所以我们在反复的获取他们的时候，最好缓存起来，便于后续的其他地方直接使用缓存的 name, tag
    }

    #endregion

    #region Check_GetTransform

    private Transform tras_cach;

    private void Check_GetTransform()
    {
        Profiler.BeginSample("Check_GetTransform");

        Profiler.BeginSample("1");
        var trans = gameObject.transform;
        Profiler.EndSample();

        Profiler.BeginSample("2");
        if (tras_cach == null)
        {
            tras_cach = gameObject.transform;
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 2 方式都没有 GC
        // 但是在实际项目中通过 UnityEngine_GameObjectWrap.get_transform 调用中，GameObject.get_transform() 会有 40 B 的 GC.Alloc
        // 所以我们在 Lua 中调用 GameObject.transform 能缓存的都先缓存起来
    }

    #endregion

    #region Check_BoxingOrUnBoxing

    private object obj1 = new object();
    private TestingStruct testing_struct = new TestingStruct();
    private TestingCls test_clz = new TestingCls();
    public struct TestingStruct
    {

    }
    public class TestingCls
    {

    }

    private IEnumerator GetBoxingObjs()
    {
        yield return 1; // boxing
    }

    private IEnumerator GetNoBoxingOjbs()
    {
        yield return null; // no boxing
    }

    private void Check_BoxingOrUnBoxing()
    {
        Profiler.BeginSample("Check_BoxingOrUnBoxing");

        {
            Profiler.BeginSample("1");
            object obj = 1; // boxing 装箱操作，所以有 GC.Alloc
            Profiler.EndSample();
            Profiler.BeginSample("2");
            var v = (int)obj;
            Profiler.EndSample();
        }

        Profiler.BeginSample("3");
        {
            object obj = obj1; // 本身是引用（类似c++堆指针）
        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        {
            object obj = testing_struct; // 本身不是引用，而是值类型的结构体，所以需要装箱 Boxing，所以有 GC
        }
        Profiler.EndSample();

        Profiler.BeginSample("5");
        {
            object obj = test_clz;
        }
        Profiler.EndSample();

        Profiler.BeginSample("6");
        {
            object obj = null;
        }
        Profiler.EndSample();

        Profiler.BeginSample("7");
        {
            var enumerator = GetBoxingObjs(); // new 了一个状态机有 GC
            while(enumerator.MoveNext()) // 有 boxing 有 GC
            {
                object obj = enumerator.Current;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("8");
        {
            var enumerator = GetNoBoxingOjbs(); // new 了一个状态机有 GC
            while (enumerator.MoveNext()) // 无 boxing 无 GC
            {
                object obj = enumerator.Current;
            }
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Boxing 实质上就是构建了另一个指针数据将数据值类型数据包了一层类对象的封装，所以需要 new 这个类对象，所以会有额外的 GC.Alloc
        // 如果数据类型本身是引用类型，就不会有装箱处理
        // IEnumerator + yield 本质上就是 C# 编译器的语法糖，会生产一个状态机，对不同逻辑的 yield 设置到不一样的 swtich(state) 的 case 分支上执行对应的逻辑
        // 所以 7、8 的方式，在获取 IEnumerator 实例的瞬间就 new 了一个对象来构造状态信息，这回导致 GC 的
        // 但是 7 方式有 boxing 装箱操作，8 没有，所以 7 的 GC 比较大

        // 如何识别 Boxing ，也可以通过搜索反编译器或 IL 查看器（例如 ReSharper 中内置的 IL 查看器工具或 dotPeek 反编译器）的输出来定位装箱。IL 指令为“box”。
        // 参考：https://docs.unity3d.com/cn/current/Manual/BestPracticeUnderstandingPerformanceInUnity4-1.html
    }
    #endregion

    #region Check_Enumerator

    private IEnumerator<int> enumerator;
    private UnityCoroutineInst_HaveBoxOperates cor_inst_have_boxing;
    private UnityCoroutineInst_NoBoxingOperates cor_inst_no_boxing;
    private bool enumerator_first_run = true;

    private void Check_Enumerator()
    {
        Profiler.BeginSample("Check_Enumerator");

        Profiler.BeginSample("1");
        if (enumerator_first_run) enumerator = GetEnumerator();
        if (enumerator != null)
        {
            if (enumerator.MoveNext())
            {
                int v = enumerator.Current;
            }
            else
            {
                enumerator = null;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        if (enumerator_first_run) StartCoroutine(GetEnumerator());
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (enumerator_first_run) cor_inst_have_boxing = new UnityCoroutineInst_HaveBoxOperates();
        if (cor_inst_have_boxing != null)
        {
            if (cor_inst_have_boxing.MoveNext())
            {
                int v = enumerator.Current;
            }
            else
            {
                cor_inst_have_boxing = null;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        if (enumerator_first_run) cor_inst_no_boxing = new UnityCoroutineInst_NoBoxingOperates();
        if (cor_inst_no_boxing != null)
        {
            if (cor_inst_no_boxing.MoveNext())
            {
                int v = enumerator.Current;
            }
            else
            {
                cor_inst_no_boxing.Dispose();
                cor_inst_no_boxing = null;
            }
        }
        Profiler.EndSample();

        Profiler.EndSample();

        enumerator_first_run = false;

        // Profile 结果
        // IEnumerator + yield 本质上就是 C# 编译器的语法糖，会生产一个状态机，对不同逻辑的 yield 设置到不一样的 swtich(state) 的 case 分支上执行对应的逻辑
        // 所以在获取 IEnumerator 实例的瞬间就 new 了一个对象来构造状态信息，这回导致 GC 的
        // 所以控制好获取 IEnumerator 的次数，和帧频调用率
    }

    private IEnumerator<int> GetEnumerator() // 使用泛型的 T Current 不会有 Boxing 装箱的问题，就不会有 GC 问题
    {
        yield return 100;
        yield return 200;
        yield return 300;
    }

    // jave.lin : Unity 中的 Cortountine 本质上就是 IEnumerator + yield return 语法糖编译后的下列的状态机内存
    // 因此，没开一个协程都会有 GC.Alloc
    // 而且下面是 Boxing/UnBoxing（拆装箱） 操作，都会有 GC
    // 注意这个是实现：System.Collections.IEnumerator 接口的，因为是 object 类型的 current，所以会有 boxing operates
    public class UnityCoroutineInst_HaveBoxOperates : System.Collections.IEnumerator // 没有使用泛型的 T Current，返回的是值类型的话，就会有 Boxing，就会有 GC
    {
        public int state = 0;
        public object Current { get; private set; }

        public bool MoveNext()
        {
            ++state;
            switch (state)
            {
                case 1: Current = 100; return true;
                case 2: Current = 200; return true;
                case 3: Current = 300; return true;
                default: return false;
            }
        }

        public void Reset()
        {
            state = 0;
            Current = null;
        }
    }

    // 无 Boxing 操作
    // 注意这个是实现：System.Collections.Generic.IEnumerator<T> 接口的，因为是 T 类型的 current（泛型），所以会没有 boxing operates
    // 总结为：能用泛型 就不用 object
    public class UnityCoroutineInst_NoBoxingOperates : IEnumerator<int>
    {
        public int state = 0;
        private int _cur;
        public int Current { get; private set; }

        object IEnumerator.Current => _cur;

        public void Dispose()
        {
            // noops
        }

        public bool MoveNext()
        {
            ++state;
            switch (state)
            {
                case 1: Current = 100; return true;
                case 2: Current = 200; return true;
                case 3: Current = 300; return true;
                default: return false;
            }
        }

        public void Reset()
        {
            state = 0;
        }
    }

    #endregion

    #region Check_EnumeratorGCSize

    private IEnumerator GetEnumerator1()
    {
        yield return null; // 没有任何临时变量，那么 IEnumerator + yield 语法糖生成的成员变量就会越少，那么 GC.Alloc 就会小
    }

    private IEnumerator GetEnumerator2()
    {
        int a, b, c;
        yield return null; // 比 GetEnumerator1 生成的临时变量多，所以 GC.Alloc 就多
        a = 1;
        b = 1;
        c = 1;
    }

    private IEnumerator GetEnumerator3()
    {
        int a, b, c, d, e, f, g;
        yield return null; // 比 GetEnumerator2 生成的临时变量多，所以 GC.Alloc 就多
        a = 1;
        b = 1;
        c = 1;
        d = 1;
        e = 1;
        f = 1;
        g = 1;
    }

    private void Check_EnumeratorGCSize()
    {
        Profiler.BeginSample("Check_EnumeratorGCSize");

        Profiler.BeginSample("1");
        {
            var e = GetEnumerator1();
            while(e.MoveNext())
            {
                var obj = e.Current;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        {
            var e = GetEnumerator2();
            while (e.MoveNext())
            {
                var obj = e.Current;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("3");
        {
            var e = GetEnumerator3();
            while (e.MoveNext())
            {
                var obj = e.Current;
            }
        }
        Profiler.EndSample();


        Profiler.EndSample();

        // Profile 結果
        // 1, 2, 3 方式都会有 GC，这个在前面有说他的本质就是：IEnumerator + yield return 时的一样语法糖生成的状态机写法
        // 所以临时变量才能在不同帧之间保留数据状态
        // 所以，临时变量的数量越多，那意味着生成的状态机的对象的类变量成员的数量就越多，那么整个状态机类的 GC.Alloc 就越大
        // 这个用例是让大家更明白：IEnumerator + yield return 的作用，了解原理，才能使用起来得心应手
    }

    #endregion

    #region Check_Task_Delay
    private async void _inner_Check_Task_Delay()
    {
        int v = 0;
        await System.Threading.Tasks.Task.Delay(1); // 1 ms 后
        v = 1; // 1 ms 后，设置 v 为 1
    }

    public class Timer_Mgr_Same_Inst
    {
        public void SameMethod(int a)
        {

        }
    }

    private void TestDefaultParams1(int a = 1, int b = 2, int c = 3)
    {

    }

    private void TestDefaultParams2(Action callback = null)
    {

    }

    private void TestDefaultParams3<T>(Action<T> callback = null)
    {

    }

    private void TestDefaultParams4<T>(Action<T> callback = null, T arg = default(T))
    {

    }

    public int AddTimer<T>(
    Action<T> on_update, T on_update_arg = default(T),
    Action<T> on_complete = null, T on_complete_arg = default(T),
    float interval = 1.0f, int repeat = 1, bool with_time_scale = false)
    {
        return 0;
    }

    private void Testing<T>(T arg)
    {

    }

    public int AddTimer1<T>(
        Action<T> on_update, T on_update_arg = default(T))
    {
        return 0;
    }

    public class MyType
    {

    }

    private void _inner_Check_Timer_Mgr_Same_Inst_Method()
    {
        // 测试不同实例的方法，会不会同样地址
        Profiler.BeginSample("_inner_Check_Timer_Mgr_Same_Inst_Method 111");
        TimerMgr<int>.Inst.AddTimer(timer_mgr_same_inst1.SameMethod); // 两个函数都有 GC
        Profiler.EndSample();
        Profiler.BeginSample("_inner_Check_Timer_Mgr_Same_Inst_Method 222");
        TimerMgr<int>.Inst.AddTimer(timer_mgr_same_inst2.SameMethod); // 两个函数都有 GC
        Profiler.EndSample();

        // Profile 结果
        // 两个函数都有 GC
        // 具体查看：Check_PassCallbackWhichHasGenericType        
    }

    private int timer_id1 = -1;
    private void _inner_Check_Timer_Mgr()
    {
        int v = 0;
        if (timer_id1 == -1)
        {
            timer_id1 = TimerMgr<int>.Inst.AddTimer(tv =>
            {
                tv += 1;
                timer_id1 = -1;
            });
        }
    }

    public interface ITimerMgr
    {
        void Update(float detaTime);
    }

    public class TimerMonoBehaviour : MonoBehaviour
    {
        public ITimerMgr timer;

        private void Update()
        {
            if (timer != null)
            {
                timer.Update(Time.deltaTime);
            }
        }
    }

    private Timer_Mgr_Same_Inst timer_mgr_same_inst1 = new Timer_Mgr_Same_Inst();
    private Timer_Mgr_Same_Inst timer_mgr_same_inst2 = new Timer_Mgr_Same_Inst();

    private void Check_Task_Delay_TimerMgr()
    {
        Profiler.BeginSample("Check_Task_Delay");

        Profiler.BeginSample("1");
        _inner_Check_Task_Delay();
        Profiler.EndSample();

        Profiler.BeginSample("2");
        _inner_Check_Timer_Mgr();
        Profiler.EndSample();

        Profiler.BeginSample("3");
        _inner_Check_Timer_Mgr_Same_Inst_Method();
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // Task.Delay 内部有 GC，.net 底层代码中各种 new
        // 建议使用 Update 来及时执行后续指定的回调
        // Task.Delay 与 yield return 的 Unity 线程方式效果上差不多
        // 使用 Task.Delay 与 Cortoutine 最大优势是，可以写起来像闭包函数一样的效果，对 “临时变量” 状态值保留比较友好
        // 而使用 TimerMgr 自定义封装的管理器，可以做到 0 GC.Collect （只要不要太多 Timer），但是书写代码上就很不直观
        // 各有优劣，极度扣 GC.Collect 下，建议使用 TimerMgr 方式
    }
    #endregion

    #region Check_PassCallbackWhichHasGenericType

    private void Check_PassCallbackWhichHasGenericType()
    {
        Profiler.BeginSample("Check_PassCallbackWhichHasGenericType");

        Profiler.BeginSample("111");
        var aaa = default(int); // no gc
        Profiler.EndSample();

        Profiler.BeginSample("222");
        TestDefaultParams1(); // no gc
        Profiler.EndSample();

        Profiler.BeginSample("333");
        TestDefaultParams2(); // no gc
        Profiler.EndSample();

        Profiler.BeginSample("444");
        TestDefaultParams3<int>(); // no gc
        Profiler.EndSample();

        Profiler.BeginSample("555");
        TestDefaultParams4<int>(); // no gc
        Profiler.EndSample();

        Profiler.BeginSample("555.333");
        TestDefaultParams4<int>(Testing); // have gc
        Profiler.EndSample();

        Profiler.BeginSample("555.444");
        TestDefaultParams4<MyType>(Testing); // have gc
        Profiler.EndSample();

        Profiler.BeginSample("555.555");
        TestDefaultParams4<MyType>(inst => { }); // no gc，但是匿名函数，且非闭包就没有 GC
        Profiler.EndSample();

        Profiler.BeginSample("555.666");
        Action<MyType> act = (MyType inst) => { }; // no gc，将匿名函数存于一个变量，也没有 GC
        TestDefaultParams4<MyType>(act);
        Profiler.EndSample();

        Profiler.BeginSample("555.777");
        act = Testing; // have gc, 本质上和 555.444 是一样的，只不过尝试将一个预定义的方法指向一个临时的 act 方法变量，结果与 555.444 是一样有 GC 的
        TestDefaultParams4<MyType>(act);
        Profiler.EndSample();

        Profiler.BeginSample("555.888");
        var temp_var = 1; // 让下面行数变成闭包：在匿名函数使用临时变量即可
        Action<MyType> act1 = (MyType inst) => { temp_var++; }; // have gc，一旦函数变成闭包函数，就会有 GC，因此在频繁调用的地方尽量不使用闭包
        // 参考：unity 官方手册说明：https://docs.unity3d.com/cn/current/Manual/BestPracticeUnderstandingPerformanceInUnity4-1.html
        TestDefaultParams4<MyType>(act); // have gc
        Profiler.EndSample();

        Profiler.BeginSample("666");
        AddTimer<int>(timer_mgr_same_inst1.SameMethod); // have gc
        Profiler.EndSample();

        Profiler.BeginSample("777");
        AddTimer<int>(Testing); // have gc
        Profiler.EndSample();

        Profiler.BeginSample("888");
        AddTimer1<int>(Testing); // have gc
        Profiler.EndSample();


        Profiler.EndSample();

        // Profile 结果
        // 带有：<T> 泛型参数的 callback 作为参数，都会有 GC
        // 带有：<T> 泛型参数的匿名函数没有 GC
        // 闭包函数，都有 GC
        // （如果让一个匿名成为闭包，在匿名函数内容使用到不在闭包函数内的外部的临时变量即可，
        //   因为 C# 闭包原理是新建一个匿名类，将临时变量存于类成员中，
        //  这点与 IEnumerator + yield 的方式很类似，都是语法糖）

        // 在函数的方法参数传参时：

        /*
        以下说明参考：unity 官方手册说明：https://docs.unity3d.com/cn/current/Manual/BestPracticeUnderstandingPerformanceInUnity4-1.html

        IL2CPP 下的匿名方法
        目前，通过查看 IL2CPP 所生成的代码得知，对System.Function 类型变量的声明和赋值将会分配一个新对象。无论变量是显式的（在方法/类中声明）还是隐式的（声明为另一个方法的参数），都是如此。
        因此，使用 IL2CPP 脚本后端下的匿名方法必定会分配托管内存。在 Mono 脚本后端下则不是这种情况。
        此外，由于方法参数的声明方式不同，将导致IL2CPP 显示出托管内存分配量产生巨大差异。正如预期的那样，闭包的每次调用会消耗最多的内存。
        预定义的方法在 IL2CPP 脚本后端下作为参数传递时，其__分配的内存几乎与闭包一样多__，但这不是很直观。匿名方法在堆上生成最少量的临时垃圾（一个或多个数量级）。
        因此，如果打算在 IL2CPP 脚本后端上发布项目，有三个主要建议：
        - 最好选择不需要将方法作为参数传递的编码风格。
        - 当不可避免时，最好选择匿名方法而不是预定义方法。
        - 无论脚本后端为何，都要避免使用闭包。
         * */

    }

    #endregion

    #region Check_List

    private int[] arr4linq = { 3, 2, 9 };
    private List<int> _check_list = new List<int>();
    private void Check_List()
    {
        Profiler.BeginSample("Check_List");

        if (_check_list.Count == 0) _check_list.AddRange(arr4linq); // 内部：List Capacity 不足时会有 GC.Alloc，所以说，如果外部很多 List 需要临时使用的，都建议使用对象池，减少不必要的 GC.Alloc

        Profiler.BeginSample("1");
        var tolist = _check_list.ToArray(); // 内部 new T[Count]，有 GC
        Profiler.EndSample();

        Profiler.BeginSample("2");
        _check_list.Sort(); // 内部有 IComparer 实现对象的 new ，有 GC
        Profiler.EndSample();

        Profiler.BeginSample("3");
        _check_list.Reverse(); // 内部反转索引内容，无 GC
        Profiler.EndSample();

        Profiler.BeginSample("4");
        _check_list.GetRange(0, 1); // 内部 new List<T>，有 GC
        Profiler.EndSample();

        Profiler.BeginSample("5");
        _check_list.GetEnumerator(); // 内部 new Enumerator，但是 Enumerator 是内部的 struct 结构体，所以返回是存于执行栈帧的数据中，所以无 GC
        Profiler.EndSample();

        Profiler.BeginSample("6");
        _check_list.FindAll(v => v > 0); // 内部 new List<T>，有 GC
        Profiler.EndSample();

        Profiler.BeginSample("7");
        _check_list.Capacity = 10; // 当 capcity 不够指定大小时，内部 new T[]，然后 Array.Copy _items 到 new T[] 中，有 GC.Alloc
        Profiler.EndSample();

        Profiler.BeginSample("8");
        _check_list.ConvertAll<object>(v => v as object ); // 内部 new List<TOutput>，有 GC
        Profiler.EndSample();

        Profiler.BeginSample("9");
        _check_list.AsReadOnly(); // 内部 new ReadOnlyCollection<T>，有 GC
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 只测试了部分的 API，但是，其实 List（准确的说，.net 中的 API）大多都有 GC 问题，在排查 GC 问题，建议使用 ILSpy 或是 VS 自带的反编译来查看源码功能
        // 确定有 GC 后，建议使用缓存方式来避免重复，无意义的 GC.Alloc 而导致 GC.Collect
    }
    #endregion

    #region Check_Using
    public class TestingCanDispose : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this); // 不调用 ~XXX 析构
        }
    }
    private void Check_Using()
    {
        Profiler.BeginSample("CheckUsing");
        using (var obj = new TestingCanDispose()) // using 自动释放只不过时自动调用实现了：IDisposable 的接口，所以 GC 还时肯定有的
        {

        }
        Profiler.EndSample();
    }
    #endregion

    #region Check_ReuseCoroutinue

    private Coroutine _testingCor1;
    private IEnumerator _testingCor2;
    private UnityCoroutineInst_NoBoxingOperates _testingCor3;

    private IEnumerator TestingCor1()
    {
         yield return 0;
        //_testingCor1 = null;
    }

    private IEnumerator TestingCor2()
    {
        yield return 0;
        //_testingCor2.Reset(); // C# 语法糖生产的没有 Reset 实现，这里会报错
        //_testingCor2 = null;
    }


    private void Check_ReuseCoroutinue()
    {
        Profiler.BeginSample("Check_ReuseCoroutinue");

        Profiler.BeginSample("1");
        if (_testingCor1 == null)
        {
            _testingCor1 = StartCoroutine(TestingCor1());
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        if (_testingCor2 == null)
        {
            _testingCor2 = TestingCor2();
        }
        if (_testingCor2 != null)
        {
            if (_testingCor2.MoveNext())
            {
                int v = (int)_testingCor2.Current;
            }
            else
            {
                //_testingCor2.Reset(); // C# 语法糖生产的没有 Reset 实现，这里会报错
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (_testingCor3 == null)
        {
            _testingCor3 = new UnityCoroutineInst_NoBoxingOperates();
        }
        if (_testingCor3 != null)
        {
            if (_testingCor3.MoveNext())
            {
                int v = _testingCor3.Current;
            }
            else
            {
                _testingCor3.Reset(); // 我们自己实现的 Cortoutine 就可以随心所欲的 Reset，因为自己实现了接口，这样就不用重新 new 一个协程管理对象，也就没有 GC 了
            }
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        //  1、2 方式都因为 C# 语法糖内部实际 new 了一个类似 UnityCoroutineInst_NoBoxingOperates 的类来分状态处理，所以每次获取一个 Enumerator 时，都会有 GC
        //  3 方式虽然我们也实现了对应的 IEnumerator，但是我们自己可实现对 Reset 接口的处理，所以不用重新 new，因此只有第一次 new 有 GC
        // 因此，没事不要频繁的 StartCortoutine ，因为有 GC
        // 尽可能使用 Update 函数来处理
    }

    #endregion

    #region Check_EnumGetValues

    public enum eThreeType
    {
        One,
        Two,
        Three,
    }
    public enum eTenType
    {
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
    }
    private void Check_EnumGetValues()
    {
        Profiler.BeginSample("Check_EnumGetValues");

        Profiler.BeginSample("1");
        {
            var arr = Enum.GetValues(typeof(eThreeType));
        }
        Profiler.EndSample();

        Profiler.BeginSample("2");
        {
            var arr = Enum.GetValues(typeof(eTenType));
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        //  1 方式有 3 次 GC
        //  2 方式有 12 次 GC
        //  枚举的成员数量越多，GC越多次，因此，最好将 Enum.GetValues(typeof(T)) 的内容缓存到一个 static 对象中，这样就只会缓存一次，也只会在初始化类时GC
    }

    #endregion

    #region Check_Lambda

    public delegate void ModVal(ref int v);
    private void ModVal_Method(ref int v)
    {
        v += 1;
    }
    private void Check_Lambda()
    {
        Profiler.BeginSample("Check_Lambda");

        const int LOOP_MAX = 10000;

        {
            Profiler.BeginSample("1");
            ModVal act = (ref int a) => 
            {
                a += 1;
            };
            int v = 0;
            for (int i = 0; i < LOOP_MAX; i++)
            {
                act(ref v);
            }
            Profiler.EndSample();
        }

        {
            Profiler.BeginSample("2");
            int v = 0;
            for (int i = 0; i < LOOP_MAX; i++)
            {
                ModVal act = (ref int a) =>
                {
                    a += 1;
                };
                act(ref v);
            }
            Profiler.EndSample();
        }

        {
            Profiler.BeginSample("3");
            int v = 0;
            for (int i = 0; i < LOOP_MAX; i++)
            {
                ModVal_Method(ref v);
            }
            Profiler.EndSample();
        }

        {
            // 赋值一遍 1 的代码
            Profiler.BeginSample("4");
            ModVal act = (ref int a) =>
            {
                a += 1;
            };
            int v = 0;
            for (int i = 0; i < LOOP_MAX; i++)
            {
                act(ref v);
            }
            Profiler.EndSample();
        }

        Profiler.EndSample();

        // Profile 结果
        // Lambda 没有 GC 消耗
        //  1、2、4、3 方式，耗时最大到最低是：从左到右
        //  但是 1、4 都是一样的代码，为何耗时会不一样
    }

    #endregion

    #region Check_LayerMaskGetMask

    private void Check_LayerMaskGetMask()
    {
        Profiler.BeginSample("Check_LayerMaskGetMask");

        {
            Profiler.BeginSample("1");
            var layer = LayerMask.GetMask("Default"); // 有 GC，因为是 params 的数组变量，这是语法糖
            Profiler.EndSample();
        }

        {
            Profiler.BeginSample("2");
            var layer = LayerMask.NameToLayer("Default"); // 有 GC，因为是 params 的数组变量，这是语法糖
            Profiler.EndSample();
        }

        Profiler.EndSample();
    }

    #endregion

    #region Check_ParamsToArg

    private void Check_ParamsArg_NoParams(int a1, int a2, int a3)
    {

    }

    private void Check_ParamsArg_HaveParams(params int[] arg)
    {

    }

    private void Check_ParamsArg_SameAs(int[] arg)
    {

    }

    private void Check_ParamsToArg()
    {
        Profiler.BeginSample("Check_ParamsToArg");

        Profiler.BeginSample("1");
        Check_ParamsArg_HaveParams(1, 2, 3); // 1, 2 本质上相同
        Profiler.EndSample();

        Profiler.BeginSample("2");
        Check_ParamsArg_SameAs(new int[] { 1, 2, 3 }); // 1, 2 本质上相同，因为 C# 的数组 new 后是在，托管堆的，所以有 GC
        Profiler.EndSample();

        Profiler.BeginSample("3");
        Check_ParamsArg_NoParams(1, 2, 3); // 3 方式的参数分配都是在执行栈中的数据，自动分配与回收，因此没有 GC
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 2 方式时相同的，只不过 1 方式是 2 方式的语法糖的方式
        //  因为 C# 的数组 new 后是在，托管堆的，所以有 GC
        // 3 方式的参数分配都是在执行栈中的数据，自动分配与回收，因此没有 GC
        // 建议：
        // - 因此我们在调用频率高的地方，不要使用 params，而应该使用 明文声明的每个参数
        // - 如果参数很多，可以使用 struct 构造体
        // - 如果参数非常的多，可以使用 class 类传入，然后将这个类缓存起来
    }

    #endregion

    #region Check_UGUI_TextToggle

    private Vector3 invisible_pos = new Vector3(9999.0f, 0.0f, 0.0f);

    private string[] txt5_strs = null;
    private int txt5_idx = 0;
    const int TXT5_LOOP_MAX = 600;

    private void Check_UGUI_TextToggleOrUpdate()
    {
        if (txt5_strs == null)
        {
            txt5_strs = new string[TXT5_LOOP_MAX];
            for (int i = 0; i < TXT5_LOOP_MAX; i++)
            {
                txt5_strs[i] = $"Text{i}";
            }
        }

        Profiler.BeginSample("Check_UGUI_TextToggleOrUpdate");

        Profiler.BeginSample("1");
        txt1.gameObject.SetActive(!txt1.gameObject.activeSelf);
        Profiler.EndSample();

        Profiler.BeginSample("2");
        txt2.enabled = !txt2.enabled;
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (txt3.transform.position.Equals(invisible_pos))
        {
            txt3.transform.position = txt3_src_pos;
        }
        else
        {
            txt3.transform.position = invisible_pos;
        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        if (string.IsNullOrEmpty(txt4.text))
        {
            txt4.text = "Text4444";
        }
        else
        {
            txt4.text = "";
        }
        Profiler.EndSample();

        Profiler.BeginSample("5");
        ++txt5_idx;
        if (txt5_idx >= TXT5_LOOP_MAX)
        {
            txt5_idx = 0;
        }
        txt5.text = txt5_strs[txt5_idx];
        
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 2 方式都会有 GC，因为底层 Text.OnDisable->MaskableGraph.OnDisable->Component.GetComponent->Component.GetComponentFastPath 的 GC
        // 3 方式没有 GC，因为只是将Text3 从镜头范围内容移走，然后再移回来
        // 4, 5 都没有 GC，也就是说更变 Text.text 的内容，没有 GC，应该是底层的顶点缓存在前期分配的足够多，才会没有分配的
        // 但是，如果单单只是将 GameObject 回来，并让他看不见的话，不要使用 SetActive 方式，会有 GC
        // 建议还是使用移位置方式来处理，比较统一
    }

    #endregion

    #region Check_UGUI_ImageToggle

    private void Check_UGUI_ImageToggle()
    {
        Profiler.BeginSample("Check_UGUI_ImageToggle");

        Profiler.BeginSample("1");
        img1.gameObject.SetActive(!img1.gameObject.activeSelf);
        Profiler.EndSample();

        Profiler.BeginSample("2");
        img2.enabled = !img2.enabled;
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (img3.transform.position.Equals(invisible_pos))
        {
            img3.transform.position = img3_src_pos;
        }
        else
        {
            img3.transform.position = invisible_pos;
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 2 方式都会有 GC，因为底层 Image.OnDisable->MaskableGraph.OnDisable->Component.GetComponent->Component.GetComponentFastPath 的 GC
        // 3 方式没有 GC，因为只是将 Image3 从镜头范围内容移走，然后再移回来
        // 但是，如果单单只是将 GameObject 回来，并让他看不见的话，不要使用 SetActive 方式，会有 GC
        // 建议还是使用移位置方式来处理，比较统一
    }

    #endregion

    #region Check_UGUI_RawImageToggle

    private void Check_UGUI_RawImageToggle()
    {
        Profiler.BeginSample("Check_UGUI_RawImageToggle");

        Profiler.BeginSample("1");
        raw_img1.gameObject.SetActive(!raw_img1.gameObject.activeSelf);
        Profiler.EndSample();

        Profiler.BeginSample("2");
        raw_img2.enabled = !raw_img2.enabled;
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (raw_img3.transform.position.Equals(invisible_pos))
        {
            raw_img3.transform.position = raw_img3_src_pos;
        }
        else
        {
            raw_img3.transform.position = invisible_pos;
        }
        Profiler.EndSample();

        Profiler.BeginSample("4");
        raw_img4_active = !raw_img4_active;
        // 本质上和 3 的方式一样
        // 但是封装起来：ActiveUtil 让外部使用起来更方便
        // 而且内部可以调整统一的管理 deactive 的策略
        if (raw_img4_active)
        {
            ActiveUtil.Inst.Active(raw_img4.gameObject);
        }
        else
        {
            ActiveUtil.Inst.Deactive(raw_img4.gameObject);
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1, 2 方式都会有 GC，因为底层 RawImage.OnDisable->MaskableGraph.OnDisable->Component.GetComponent->Component.GetComponentFastPath 的 GC
        // 3, 4 方式没有 GC，因为只是将 RawImage3 从镜头范围内容移走，然后再移回来
        // 4 方式是封装好，便于外部统一使用的方式
        // 但是，如果单单只是将 GameObject 回来，并让他看不见的话，不要使用 SetActive 方式，会有 GC
        // 建议还是使用移位置方式来处理，比较统一
        // 也建议 UGUI 的内容都同意使用位移出镜头的方式来隐藏，需要显示时，再移动回来即可
    }

    #endregion

    #region Check_MeshRenderToggle

    private void Check_MeshRenderToggle()
    {
        Profiler.BeginSample("Check_MeshRenderToggle");

        Profiler.BeginSample("1");
        cube_renderer.gameObject.SetActive(!cube_renderer.gameObject.activeSelf);
        Profiler.EndSample();

        Profiler.BeginSample("2");
        sphere_renderer.enabled = !sphere_renderer.enabled;
        Profiler.EndSample();

        Profiler.BeginSample("3");
        if (capsule_renderer.transform.position.Equals(capsule_renderer_src_pos))
        {
            capsule_renderer.transform.position = invisible_pos;
        }
        else
        {
            capsule_renderer.transform.position = capsule_renderer_src_pos;
        }
        Profiler.EndSample();

        Profiler.EndSample();

        // Profile 结果
        // 1,2,3 都不会有 GC
        // 但是从效率上来说 : 1 < 2 < 3 ==> 1 最低，2：中等，3：最高
    }

    #endregion

}
