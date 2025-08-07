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

namespace ocean_plugin
{
    class StructureTensorEigenvalues : SeismicAttribute<StructureTensorEigenvalues.Arguments>, IDescriptionSource
    {
            private string[] outputNames = {
                "lambda1",
                "lambda2",
                "lambda3"
                };


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


        public override void CopyArgumentPackage(StructureTensorEigenvalues.Arguments fromArgumentPackage, StructureTensorEigenvalues.Arguments toArgumentPackage)
        {
            if (fromArgumentPackage != null && toArgumentPackage != null)
            {
                toArgumentPackage.CopyFrom(fromArgumentPackage);
            }
        }

        public override bool CompareArgumentPackage(StructureTensorEigenvalues.Arguments firstArgumentPackage, StructureTensorEigenvalues.Arguments secondArgumentPackage)
        {
            if (firstArgumentPackage != null && secondArgumentPackage != null)
            {
                return firstArgumentPackage.EqualsTo(secondArgumentPackage);
            }

            return false;
        }

        public override SeismicAttributeGenerator CreateAttributeGenerator(StructureTensorEigenvalues.Arguments argumentPackage, IGeneratorContext context)
        {
            return new StructureTensorEigenvalues.Generator(argumentPackage, context);
        }

        public override bool Validate(StructureTensorEigenvalues.Arguments argumentPackage, IGeneratorContext context, out string errorMessage)
        {
            errorMessage = "N/A";

            // TODO: Please implement the validation logic for the argumentPackage.
            // return true, when the given argumentPackage is valid.
            // return false, and fill the errorMessage when the given argumentPackage is not valid.

            return true;
        }

        public override SeismicAttributeInfo CreateSeismicAttributeInfo(StructureTensorEigenvalues.Arguments argumentPackage, IGeneratorContext context)
        {

            IList<Slb.Ocean.Petrel.DomainObject.Template> templates = new List<Slb.Ocean.Petrel.DomainObject.Template>();
            IList<Range1<float>> ranges = new List<Range1<float>>();

            templates.Add(Slb.Ocean.Petrel.DomainObject.Template.NullObject);

            ranges.Add(new Range1<float>(float.NaN, float.NaN));

            return new SeismicAttributeInfo(
                templates,
                ranges,
                new Index3(5, 5, 5),
                BorderProcessingMethod.Repeat);

        }

        /// <summary>
        /// Gets the category of the attribute
        /// </summary>
        public override string CategoryName
        {
            get { return WellKnownAttributeCategory.Structural; }
        }

        /// <summary>
        /// Gets the number of the expected input cubes
        /// </summary>
        public override int InputCount
        {
            get { return 1; }
        }

        public override int OutputCount
        {
            get { return 3; }
        }

        protected override IEnumerable<string> GetInputLabels(StructureTensorEigenvalues.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "Input";
        }

        protected override IEnumerable<string> GetOutputLabels(StructureTensorEigenvalues.Arguments argumentPackage, IGeneratorContext context)
        {
            yield return "lambda1";
            yield return "lambda2";
            yield return "lambda3";
        }


        #endregion

        #region Attribute Description related members

        public IDescription Description
        {
            get { return new AttributeDescription(); }
        }

        private class AttributeDescription : IDescription
        {
            #region IDescription Members

            /// <summary>
            /// Gets the name of the attribute
            /// </summary>
            public string Name
            {
                get { return "StructureTensorEigenvalues"; }
            }

            /// <summary>
            /// Gets the description of the attribute
            /// </summary>
            public string Description
            {
                get { return "Three eigenvalues (λ1, λ2, λ3) of the structure tensor"; }
            }

            /// <summary>
            /// Gets the short description of the attribute
            /// Currently it is not in use.
            /// </summary>
            public string ShortDescription
            {
                get { return string.Empty; }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// This class contains the arguments of the attribute, if it has any.
        /// </summary>
        [Archivable(FromRelease = "2020.1")]
        public class Arguments : Slb.Ocean.Petrel.Workflow.DescribedArgumentsByReflection, IIdentifiable, IDisposable, Slb.Ocean.Petrel.Seismic.INotifyingOnChanged
        {
            public Arguments() { }

            [Archived(Name = "Droid")]
            private Droid droid;
            public Droid Droid
            {
                get { return droid; }
                set { droid = value; }
            }


            public void CopyFrom(Arguments another)
            {
                // TODO: implement the argument copying
                throw new NotImplementedException();
            }

            public bool EqualsTo(Arguments another)
            {
                // TODO: implement the argument comparing.
                // return true if the arguments are considered equal,
                // return false if they are considered not equal.

                throw new NotImplementedException();
            }
            #region IDisposable Members

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // TODO: free managed resources
                }

                // TODO: free unmanaged resources
            }

            #endregion

            #region INotifyingOnChanged Members

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
            public static string DataSourceId = @"0fe46afa-5dee-4f86-8a10-8bf8c924550f";
            public override IDataSource GetDataSource()
            {
                return new StructuredArchiveDataSource(DataSourceId, new[] { typeof(Arguments) });
            }
        }


        public class Generator : SeismicAttributeGenerator
        {
            /// <summary>
            /// Argument package
            /// </summary>
            private StructureTensorEigenvalues.Arguments arguments;
            /// <summary>
            /// Generator context for the attribute
            /// </summary>
            private IGeneratorContext generatorContext;

            /// <summary>
            /// Parameterized constructor to set argument package and generator context
            /// </summary>
            /// <param name="arguments">Argument package</param>
            /// <param name="context">Generator context</param>
            public Generator(StructureTensorEigenvalues.Arguments arguments, IGeneratorContext generatorContext)
            {
                this.arguments = arguments;
                this.generatorContext = generatorContext;
            }

            #region Overrides from SeismicAttributeGenerator

            public override void Initialize()
            {
                // TODO: add any initialization logic here
            }

            /// <summary>
            /// This method does the actual work of the attribute.
            /// </summary>
            /// <param name="input">array of the input subcubes</param>
            /// <param name="output">the result cube</param>
            public override void Calculate(ISubCube[] input, ISubCube[] output)
            {
                // 1) 获取子块和输出
                ISubCube inCube = input[0];
                ISubCube outLam1 = output[0];
                ISubCube outLam2 = output[1];
                ISubCube outLam3 = output[2];

                // 2) 拿窗口大小 (Index3 被填在 CreateSeismicAttributeInfo)
                Index3 window = this.Info.WindowSize;
                int wx = window.X / 2, wy = window.Y / 2, wz = window.Z / 2;

                // 3) 遍历子块范围
                Index3 min = inCube.MinIJK;
                Index3 max = inCube.MaxIJK;
                for (int z = min.Z; z <= max.Z; z++)
                    for (int y = min.Y; y <= max.Y; y++)
                        for (int x = min.X; x <= max.X; x++)
                        {
                            // 3.1) 中心差分梯度
                            float gx = (inCube[new Index3(x + 1, y, z)]
                                      - inCube[new Index3(x - 1, y, z)]) * 0.5f;
                            float gy = (inCube[new Index3(x, y + 1, z)]
                                      - inCube[new Index3(x, y - 1, z)]) * 0.5f;
                            float gz = (inCube[new Index3(x, y, z + 1)]
                                      - inCube[new Index3(x, y, z - 1)]) * 0.5f;

                            // 3.2) 局部窗口累加结构张量分量
                            double Txx = 0, Txy = 0, Txz = 0, Tyy = 0, Tyz = 0, Tzz = 0;
                            for (int dz = -wz; dz <= wz; dz++)
                                for (int dy = -wy; dy <= wy; dy++)
                                    for (int dx = -wx; dx <= wx; dx++)
                                    {
                                        // 在邻域内同样计算梯度
                                        float gxp = (inCube[new Index3(x + dx + 1, y + dy, z + dz)]
                                                   - inCube[new Index3(x + dx - 1, y + dy, z + dz)]) * 0.5f;
                                        float gyp = (inCube[new Index3(x + dx, y + dy + 1, z + dz)]
                                                   - inCube[new Index3(x + dx, y + dy - 1, z + dz)]) * 0.5f;
                                        float gzp = (inCube[new Index3(x + dx, y + dy, z + dz + 1)]
                                                   - inCube[new Index3(x + dx, y + dy, z + dz - 1)]) * 0.5f;

                                        Txx += gxp * gxp;
                                        Txy += gxp * gyp;
                                        Txz += gxp * gzp;
                                        Tyy += gyp * gyp;
                                        Tyz += gyp * gzp;
                                        Tzz += gzp * gzp;
                                    }

                            // 3.3) 特征值分解
                            ComputeEigenvaluesSymmetric3x3(
                                Txx, Txy, Txz,
                                      Tyy, Tyz,
                                            Tzz,
                                out double l1, out double l2, out double l3);

                            // 3.4) 写回
                            var idx = new Index3(x, y, z);
                            outLam1[idx] = (float)l1;
                            outLam2[idx] = (float)l2;
                            outLam3[idx] = (float)l3;
                        }
            }

            #endregion
        }
        /// <summary>
        /// Analytic eigen-decomposition for symmetric 3×3 matrix:
        /// [ a00 a01 a02 ]
        /// [ a01 a11 a12 ]
        /// [ a02 a12 a22 ]
        /// </summary>
        private static void ComputeEigenvaluesSymmetric3x3(
                double a00, double a01, double a02,
                double a11, double a12,
                double a22,
                out double w0, out double w1, out double w2)
        {
            // 平移到零均值
            double m = (a00 + a11 + a22) / 3.0;
            double b00 = a00 - m, b11 = a11 - m, b22 = a22 - m;
            double b01 = a01, b02 = a02, b12 = a12;

            double p = (b00 * b00 + b11 * b11 + b22 * b22
                      + 2 * (b01 * b01 + b02 * b02 + b12 * b12)) / 6.0;
            double detB = b00 * (b11 * b22 - b12 * b12)
                        - b01 * (b01 * b22 - b12 * b02)
                        + b02 * (b01 * b12 - b11 * b02);
            double q = detB / 2.0;

            // 计算角度
            double phi = Math.Acos(Math.Max(-1, Math.Min(1, q / Math.Sqrt(p * p * p)))) / 3.0;

            // 重构特征值
            w0 = m + 2.0 * Math.Sqrt(p) * Math.Cos(phi);
            w1 = m + 2.0 * Math.Sqrt(p) * Math.Cos(phi + 2.0 * Math.PI / 3.0);
            w2 = 3.0 * m - w0 - w1;

            // 降序排序
            if (w0 < w1) { var t = w0; w0 = w1; w1 = t; }
            if (w1 < w2) { var t = w1; w1 = w2; w2 = t; }
            if (w0 < w1) { var t = w0; w0 = w1; w1 = t; }
        }


    }
}
