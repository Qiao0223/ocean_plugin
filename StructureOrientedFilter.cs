using System;
using System.Collections.Generic;
using System.Linq; // 关键：为了使用 List.Average() 方法

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
    class StructureOrientedFilter : SeismicAttribute<StructureOrientedFilter.Arguments>, IDescriptionSource
    {
        #region Boilerplate (无需修改)
        protected override Arguments CreateArgumentPackageCore(IDataSourceManager manager)
        {
            var argPack = new Arguments();
            var dataSource = manager.GetSource(ArgumentPackageDataSourceFactory.DataSourceId) as StructuredArchiveDataSource;
            if (dataSource != null)
            {
                argPack.Droid = dataSource.GenerateDroid();
                dataSource.AddItem(argPack.Droid, argPack);
            }
            return argPack;
        }
        public override void CopyArgumentPackage(Arguments from, Arguments to)
        {
            if (from != null && to != null) to.CopyFrom(from);
        }
        public override bool CompareArgumentPackage(Arguments first, Arguments second)
        {
            if (first != null && second != null) return first.EqualsTo(second);
            return false;
        }
        public override SeismicAttributeGenerator CreateAttributeGenerator(Arguments args, IGeneratorContext ctx) => new Generator(args, ctx);
        #endregion

        public override bool Validate(StructureOrientedFilter.Arguments args, IGeneratorContext context, out string errorMessage)
        {
            // **核心修正**: 为新参数添加验证
            if (args.InlineFilterRadius < 0 || args.XlineFilterRadius < 0 || args.MaxVerticalSearchRadius < 0)
            {
                errorMessage = "Filter radius and search radius cannot be negative.";
                return false;
            }
            errorMessage = "N/A";
            return true;
        }

        public override SeismicAttributeInfo CreateSeismicAttributeInfo(StructureOrientedFilter.Arguments args, IGeneratorContext context)
        {
            int opSizeI = args.InlineFilterRadius * 2 + 1;
            int opSizeJ = args.XlineFilterRadius * 2 + 1;
            // **核心修正**: 请求一个在K方向足够厚的邻域，以容纳所有可能的垂向位移
            int opSizeK = args.MaxVerticalSearchRadius * 2 + 1;

            var operatorSize = new Index3(opSizeI, opSizeJ, opSizeK);

            var templates = new List<Template> {
                PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicDefault,
                PetrelProject.WellKnownTemplates.SeismicColorGroup.SeismicDefault
            };
            var ranges = new List<Range1<float>>();

            return new SeismicAttributeInfo(templates, ranges, operatorSize, BorderProcessingMethod.Repeat);
        }

        public override string CategoryName => WellKnownAttributeCategory.Structural;
        public override int InputCount => 3;
        public override int OutputCount => 2;

        protected override IEnumerable<string> GetInputLabels(StructureOrientedFilter.Arguments args, IGeneratorContext context)
        {
            yield return "Seismic Input";
            yield return "Inline Dip";
            yield return "Crossline Dip";
        }

        protected override IEnumerable<string> GetOutputLabels(StructureOrientedFilter.Arguments args, IGeneratorContext context)
        {
            yield return "Background";
            yield return "Residual";
        }

        #region Description and Arguments
        public IDescription Description => new AttributeDescription();
        private class AttributeDescription : IDescription
        {
            public string Name => "Structure Oriented Mean Filter";
            public string Description => "Applies a mean filter guided by local dip fields. Simultaneously outputs Background and Residual volumes.";
            public string ShortDescription => "Dip-guided mean filter.";
        }

        [Archivable(FromRelease = "2020.1")]
        public class Arguments : DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            private int inlineFilterRadius = 10;
            private int xlineFilterRadius = 10;
            // **核心修正**: 添加一个新的用户参数
            private int maxVerticalSearchRadius = 50;

            [Archived(Name = "InlineFilterRadius")]
            [Description("Inline Filter Radius", "The filter half-length in the inline direction (e.g., 25).")]
            public int InlineFilterRadius { get => inlineFilterRadius; set { inlineFilterRadius = value; OnChanged(); } }

            [Archived(Name = "XlineFilterRadius")]
            [Description("Xline Filter Radius", "The filter half-length in the crossline direction (e.g., 25).")]
            public int XlineFilterRadius { get => xlineFilterRadius; set { xlineFilterRadius = value; OnChanged(); } }

            [Archived(Name = "MaxVerticalSearchRadius")]
            [Description("Max Vertical Search Radius", "Maximum expected vertical shift in samples. Increase if dips are steep. This impacts memory usage.")]
            public int MaxVerticalSearchRadius { get => maxVerticalSearchRadius; set { maxVerticalSearchRadius = value; OnChanged(); } }

            public Arguments() { }
            [Archived(Name = "Droid")] private Droid droid;
            public Droid Droid { get => droid; set => droid = value; }

            public void CopyFrom(Arguments another)
            {
                if (another != null)
                {
                    this.InlineFilterRadius = another.InlineFilterRadius;
                    this.XlineFilterRadius = another.XlineFilterRadius;
                    // **核心修正**: 复制新参数
                    this.MaxVerticalSearchRadius = another.MaxVerticalSearchRadius;
                }
            }
            public bool EqualsTo(Arguments another)
            {
                if (another == null) return false;
                return this.InlineFilterRadius == another.InlineFilterRadius &&
                       this.XlineFilterRadius == another.XlineFilterRadius &&
                       // **核心修正**: 比较新参数
                       this.MaxVerticalSearchRadius == another.MaxVerticalSearchRadius;
            }

            public event EventHandler<ArgumentPackageChangedEventArgs> Changed;
            private void OnChanged() => Changed?.Invoke(this, new ArgumentPackageChangedEventArgs());
            public void Dispose() { }
        }

        public class ArgumentPackageDataSourceFactory : DataSourceFactory
        {
            public static string DataSourceId = @"a1281761-b0c7-45a4-88d3-4abdce20c0a7";
            public override IDataSource GetDataSource() => new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) });
        }
        #endregion

        public class Generator : SeismicAttributeGenerator
        {
            private StructureOrientedFilter.Arguments arguments;
            private IGeneratorContext context;
            private double shiftPerIlStepFactor, shiftPerXlStepFactor;

            public Generator(Arguments arguments, IGeneratorContext context)
            {
                this.arguments = arguments;
                this.context = context;
            }

            public override void Initialize()
            {
                var inputSeismic = this.context.InputSeismicData[0].SeismicCube;
                double dz = inputSeismic.SampleSpacingIJK.Z;
                double dy = inputSeismic.SampleSpacingIJK.X;
                double dx = inputSeismic.SampleSpacingIJK.Y;

                if (Math.Abs(dz) < 1e-9) throw new InvalidOperationException("Vertical sampling (dz) cannot be zero.");

                this.shiftPerIlStepFactor = dy / dz;
                this.shiftPerXlStepFactor = dx / dz;
            }

            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                var inSeismic = input[0];
                var inDipIl = input[1];
                var inDipXl = input[2];
                var outBackground = output[0];
                var outResidual = output[1];

                var neighborhoodValues = new List<float>();
                var inMin = inSeismic.MinIJK;
                var inMax = inSeismic.MaxIJK;
                var outMin = outBackground.MinIJK;
                var outMax = outBackground.MaxIJK;

                for (int k = outMin.K; k <= outMax.K; k++)
                {
                    for (int j = outMin.J; j <= outMax.J; j++)
                    {
                        for (int i = outMin.I; i <= outMax.I; i++)
                        {
                            var centerIdx = new Index3(i, j, k);
                            neighborhoodValues.Clear();

                            var tanDipIl = Math.Tan(inDipIl[centerIdx] * Math.PI / 180.0);
                            var tanDipXl = Math.Tan(inDipXl[centerIdx] * Math.PI / 180.0);
                            var shiftPerIlStep = tanDipIl * this.shiftPerIlStepFactor;
                            var shiftPerXlStep = tanDipXl * this.shiftPerXlStepFactor;

                            for (int offset_il = -arguments.InlineFilterRadius; offset_il <= arguments.InlineFilterRadius; offset_il++)
                            {
                                for (int offset_xl = -arguments.XlineFilterRadius; offset_xl <= arguments.XlineFilterRadius; offset_xl++)
                                {
                                    var totalVerticalShift = (offset_il * shiftPerIlStep) + (offset_xl * shiftPerXlStep);
                                    var neighbor_i = i + offset_il;
                                    var neighbor_j = j + offset_xl;
                                    var neighbor_k = (int)Math.Round(k + totalVerticalShift);

                                    // **核心修正**: 边界检查现在可以正常工作了，因为它检查的是Petrel提供给我们的、已经足够厚的`inSeismic`数据块
                                    if (neighbor_i >= inMin.I && neighbor_i <= inMax.I &&
                                        neighbor_j >= inMin.J && neighbor_j <= inMax.J &&
                                        neighbor_k >= inMin.K && neighbor_k <= inMax.K) // 检查K方向是否在提供的Halo内
                                    {
                                        neighborhoodValues.Add(inSeismic[new Index3(neighbor_i, neighbor_j, neighbor_k)]);
                                    }
                                }
                            }

                            float meanValue = 0;
                            if (neighborhoodValues.Any())
                            {
                                meanValue = neighborhoodValues.Average();
                            }

                            var originalValue = inSeismic[centerIdx];
                            outBackground[centerIdx] = meanValue;
                            outResidual[centerIdx] = originalValue - meanValue;
                        }
                    }
                }
            }
        }
    }
}