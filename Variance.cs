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
using Slb.Ocean.Petrel.UI.Controls;

namespace ocean_plugin
{
    class Variance : SeismicAttribute<Variance.Arguments>, IDescriptionSource
    {
        #region Overrides from SeismicAttribute (boilerplate)

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

        public override void CopyArgumentPackage(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            if (fromArgumentPackage != null && toArgumentPackage != null)
            {
                toArgumentPackage.CopyFrom(fromArgumentPackage);
            }
        }

        public override bool CompareArgumentPackage(Arguments firstArgumentPackage, Arguments secondArgumentPackage)
        {
            if (firstArgumentPackage != null && secondArgumentPackage != null)
            {
                return firstArgumentPackage.EqualsTo(secondArgumentPackage);
            }
            return false;
        }

        public override SeismicAttributeGenerator CreateAttributeGenerator(Arguments argumentPackage, IGeneratorContext context)
        {
            return new Generator(argumentPackage, context);
        }

        #endregion

        public override bool Validate(Arguments argumentPackage, IGeneratorContext context, out string errorMessage)
        {
            if (argumentPackage.WindowInline < 1 || argumentPackage.WindowXline < 1 || argumentPackage.WindowZ < 1)
            {
                errorMessage = "Window sizes must be >= 1.";
                return false;
            }
            if (argumentPackage.WindowInline % 2 == 0 || argumentPackage.WindowXline % 2 == 0 || argumentPackage.WindowZ % 2 == 0)
            {
                errorMessage = "Window sizes must be odd.";
                return false;
            }
            if (argumentPackage.ChunkInline < 1)
            {
                errorMessage = "Inline chunk size must be >= 1.";
                return false;
            }
            errorMessage = "N/A";
            return true;
        }

        public override SeismicAttributeInfo CreateSeismicAttributeInfo(Arguments argumentPackage, IGeneratorContext context)
        {
            IList<Template> templates = new List<Template>
            {
                PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicVariance
            };
            IList<Range1<float>> ranges = new List<Range1<float>>
            {
                new Range1<float>(0f, float.NaN)
            };

            return new SeismicAttributeInfo(
                templates,
                ranges,
                new Index3(argumentPackage.WindowInline, argumentPackage.WindowXline, argumentPackage.WindowZ),
                BorderProcessingMethod.Repeat);
        }

        public override string CategoryName => WellKnownAttributeCategory.Basic;
        public override int InputCount => 1;
        public override int OutputCount => 1;

        protected override IEnumerable<string> GetInputLabels(Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Input";
        }

        protected override IEnumerable<string> GetOutputLabels(Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Output";
        }

        #region Attribute Description related members

        public IDescription Description => new AttributeDescription();

        private class AttributeDescription : IDescription
        {
            public string Name => "Variance";
            public string Description => "Computes local variance on inline/xline windows and then averages the variance along Z.";
            public string ShortDescription => "Local variance with vertical averaging.";
        }

        #endregion

        [Archivable(FromRelease = "2020.1")]
        public class Arguments : DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            private int windowInline = 5;
            private int windowXline = 5;
            private int windowZ = 9;
            private int chunkInline = 20;
            private bool unbiasedVariance = false;

            [Archived(Name = "WindowInline")]
            [Description("Inline Window Size", "Odd window size for inline direction (e.g., 5).")]
            public int WindowInline { get => windowInline; set { windowInline = value; OnChanged(); } }

            [Archived(Name = "WindowXline")]
            [Description("Crossline Window Size", "Odd window size for crossline direction (e.g., 5).")]
            public int WindowXline { get => windowXline; set { windowXline = value; OnChanged(); } }

            [Archived(Name = "WindowZ")]
            [Description("Vertical Averaging Window", "Odd window size for vertical averaging (e.g., 9).")]
            public int WindowZ { get => windowZ; set { windowZ = value; OnChanged(); } }

            [Archived(Name = "ChunkInline")]
            [Description("Inline Chunk Size", "Inline chunk size for calculation. Larger values are faster but use more memory.")]
            public int ChunkInline { get => chunkInline; set { chunkInline = value; OnChanged(); } }

            [Archived(Name = "UnbiasedVariance")]
            [Description("Unbiased Variance", "Apply N/(N-1) scaling to estimate unbiased variance.")]
            public bool UnbiasedVariance { get => unbiasedVariance; set { unbiasedVariance = value; OnChanged(); } }

            public Arguments() { }

            [Archived(Name = "Droid")]
            private Droid droid;
            public Droid Droid { get => droid; set => droid = value; }

            public void CopyFrom(Arguments another)
            {
                if (another == null)
                {
                    return;
                }
                WindowInline = another.WindowInline;
                WindowXline = another.WindowXline;
                WindowZ = another.WindowZ;
                ChunkInline = another.ChunkInline;
                UnbiasedVariance = another.UnbiasedVariance;
            }

            public bool EqualsTo(Arguments another)
            {
                if (another == null) return false;
                return WindowInline == another.WindowInline &&
                       WindowXline == another.WindowXline &&
                       WindowZ == another.WindowZ &&
                       ChunkInline == another.ChunkInline &&
                       UnbiasedVariance == another.UnbiasedVariance;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing) { }

            public event EventHandler<ArgumentPackageChangedEventArgs> Changed;
            private void OnChanged()
            {
                Changed?.Invoke(this, new ArgumentPackageChangedEventArgs());
            }
        }

        public class ArgumentPackageDataSourceFactory : DataSourceFactory
        {
            public static string DataSourceId = @"b1f5b16f-40f3-4b8f-86d2-6f8c83b1d7db";
            public override IDataSource GetDataSource()
            {
                return new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) });
            }
        }

        public class Generator : SeismicAttributeGenerator
        {
            private readonly Arguments arguments;
            private readonly IGeneratorContext context;

            public Generator(Arguments arguments, IGeneratorContext context)
            {
                this.arguments = arguments;
                this.context = context;
            }

            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                var inSeismic = input[0];
                var outCube = output[0];

                var inMin = inSeismic.MinIJK;
                var inMax = inSeismic.MaxIJK;
                var outMin = outCube.MinIJK;
                var outMax = outCube.MaxIJK;

                int sizeI = outMax.I - outMin.I + 1;
                int sizeJ = outMax.J - outMin.J + 1;
                int sizeK = outMax.K - outMin.K + 1;

                int halfI = arguments.WindowInline / 2;
                int halfJ = arguments.WindowXline / 2;
                int halfK = arguments.WindowZ / 2;
                int chunkInline = Math.Min(arguments.ChunkInline, sizeI);

                for (int blockStartI = 0; blockStartI < sizeI; blockStartI += chunkInline)
                {
                    int blockSizeI = Math.Min(chunkInline, sizeI - blockStartI);
                    float[,,] varBlock = new float[blockSizeI, sizeJ, sizeK];

                    for (int k = 0; k < sizeK; k++)
                    {
                        int globalK = outMin.K + k;
                        for (int j = 0; j < sizeJ; j++)
                        {
                            int globalJ = outMin.J + j;
                            for (int bi = 0; bi < blockSizeI; bi++)
                            {
                                int globalI = outMin.I + blockStartI + bi;

                                double sum = 0.0;
                                double sum2 = 0.0;
                                int count = 0;

                                for (int wi = -halfI; wi <= halfI; wi++)
                                {
                                    int ii = globalI + wi;
                                    if (ii < inMin.I || ii > inMax.I) continue;
                                    for (int wj = -halfJ; wj <= halfJ; wj++)
                                    {
                                        int jj = globalJ + wj;
                                        if (jj < inMin.J || jj > inMax.J) continue;
                                        float val = inSeismic[new Index3(ii, jj, globalK)];
                                        sum += val;
                                        sum2 += val * val;
                                        count++;
                                    }
                                }

                                float variance = 0f;
                                if (count > 0)
                                {
                                    double mean = sum / count;
                                    double mean2 = sum2 / count;
                                    double var = mean2 - mean * mean;
                                    if (var < 0.0) var = 0.0;
                                    if (arguments.UnbiasedVariance && count > 1)
                                    {
                                        var *= (double)count / (count - 1);
                                    }
                                    variance = (float)var;
                                }

                                varBlock[bi, j, k] = variance;
                            }
                        }
                    }

                    for (int k = 0; k < sizeK; k++)
                    {
                        for (int j = 0; j < sizeJ; j++)
                        {
                            for (int bi = 0; bi < blockSizeI; bi++)
                            {
                                double sum = 0.0;
                                int count = 0;
                                for (int wk = -halfK; wk <= halfK; wk++)
                                {
                                    int kk = k + wk;
                                    if (kk < 0 || kk >= sizeK) continue;
                                    sum += varBlock[bi, j, kk];
                                    count++;
                                }
                                float averaged = count > 0 ? (float)(sum / count) : 0f;
                                int globalI = outMin.I + blockStartI + bi;
                                int globalJ = outMin.J + j;
                                int globalK = outMin.K + k;
                                outCube[new Index3(globalI, globalJ, globalK)] = averaged;
                            }
                        }
                    }
                }
            }
        }
    }
}
