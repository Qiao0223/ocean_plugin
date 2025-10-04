using System;
using System.Collections.Generic;

// 这些是Petrel Ocean API的核心引用
using Slb.Ocean.Core;
using Slb.Ocean.Basics;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.Data;
using Slb.Ocean.Petrel.Data.Persistence;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Petrel.Seismic;
using Slb.Ocean.Petrel.DomainObject.Seismic;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.UI.Controls; // 为了使用 [Description] 特性，需要这个引用

namespace ocean_plugin
{
    // 类名保持不变
    class AbsoluteClipNormalization : SeismicAttribute<AbsoluteClipNormalization.Arguments>, IDescriptionSource
    {
        #region Overrides from SeismicAttribute (这部分通常无需修改)

        protected override Arguments CreateArgumentPackageCore(IDataSourceManager manager)
        {
            Arguments argPack = new Arguments();
            StructuredArchiveDataSource dataSource = manager.GetSource(ArgumentPackageDataSourceFactory.DataSourceId) as StructuredArchiveDataSource;
            if (dataSource != null)
            {
                argPack.Droid = dataSource.GenerateDroid();
                dataSource.AddItem(argPack.Droid, argPack);
            }
            return argPack;
        }

        public override void CopyArgumentPackage(AbsoluteClipNormalization.Arguments fromArgumentPackage, AbsoluteClipNormalization.Arguments toArgumentPackage)
        {
            if (fromArgumentPackage != null && toArgumentPackage != null)
            {
                toArgumentPackage.CopyFrom(fromArgumentPackage);
            }
        }

        public override bool CompareArgumentPackage(AbsoluteClipNormalization.Arguments firstArgumentPackage, AbsoluteClipNormalization.Arguments secondArgumentPackage)
        {
            if (firstArgumentPackage != null && secondArgumentPackage != null)
            {
                return firstArgumentPackage.EqualsTo(secondArgumentPackage);
            }
            return false;
        }

        public override SeismicAttributeGenerator CreateAttributeGenerator(AbsoluteClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            return new AbsoluteClipNormalization.Generator(argumentPackage, context);
        }

        #endregion

        // ====================================================================================
        // 修改点 1: 实现参数验证逻辑
        // ====================================================================================
        public override bool Validate(AbsoluteClipNormalization.Arguments argumentPackage, IGeneratorContext context, out string errorMessage)
        {
            // 验证规则1: 阈值必须为非负数
            if (argumentPackage.LowerThreshold < 0 || argumentPackage.UpperThreshold < 0)
            {
                errorMessage = "Lower and Upper thresholds must be non-negative.";
                return false; // 验证失败
            }
            // 验证规则2: 下限必须小于上限
            if (argumentPackage.LowerThreshold >= argumentPackage.UpperThreshold)
            {
                errorMessage = "Lower Threshold must be less than Upper Threshold.";
                return false; // 验证失败
            }

            errorMessage = "N/A";
            return true; // 所有检查通过，验证成功
        }

        // ====================================================================================
        // 修改点 2: 配置输出属性的元数据
        // ====================================================================================
        public override SeismicAttributeInfo CreateSeismicAttributeInfo(AbsoluteClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            // 为输出属性指定一个合适的颜色模板。因为输出范围是[0,1]，方差模板效果不错。
            IList<Slb.Ocean.Petrel.DomainObject.Template> templates = new List<Slb.Ocean.Petrel.DomainObject.Template>
            {
                PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicVariance
            };
            // 明确告诉Petrel输出的数据范围是 [0, 1]，这样颜色条会很匹配。
            IList<Range1<float>> ranges = new List<Range1<float>>
            {
                new Range1<float>(0f, 1f)
            };

            // 返回属性信息。Index3(1, 1, 1)表示这是一个逐点计算，不需要邻域数据。
            return new SeismicAttributeInfo(
                templates,
                ranges,
                new Index3(1, 1, 1),
                BorderProcessingMethod.Repeat);
        }

        #region Boilerplate (这部分通常无需修改)
        public override string CategoryName
        {
            get { return WellKnownAttributeCategory.Basic; }
        }
        public override int InputCount
        {
            get { return 1; }
        }
        public override int OutputCount
        {
            get { return 1; }
        }
        protected override IEnumerable<string> GetInputLabels(AbsoluteClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Input";
        }
        protected override IEnumerable<string> GetOutputLabels(AbsoluteClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Output";
        }
        #endregion

        #region Attribute Description related members (属性描述)

        public IDescription Description
        {
            get { return new AttributeDescription(); }
        }

        // ====================================================================================
        // 修改点 3: 更新属性的名称和描述
        // ====================================================================================
        private class AttributeDescription : IDescription
        {
            public string Name
            {
                // 提供一个清晰、易于理解的名称
                get { return "Absolute Value Band-Clip Normalization"; }
            }
            public string Description
            {
                // 详细描述该属性的功能
                get { return "Clips the absolute value of data between a lower and upper threshold, then normalizes the result to [0, 1]. Values below lower threshold are mapped to 0, values above upper threshold are mapped to 1."; }
            }
            public string ShortDescription
            {
                get { return "Band-clips absolute value and scales to 0-1."; }
            }
        }

        #endregion

        // ====================================================================================
        // 修改点 4: 定义用户可配置的参数
        // ====================================================================================
        [Archivable(FromRelease = "2020.1")]
        public class Arguments : Slb.Ocean.Petrel.Workflow.DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            // 1. 定义私有字段来存储参数值
            private double lowerThreshold = 0.0;
            private double upperThreshold = 10000.0;

            // 2. 创建公共属性，并添加 [Archived] 和 [Description] 特性
            // [Archived] 用于项目保存和加载
            // [Description] 用于在Petrel界面上显示参数名称和提示信息
            [Archived(Name = "LowerThreshold")]
            [Description("Lower Threshold (Absolute)", "The lower absolute value boundary. Values below this will be mapped to 0.")]
            public double LowerThreshold
            {
                get { return lowerThreshold; }
                set { lowerThreshold = value; OnChanged(); } // 当值改变时，调用 OnChanged() 通知Petrel
            }

            [Archived(Name = "UpperThreshold")]
            [Description("Upper Threshold (Absolute)", "The upper absolute value boundary. Values above this will be mapped to 1.")]
            public double UpperThreshold
            {
                get { return upperThreshold; }
                set { upperThreshold = value; OnChanged(); }
            }

            // 构造函数
            public Arguments() { }

            // Droid是Petrel内部用于对象识别的机制
            [Archived(Name = "Droid")]
            private Droid droid;
            public Droid Droid
            {
                get { return droid; }
                set { droid = value; }
            }

            // 3. 实现参数的复制逻辑
            public void CopyFrom(Arguments another)
            {
                if (another != null)
                {
                    this.LowerThreshold = another.LowerThreshold;
                    this.UpperThreshold = another.UpperThreshold;
                }
            }

            // 4. 实现参数的比较逻辑
            public bool EqualsTo(Arguments another)
            {
                if (another == null) return false;

                return this.LowerThreshold.Equals(another.LowerThreshold) &&
                       this.UpperThreshold.Equals(another.UpperThreshold);
            }

            #region Boilerplate (这部分通常无需修改)
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected virtual void Dispose(bool disposing) { }
            public event EventHandler<ArgumentPackageChangedEventArgs> Changed;
            private void OnChanged()
            {
                if (Changed != null)
                    Changed(this, new ArgumentPackageChangedEventArgs());
            }
            #endregion
        }

        public class ArgumentPackageDataSourceFactory : DataSourceFactory
        {
            // 这个GUID是这个属性类的唯一标识符，由向导生成，保持不变
            public static string DataSourceId = @"5e77b41d-60cd-4cfb-b1d3-caeb329e1ee4";
            public override IDataSource GetDataSource()
            {
                return new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) });
            }
        }

        // ====================================================================================
        // 修改点 5: 实现核心计算逻辑
        // ====================================================================================
        public class Generator : SeismicAttributeGenerator
        {
            private AbsoluteClipNormalization.Arguments arguments;

            // 构造函数，Petrel框架会把用户设置好的参数传进来
            public Generator(AbsoluteClipNormalization.Arguments arguments, IGeneratorContext context)
            {
                this.arguments = arguments;
                // generatorContext 由基类管理，这里无需保存
            }

            #region Overrides from SeismicAttributeGenerator

            // 因为是逐点计算，不需要任何预处理，所以此方法为空
            public override void Initialize()
            {
            }

            // 这是真正执行计算的地方
            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                // 获取输入和输出数据块
                ISubCube inCube = input[0];
                ISubCube outCube = output[0];

                // 在循环外获取参数值并转换为float，可以提高效率
                float lower = (float)this.arguments.LowerThreshold;
                float upper = (float)this.arguments.UpperThreshold;
                float range = upper - lower;

                // 安全检查，防止除以零
                if (range <= 1e-9) return;

                // 获取数据块的索引范围
                Index3 min = outCube.MinIJK;
                Index3 max = outCube.MaxIJK;

                // 遍历数据块中的每一个点
                for (int k = min.K; k <= max.K; k++)
                    for (int j = min.J; j <= max.J; j++)
                        for (int i = min.I; i <= max.I; i++)
                        {
                            Index3 idx = new Index3(i, j, k);
                            float value = inCube[idx];

                            // 处理无效值 (Not a Number)
                            if (float.IsNaN(value))
                            {
                                outCube[idx] = float.NaN;
                                continue;
                            }

                            // ** 核心算法实现 **
                            float absValue = Math.Abs(value);
                            float normalizedValue;

                            if (absValue <= lower)
                            {
                                normalizedValue = 0.0f;
                            }
                            else if (absValue >= upper)
                            {
                                normalizedValue = 1.0f;
                            }
                            else
                            {
                                // 在 [lower, upper] 区间内，线性映射到 [0, 1]
                                normalizedValue = (absValue - lower) / range;
                            }

                            // 将计算结果写入输出数据块
                            outCube[idx] = normalizedValue;
                        }
            }

            #endregion
        }
    }
}