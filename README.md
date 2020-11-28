# check_gc
为了测试 Unity and C#.net GC

目前是为了学习，和在实际项目中做优化时需要实践测试的工程

## 有啥用
check_gc 工程中列出了 Unity 中一部分带有 GC 操作的脚本演示

如果你也正好想了解一部分 Unity 中带有 GC 操作的 API，这或许对你有些帮助

也有部分是直接给出优化方式，或是建议方式的代码

## 怎么用
unity version 使用的：2019.3.8f1

打开后，再打开：Profiler 窗口

再运行，再 Hierarchy 下查看 GC 那一栏信息

即可查看到对应测试的 API 使用是否有 GC