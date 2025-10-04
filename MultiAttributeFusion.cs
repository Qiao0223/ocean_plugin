using System;
using System.Collections.Generic;

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
using Slb.Ocean.Petrel.UI.Controls; // 需要为 [Description] 注解引入

namespace ocean_plugin
{
    class MultiAttributeFusion : SeismicAttribute<MultiAttributeFusion.Arguments>, IDescriptionSource
    {
        #region Overrides from SeismicAttribute

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

        public override void CopyArgumentPackage(MultiAttributeFusion.Arguments fromArgumentPackage, MultiAttributeFusion.Arguments toArgumentPackage)
        {
            if (fromArgumentPackage != null && toArgumentPackage != null)
            {
                toArgumentPackage.CopyFrom(fromArgumentPackage);
            }
        }

        public override bool CompareArgumentPackage(MultiAttributeFusion.Arguments firstArgumentPackage, MultiAttributeFusion.Arguments secondArgumentPackage)
        {
            if (firstArgumentPackage != null && secondArgumentPackage != null)
            {
                return firstArgumentPackage.EqualsTo(secondArgumentPackage);
            }
            return false;
        }

        public override SeismicAttributeGenerator CreateAttributeGenerator(MultiAttributeFusion.Arguments argumentPackage, IGeneratorContext context)
        {
            return new MultiAttributeFusion.Generator(argumentPackage, context);
        }

        // ====================================================================================
        // 关键修改 1: 实现参数验证逻辑
        // ====================================================================================
        public override bool Validate(MultiAttributeFusion.Arguments args, IGeneratorContext context, out string errorMessage)
        {
            if (args.MinThreshold1 > args.MaxThreshold1) { errorMessage = "Input 1: Min Threshold cannot be greater than Max Threshold."; return false; }
            if (args.MinThreshold2 > args.MaxThreshold2) { errorMessage = "Input 2: Min Threshold cannot be greater than Max Threshold."; return false; }
            if (args.MinThreshold3 > args.MaxThreshold3) { errorMessage = "Input 3: Min Threshold cannot be greater than Max Threshold."; return false; }
            if (args.MinThreshold4 > args.MaxThreshold4) { errorMessage = "Input 4: Min Threshold cannot be greater than Max Threshold."; return false; }
            if (args.MinThreshold5 > args.MaxThreshold5) { errorMessage = "Input 5: Min Threshold cannot be greater than Max Threshold."; return false; }
            if (args.MinThreshold6 > args.MaxThreshold6) { errorMessage = "Input 6: Min Threshold cannot be greater than Max Threshold."; return false; }

            errorMessage = "N/A";
            return true;
        }

        public override SeismicAttributeInfo CreateSeismicAttributeInfo(MultiAttributeFusion.Arguments argumentPackage, IGeneratorContext context)
        {
            // 使用默认模板和自动范围，更适合融合结果
            IList<Slb.Ocean.Petrel.DomainObject.Template> templates = new List<Slb.Ocean.Petrel.DomainObject.Template> { PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicDefault };
            IList<Range1<float>> ranges = new List<Range1<float>>(); // 空列表表示自动计算范围
            return new SeismicAttributeInfo(templates, ranges, new Index3(1, 1, 1), BorderProcessingMethod.Repeat);
        }

        public override string CategoryName
        {
            get { return WellKnownAttributeCategory.Basic; }
        }

        public override int InputCount
        {
            get { return 6; }
        }

        public override int OutputCount
        {
            get { return 1; }
        }

        protected override IEnumerable<string> GetInputLabels(MultiAttributeFusion.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "RMS Amplitude";
            yield return "RMS Frequency";
            yield return "Envelope";
            yield return "Sweetness";
            yield return "Amplitude Gradient";
            yield return "Structural Tensor";
        }

        protected override IEnumerable<string> GetOutputLabels(MultiAttributeFusion.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Fusion Result";
        }

        #endregion

        #region Attribute Description related members
        public IDescription Description
        {
            get { return new AttributeDescription(); }
        }

        private class AttributeDescription : IDescription
        {
            public string Name
            {
                get { return "Thresholded Attribute Fusion"; }
            }
            public string Description
            {
                get { return "Conditionally blends multiple attributes using weights and thresholds. Output = Sum(w_i * (InRange(v_i) ? v_i : 0))"; }
            }
            public string ShortDescription
            {
                get { return "Weighted fusion with thresholds."; }
            }
        }
        #endregion

        // ====================================================================================
        // 关键修改 2: 填充 Arguments 类，为每个输入定义三个参数
        // ====================================================================================
        [Archivable(FromRelease = "2020.1")]
        public class Arguments : Slb.Ocean.Petrel.Workflow.DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            #region Parameters
            // --- Input 1 Parameters ---
            private double weight1 = 1.0;
            private double minThreshold1 = 0.0;
            private double maxThreshold1 = 1.0;

            [Archived(Name = "Weight1"), Description("Weight (Input 1)", "Weight for the first attribute.")]
            public double Weight1 { get { return weight1; } set { weight1 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold1"), Description("Min Threshold (Input 1)", "Minimum value threshold for the first attribute.")]
            public double MinThreshold1 { get { return minThreshold1; } set { minThreshold1 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold1"), Description("Max Threshold (Input 1)", "Maximum value threshold for the first attribute.")]
            public double MaxThreshold1 { get { return maxThreshold1; } set { maxThreshold1 = value; OnChanged(); } }

            // --- Input 2 Parameters ---
            private double weight2 = 0.0;
            private double minThreshold2 = 0.0;
            private double maxThreshold2 = 1.0;

            [Archived(Name = "Weight2"), Description("Weight (Input 2)", "Weight for the second attribute.")]
            public double Weight2 { get { return weight2; } set { weight2 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold2"), Description("Min Threshold (Input 2)", "Minimum value threshold for the second attribute.")]
            public double MinThreshold2 { get { return minThreshold2; } set { minThreshold2 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold2"), Description("Max Threshold (Input 2)", "Maximum value threshold for the second attribute.")]
            public double MaxThreshold2 { get { return maxThreshold2; } set { maxThreshold2 = value; OnChanged(); } }

            // --- Input 3 Parameters ---
            private double weight3 = 0.0;
            private double minThreshold3 = 0.0;
            private double maxThreshold3 = 1.0;

            [Archived(Name = "Weight3"), Description("Weight (Input 3)", "Weight for the third attribute.")]
            public double Weight3 { get { return weight3; } set { weight3 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold3"), Description("Min Threshold (Input 3)", "Minimum value threshold for the third attribute.")]
            public double MinThreshold3 { get { return minThreshold3; } set { minThreshold3 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold3"), Description("Max Threshold (Input 3)", "Maximum value threshold for the third attribute.")]
            public double MaxThreshold3 { get { return maxThreshold3; } set { maxThreshold3 = value; OnChanged(); } }

            // --- Input 4 Parameters ---
            private double weight4 = 0.0;
            private double minThreshold4 = 0.0;
            private double maxThreshold4 = 1.0;

            [Archived(Name = "Weight4"), Description("Weight (Input 4)", "Weight for the fourth attribute.")]
            public double Weight4 { get { return weight4; } set { weight4 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold4"), Description("Min Threshold (Input 4)", "Minimum value threshold for the fourth attribute.")]
            public double MinThreshold4 { get { return minThreshold4; } set { minThreshold4 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold4"), Description("Max Threshold (Input 4)", "Maximum value threshold for the fourth attribute.")]
            public double MaxThreshold4 { get { return maxThreshold4; } set { maxThreshold4 = value; OnChanged(); } }

            // --- Input 5 Parameters ---
            private double weight5 = 0.0;
            private double minThreshold5 = 0.0;
            private double maxThreshold5 = 1.0;

            [Archived(Name = "Weight5"), Description("Weight (Input 5)", "Weight for the fifth attribute.")]
            public double Weight5 { get { return weight5; } set { weight5 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold5"), Description("Min Threshold (Input 5)", "Minimum value threshold for the fifth attribute.")]
            public double MinThreshold5 { get { return minThreshold5; } set { minThreshold5 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold5"), Description("Max Threshold (Input 5)", "Maximum value threshold for the fifth attribute.")]
            public double MaxThreshold5 { get { return maxThreshold5; } set { maxThreshold5 = value; OnChanged(); } }

            // --- Input 6 Parameters ---
            private double weight6 = 0.0;
            private double minThreshold6 = 0.0;
            private double maxThreshold6 = 1.0;

            [Archived(Name = "Weight6"), Description("Weight (Input 6)", "Weight for the sixth attribute.")]
            public double Weight6 { get { return weight6; } set { weight6 = value; OnChanged(); } }
            [Archived(Name = "MinThreshold6"), Description("Min Threshold (Input 6)", "Minimum value threshold for the sixth attribute.")]
            public double MinThreshold6 { get { return minThreshold6; } set { minThreshold6 = value; OnChanged(); } }
            [Archived(Name = "MaxThreshold6"), Description("Max Threshold (Input 6)", "Maximum value threshold for the sixth attribute.")]
            public double MaxThreshold6 { get { return maxThreshold6; } set { maxThreshold6 = value; OnChanged(); } }
            #endregion

            #region Boilerplate Methods
            public Arguments() { }

            [Archived(Name = "Droid")] private Droid droid;
            public Droid Droid { get { return droid; } set { droid = value; } }

            // ====================================================================================
            // 关键修改 3: 实现参数的复制和比较方法
            // ====================================================================================
            public void CopyFrom(Arguments another)
            {
                if (another == null) return;
                this.Weight1 = another.Weight1; this.MinThreshold1 = another.MinThreshold1; this.MaxThreshold1 = another.MaxThreshold1;
                this.Weight2 = another.Weight2; this.MinThreshold2 = another.MinThreshold2; this.MaxThreshold2 = another.MaxThreshold2;
                this.Weight3 = another.Weight3; this.MinThreshold3 = another.MinThreshold3; this.MaxThreshold3 = another.MaxThreshold3;
                this.Weight4 = another.Weight4; this.MinThreshold4 = another.MinThreshold4; this.MaxThreshold4 = another.MaxThreshold4;
                this.Weight5 = another.Weight5; this.MinThreshold5 = another.MinThreshold5; this.MaxThreshold5 = another.MaxThreshold5;
                this.Weight6 = another.Weight6; this.MinThreshold6 = another.MinThreshold6; this.MaxThreshold6 = another.MaxThreshold6;
            }

            public bool EqualsTo(Arguments another)
            {
                if (another == null) return false;
                return this.Weight1.Equals(another.Weight1) && this.MinThreshold1.Equals(another.MinThreshold1) && this.MaxThreshold1.Equals(another.MaxThreshold1) &&
                       this.Weight2.Equals(another.Weight2) && this.MinThreshold2.Equals(another.MinThreshold2) && this.MaxThreshold2.Equals(another.MaxThreshold2) &&
                       this.Weight3.Equals(another.Weight3) && this.MinThreshold3.Equals(another.MinThreshold3) && this.MaxThreshold3.Equals(another.MaxThreshold3) &&
                       this.Weight4.Equals(another.Weight4) && this.MinThreshold4.Equals(another.MinThreshold4) && this.MaxThreshold4.Equals(another.MaxThreshold4) &&
                       this.Weight5.Equals(another.Weight5) && this.MinThreshold5.Equals(another.MinThreshold5) && this.MaxThreshold5.Equals(another.MaxThreshold5) &&
                       this.Weight6.Equals(another.Weight6) && this.MinThreshold6.Equals(another.MinThreshold6) && this.MaxThreshold6.Equals(another.MaxThreshold6);
            }

            public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
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
            public static string DataSourceId = @"fd0d0a73-9acc-4e0a-ba31-82d1426c340e";
            public override IDataSource GetDataSource()
            {
                return new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) });
            }
        }

        public class Generator : SeismicAttributeGenerator
        {
            private MultiAttributeFusion.Arguments arguments;
            private IGeneratorContext generatorContext;

            public Generator(MultiAttributeFusion.Arguments arguments, IGeneratorContext generatorContext)
            {
                this.arguments = arguments;
                this.generatorContext = generatorContext;
            }

            #region Overrides from SeismicAttributeGenerator
            public override void Initialize()
            {
                // 无需预计算，保持为空
            }

            // ====================================================================================
            // 关键修改 4: 实现融合算法的核心
            // ====================================================================================
            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                ISubCube outCube = output[0];

                // 提前将参数转换为float，避免在内层循环中反复转换
                float w1 = (float)arguments.Weight1, min1 = (float)arguments.MinThreshold1, max1 = (float)arguments.MaxThreshold1;
                float w2 = (float)arguments.Weight2, min2 = (float)arguments.MinThreshold2, max2 = (float)arguments.MaxThreshold2;
                float w3 = (float)arguments.Weight3, min3 = (float)arguments.MinThreshold3, max3 = (float)arguments.MaxThreshold3;
                float w4 = (float)arguments.Weight4, min4 = (float)arguments.MinThreshold4, max4 = (float)arguments.MaxThreshold4;
                float w5 = (float)arguments.Weight5, min5 = (float)arguments.MinThreshold5, max5 = (float)arguments.MaxThreshold5;
                float w6 = (float)arguments.Weight6, min6 = (float)arguments.MinThreshold6, max6 = (float)arguments.MaxThreshold6;

                Index3 min = outCube.MinIJK;
                Index3 max = outCube.MaxIJK;

                for (int k = min.K; k <= max.K; k++)
                {
                    for (int j = min.J; j <= max.J; j++)
                    {
                        for (int i = min.I; i <= max.I; i++)
                        {
                            Index3 idx = new Index3(i, j, k);
                            float finalValue = 0.0f;
                            float currentVal;

                            // 处理每个输入
                            // 如果输入槽为空，Petrel传入的input[i]会是null
                            // 我们需要检查null，避免程序崩溃

                            if (input[0] != null)
                            {
                                currentVal = input[0][idx];
                                if (currentVal >= min1 && currentVal <= max1)
                                    finalValue += currentVal * w1;
                            }
                            if (input[1] != null)
                            {
                                currentVal = input[1][idx];
                                if (currentVal >= min2 && currentVal <= max2)
                                    finalValue += currentVal * w2;
                            }
                            if (input[2] != null)
                            {
                                currentVal = input[2][idx];
                                if (currentVal >= min3 && currentVal <= max3)
                                    finalValue += currentVal * w3;
                            }
                            if (input[3] != null)
                            {
                                currentVal = input[3][idx];
                                if (currentVal >= min4 && currentVal <= max4)
                                    finalValue += currentVal * w4;
                            }
                            if (input[4] != null)
                            {
                                currentVal = input[4][idx];
                                if (currentVal >= min5 && currentVal <= max5)
                                    finalValue += currentVal * w5;
                            }
                            if (input[5] != null)
                            {
                                currentVal = input[5][idx];
                                if (currentVal >= min6 && currentVal <= max6)
                                    finalValue += currentVal * w6;
                            }

                            outCube[idx] = finalValue;
                        }
                    }
                }
            }
            #endregion
        }
    }
}