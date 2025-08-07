using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;

namespace ocean_plugin
{
    /// <summary>
    /// 控制插件模块生命周期的类，实现 IModule 接口
    /// </summary>
    public class ModuleAttribute : IModule, IDisposable
    {
        public ModuleAttribute()
        {
            // 构造函数，可用于初始化模块级别的数据（此处暂未实现）
        }

        #region IModule 接口成员

        /// <summary>
        /// 模块初始化阶段最早调用的方法，仅调用一次
        /// 用于注册数据源工厂类（参数包的序列化支持）
        /// </summary>
        public void Initialize()
        {
            // 注册参数包的数据源工厂，支持参数的保存和恢复
            PetrelSystem.AddDataSourceFactory(new ocean_plugin.StructureTensor.ArgumentPackageDataSourceFactory());
        }

        /// <summary>
        /// 模块初始化阶段的第二个调用方法，仅调用一次
        /// 注册非 UI 组件，如地震属性计算类
        /// </summary>
        public void Integrate()
        {
            // 确保地震属性服务已就绪，否则抛出异常
            if (Slb.Ocean.Petrel.Seismic.SeismicSystem.SeismicAttributeService == null)
                throw new LifecycleException("地震属性服务不可用。");

            // 注册自定义地震属性（例如结构张量特征值计算）
            Slb.Ocean.Petrel.Seismic.SeismicSystem.SeismicAttributeService.AddSeismicAttribute(new ocean_plugin.StructureTensor());
        }

        /// <summary>
        /// 模块初始化阶段的第三个调用方法，仅调用一次
        /// 可用于注册 UI 相关组件，如设置页面、右键菜单等（当前未实现）
        /// </summary>
        public void IntegratePresentation()
        {
            // 当前未注册任何 UI 相关组件
        }

        /// <summary>
        /// 模块卸载前调用（如关闭 Petrel 时），用于资源清理
        /// </summary>
        public void Disintegrate()
        {
            // 当前未进行资源清理操作
        }

        #endregion

        #region IDisposable 接口成员

        /// <summary>
        /// 显式释放资源（与 Disintegrate 类似）
        /// </summary>
        public void Dispose()
        {
            // 当前未释放任何资源
        }
        #endregion
    }
}
