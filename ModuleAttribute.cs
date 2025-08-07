using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;

namespace ocean_plugin
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    public class ModuleAttribute : IModule
    {
        public ModuleAttribute()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region IModule Members

        /// <summary>
        /// This method runs once in the Module life; when it loaded into the petrel.
        /// This method called first.
        /// </summary>
        public void Initialize()
        {
            // Register ocean_plugin.StructureTensorEigenvalues
            PetrelSystem.AddDataSourceFactory(new ocean_plugin.StructureTensorEigenvalues.ArgumentPackageDataSourceFactory());
            // TODO:  Add ModuleAttribute.Initialize implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            // Register ocean_plugin.StructureTensorEigenvalues
            if (Slb.Ocean.Petrel.Seismic.SeismicSystem.SeismicAttributeService == null)
                throw new LifecycleException("Required AttributeService is not available.");
            Slb.Ocean.Petrel.Seismic.SeismicSystem.SeismicAttributeService.AddSeismicAttribute(new ocean_plugin.StructureTensorEigenvalues());

            // TODO:  Add ModuleAttribute.Integrate implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {

            // TODO:  Add ModuleAttribute.IntegratePresentation implementation
        }

        /// <summary>
        /// This method runs once in the Module life.
        /// right before the module is unloaded. 
        /// It usually happens when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            // TODO:  Add ModuleAttribute.Disintegrate implementation
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add ModuleAttribute.Dispose implementation
        }

        #endregion

    }


}