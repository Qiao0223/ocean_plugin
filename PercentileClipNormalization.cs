using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    class PercentileClipNormalization : SeismicAttribute<PercentileClipNormalization.Arguments>, IDescriptionSource
    {
        #region Boilerplate Code (无需修改)
        protected override Arguments CreateArgumentPackageCore(IDataSourceManager manager)
        {
            Arguments argPack = new Arguments();
            StructuredArchiveDataSource dataSource = manager.GetSource(ArgumentPackageDataSourceFactory.DataSourceId) as StructuredArchiveDataSource;
            if (dataSource != null)
            {
                argPack.Droid = dataSource.GenerateDroid(); dataSource.AddItem(argPack.Droid, argPack);
            }
            return argPack;
        }
        public override void CopyArgumentPackage(PercentileClipNormalization.Arguments fromArgumentPackage, PercentileClipNormalization.Arguments toArgumentPackage)
        {
            if (fromArgumentPackage != null && toArgumentPackage != null)
            {
                toArgumentPackage.CopyFrom(fromArgumentPackage);
            }
        }
        public override bool CompareArgumentPackage(PercentileClipNormalization.Arguments firstArgumentPackage, PercentileClipNormalization.Arguments secondArgumentPackage)
        {
            if (firstArgumentPackage != null && secondArgumentPackage != null)
            {
                return firstArgumentPackage.EqualsTo(secondArgumentPackage);
            }
            return false;
        }
        public override SeismicAttributeGenerator CreateAttributeGenerator(PercentileClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            return new PercentileClipNormalization.Generator(argumentPackage, context);
        }
        // *********************************************************************************
        // 关键修改 1: 更新 Validate 方法以检查新参数的逻辑有效性
        // *********************************************************************************
        public override bool Validate(PercentileClipNormalization.Arguments argumentPackage, IGeneratorContext context, out string errorMessage)
        {
            if (argumentPackage.LowerPercentile >= argumentPackage.UpperPercentile)
            {
                errorMessage = "Lower Percentile must be less than Upper Percentile.";
                return false;
            }
            errorMessage = "N/A";
            return true;
        }
        public override SeismicAttributeInfo CreateSeismicAttributeInfo(PercentileClipNormalization.Arguments argumentPackage, IGeneratorContext context)
        {
            IList<Template> templates = new List<Template>();
            IList<Range1<float>> ranges = new List<Range1<float>>();
            templates.Add(PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicDefault);
            ranges.Add(new Range1<float>(0f, 1f));
            return new SeismicAttributeInfo(templates, ranges, new Index3(1, 1, 1), BorderProcessingMethod.Repeat);
        }
        public override string CategoryName
        {
            get
            {
                return WellKnownAttributeCategory.Basic;
            }
        }
        public override int InputCount { get { return 1; } }
        public override int OutputCount { get { return 1; } }
        protected override IEnumerable<string> GetInputLabels(PercentileClipNormalization.Arguments argumentPackage, IGeneratorContext context) { yield return "Input"; }
        protected override IEnumerable<string> GetOutputLabels(PercentileClipNormalization.Arguments argumentPackage, IGeneratorContext context) { yield return "Output"; }
        #endregion

        #region Attribute Description & Arguments
        public IDescription Description { get { return new AttributeDescription(); } }
        private class AttributeDescription : IDescription
        {
            public string Name { get { return "Percentile Clip Normalization"; } }
            // 更新描述以反映其参数化特性
            public string Description { get { return "Clips data at user-defined percentiles to suppress outliers, then scales the result to a [0, 1] range to enhance contrast. Memory-efficient implementation."; } }
            public string ShortDescription { get { return "Clips at percentiles and scales to 0-1."; } }
        }

        [Archivable(FromRelease = "2020.1")]
        public class Arguments : Slb.Ocean.Petrel.Workflow.DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            // *********************************************************************************
            // 关键修改 2: 在 Arguments 类中定义用户可输入的参数
            // *********************************************************************************
            private double lowerPercentile = 1.0;
            private double upperPercentile = 99.0;

            [Archived(Name = "LowerPercentile")]
            [Description("Lower Percentile (%)", "The lower percentile boundary for clipping (e.g., 1). Values below this will be set to this boundary.")]
            public double LowerPercentile
            {
                get { return lowerPercentile; }
                set
                {
                    lowerPercentile = value;
                    OnChanged(); // 通知Petrel参数已改变
                }
            }

            [Archived(Name = "UpperPercentile")]
            [Description("Upper Percentile (%)", "The upper percentile boundary for clipping (e.g., 99). Values above this will be set to this boundary.")]
            public double UpperPercentile
            {
                get { return upperPercentile; }
                set
                {
                    upperPercentile = value;
                    OnChanged(); // 通知Petrel参数已改变
                }
            }

            public Arguments() { }
            [Archived(Name = "Droid")] private Droid droid;
            public Droid Droid { get { return droid; } set { droid = value; } }

            // 必须更新 CopyFrom 和 EqualsTo 以包含新参数
            public void CopyFrom(Arguments another)
            {
                if (another != null)
                {
                    this.LowerPercentile = another.LowerPercentile;
                    this.UpperPercentile = another.UpperPercentile;
                }
            }
            public bool EqualsTo(Arguments another)
            {
                return another != null &&
                       this.LowerPercentile.Equals(another.LowerPercentile) &&
                       this.UpperPercentile.Equals(another.UpperPercentile);
            }

            public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
            protected virtual void Dispose(bool disposing) { }

            // OnChanged 事件的实现
            public event EventHandler<ArgumentPackageChangedEventArgs> Changed;
            private void OnChanged()
            {
                if (Changed != null)
                {
                    Changed(this, new ArgumentPackageChangedEventArgs());
                }
            }
        }

        public class ArgumentPackageDataSourceFactory : DataSourceFactory
        {
            public static string DataSourceId = @"917e25ef-e3c6-4094-9a5b-3f613da4f9e4"; // Keep this ID stable
            public override IDataSource GetDataSource() { return new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) }); }
        }
        #endregion

        public class Generator : SeismicAttributeGenerator
        {
            // *********************************************************************************
            // 关键修改 3: 存储 Arguments 对象以便在 Initialize 中使用
            // *********************************************************************************
            private readonly Arguments arguments;
            private readonly IGeneratorContext generatorContext;
            private float clippingMin;
            private float clippingMax;
            private bool isInitialized = false;

            private const int NUM_HISTOGRAM_BINS = 10000;

            public Generator(PercentileClipNormalization.Arguments arguments, IGeneratorContext generatorContext)
            {
                this.arguments = arguments; // 保存传入的参数对象
                this.generatorContext = generatorContext;
            }

            public override void Initialize()
            {
                if (isInitialized) return;

                try
                {
                    PetrelLogger.Info("PercentileClipNormalization: Starting pre-computation using memory-efficient histogram method...");
                    Stopwatch sw = Stopwatch.StartNew();

                    SeismicEntity inputEntity = this.generatorContext.InputSeismicData[0];
                    if (!inputEntity.IsSeismicCube) { PetrelLogger.Error("PercentileClipNormalization: Input data is not a SeismicCube."); isInitialized = true; return; }
                    SeismicCube inputCube = inputEntity.SeismicCube;
                    if (inputCube == null) { PetrelLogger.Error("PercentileClipNormalization: The SeismicCube inside the SeismicEntity is null."); isInitialized = true; return; }

                    Index3 numSamples = inputCube.NumSamplesIJK;
                    float[] traceBuffer = new float[numSamples.K];

                    float globalMin = float.MaxValue;
                    float globalMax = float.MinValue;
                    long totalSampleCount = 0;

                    PetrelLogger.Info("Histogram Pass 1: Finding global min/max...");
                    for (int i = 0; i < numSamples.I; i++)
                    {
                        for (int j = 0; j < numSamples.J; j++)
                        {
                            inputCube.GetTraceData(i, j, traceBuffer);
                            for (int k = 0; k < numSamples.K; k++)
                            {
                                float value = traceBuffer[k];
                                if (!float.IsNaN(value))
                                {
                                    if (value < globalMin) globalMin = value;
                                    if (value > globalMax) globalMax = value;
                                    totalSampleCount++;
                                }
                            }
                        }
                    }

                    if (totalSampleCount == 0)
                    {
                        PetrelLogger.Info("PercentileClipNormalization WARNING: No valid data found.");
                        clippingMin = 0; clippingMax = 1; isInitialized = true; return;
                    }
                    PetrelLogger.Info($"Histogram Pass 1 Complete. Min={globalMin}, Max={globalMax}, Total Samples={totalSampleCount}");

                    float range = globalMax - globalMin;
                    if (range < 1e-9)
                    {
                        PetrelLogger.Info("PercentileClipNormalization WARNING: All data points are constant.");
                        clippingMin = globalMin; clippingMax = globalMax; isInitialized = true; return;
                    }

                    long[] histogramBins = new long[NUM_HISTOGRAM_BINS];
                    PetrelLogger.Info("Histogram Pass 2: Populating histogram...");
                    for (int i = 0; i < numSamples.I; i++)
                    {
                        for (int j = 0; j < numSamples.J; j++)
                        {
                            inputCube.GetTraceData(i, j, traceBuffer);
                            for (int k = 0; k < numSamples.K; k++)
                            {
                                float value = traceBuffer[k];
                                if (!float.IsNaN(value))
                                {
                                    int binIndex = (int)(((value - globalMin) / range) * (NUM_HISTOGRAM_BINS - 1));
                                    binIndex = Math.Max(0, Math.Min(NUM_HISTOGRAM_BINS - 1, binIndex));
                                    histogramBins[binIndex]++;
                                }
                            }
                        }
                    }
                    PetrelLogger.Info("Histogram Pass 2 Complete.");

                    // *********************************************************************************
                    // 关键修改 4: 使用参数替换硬编码的值
                    // *********************************************************************************
                    double lowerPercentile = this.arguments.LowerPercentile / 100.0;
                    double upperPercentile = this.arguments.UpperPercentile / 100.0;

                    this.clippingMin = GetValueFromHistogram(histogramBins, totalSampleCount, lowerPercentile, globalMin, range);
                    this.clippingMax = GetValueFromHistogram(histogramBins, totalSampleCount, upperPercentile, globalMin, range);

                    sw.Stop();
                    PetrelLogger.Info($"PercentileClipNormalization: Pre-computation finished in {sw.Elapsed.TotalSeconds:F2}s. Clipping Range: [{this.clippingMin}, {this.clippingMax}]");
                }
                catch (Exception ex)
                {
                    PetrelLogger.Error("PercentileClipNormalization: A critical error occurred during initialization.", ex);
                    clippingMin = 0;
                    clippingMax = 1;
                }
                finally
                {
                    isInitialized = true;
                }
            }

            private float GetValueFromHistogram(long[] bins, long totalCount, double percentile, float minVal, float range)
            {
                long targetCount = (long)(totalCount * percentile);
                long currentCount = 0;

                for (int i = 0; i < bins.Length; i++)
                {
                    currentCount += bins[i];
                    if (currentCount >= targetCount)
                    {
                        return minVal + ((float)i / (bins.Length - 1)) * range;
                    }
                }
                return minVal + range;
            }

            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                if (!isInitialized)
                {
                    Initialize();
                }

                ISubCube inCube = input[0];
                ISubCube outCube = output[0];

                float range = this.clippingMax - this.clippingMin;

                if (Math.Abs(range) < 1e-9)
                {
                    Index3 minFill = outCube.MinIJK;
                    Index3 maxFill = outCube.MaxIJK;
                    for (int k = minFill.K; k <= maxFill.K; k++)
                        for (int j = minFill.J; j <= maxFill.J; j++)
                            for (int i = minFill.I; i <= maxFill.I; i++)
                            {
                                outCube[new Index3(i, j, k)] = 0.0f;
                            }
                    return;
                }

                Index3 min = outCube.MinIJK;
                Index3 max = outCube.MaxIJK;

                for (int k = min.K; k <= max.K; k++)
                    for (int j = min.J; j <= max.J; j++)
                        for (int i = min.I; i <= max.I; i++)
                        {
                            Index3 idx = new Index3(i, j, k);
                            float value = inCube[idx];

                            if (float.IsNaN(value))
                            {
                                outCube[idx] = float.NaN;
                                continue;
                            }

                            float clippedValue = Math.Max(this.clippingMin, Math.Min(value, this.clippingMax));
                            float normalizedValue = (clippedValue - this.clippingMin) / range;
                            outCube[idx] = normalizedValue;
                        }
            }
        }
    }
}