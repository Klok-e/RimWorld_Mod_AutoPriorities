using System.Reflection;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;

namespace Tests.Helpers
{
    internal class AutoPopulatedNSubstitutePropertiesCustomization : ICustomization
    {
        #region ICustomization Members

        public void Customize(IFixture fixture)
        {
            fixture.ResidueCollectors.Add(new Postprocessor(
                new NSubstituteBuilder(new MethodInvoker(new NSubstituteMethodQuery())),
                new AutoPropertiesCommand(new PropertiesOnlySpecification())));
        }

        #endregion

        private class PropertiesOnlySpecification : IRequestSpecification
        {
            #region IRequestSpecification Members

            public bool IsSatisfiedBy(object request)
            {
                return request is PropertyInfo;
            }

            #endregion
        }
    }
}
