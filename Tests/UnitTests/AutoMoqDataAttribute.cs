using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace Chatter.UnitTests
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(() =>
            new Fixture { OmitAutoProperties = true }.Customize(new AutoMoqCustomization()))
        {
        }
    }
}
