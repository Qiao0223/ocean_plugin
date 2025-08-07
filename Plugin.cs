using System;
using System.Collections.Generic;
using Slb.Ocean.Core;

namespace ocean_plugin
{
    /// <summary>
    /// 插件定义类，继承自 Ocean 插件基类 Plugin，用于注册模块和插件元信息
    /// </summary>
    public class Plugin : Slb.Ocean.Core.Plugin
    {
        /// <summary>
        /// 插件支持的 Petrel 版本号
        /// </summary>
        public override string AppVersion
        {
            get { return "2020.1"; }
        }

        /// <summary>
        /// 插件作者
        /// </summary>
        public override string Author
        {
            get { return "Qiao"; }
        }

        /// <summary>
        /// 插件联系邮箱
        /// </summary>
        public override string Contact
        {
            get { return "1968476645@qq.com"; }
        }

        /// <summary>
        /// 插件依赖项（没有依赖则返回 null）
        /// </summary>
        public override IEnumerable<PluginIdentifier> Dependencies
        {
            get { return null; }
        }

        /// <summary>
        /// 插件描述（可用于说明插件功能，目前为空）
        /// </summary>
        public override string Description
        {
            get { return ""; }
        }

        /// <summary>
        /// 插件图标资源名称（可用于在 Petrel UI 显示图标，这里没有指定）
        /// </summary>
        public override string ImageResourceName
        {
            get { return null; }
        }

        /// <summary>
        /// 插件官方网站或文档链接
        /// </summary>
        public override Uri PluginUri
        {
            get { return new Uri("http://www.pluginuri.info"); }
        }

        /// <summary>
        /// 插件中包含的所有模块（每个模块负责一个功能）
        /// </summary>
        public override IEnumerable<ModuleReference> Modules
        {
            get
            {
                // 注册 ModuleAttribute 这个模块，模块中包含生命周期和功能逻辑
                yield return new ModuleReference(typeof(ocean_plugin.ModuleAttribute));
            }
        }

        /// <summary>
        /// 插件名称（会显示在 Petrel 中）
        /// </summary>
        public override string Name
        {
            get { return "Plugin"; }
        }

        /// <summary>
        /// 插件唯一标识符（使用命名空间+版本生成）
        /// </summary>
        public override PluginIdentifier PluginId
        {
            get { return new PluginIdentifier(GetType().FullName, GetType().Assembly.GetName().Version); }
        }

        /// <summary>
        /// 插件的信任级别（默认为 Default）
        /// </summary>
        public override ModuleTrust Trust
        {
            get { return new ModuleTrust("Default"); }
        }
    }
}
