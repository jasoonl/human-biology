using HumanBodyExplorer.Core;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class DiContainerTests
    {
        private interface IGreeter
        {
            string Greet();
        }

        private class Greeter : IGreeter
        {
            public string Greet() => "hello";
        }

        private class Consumer
        {
            [Inject] public IGreeter Greeter;
        }

        [Test]
        public void Inject_ResolvesBoundDependency()
        {
            var container = new DiContainer();
            container.Bind<IGreeter>().To<Greeter>();

            var consumer = new Consumer();
            container.Inject(consumer);

            Assert.IsNotNull(consumer.Greeter);
            Assert.AreEqual("hello", consumer.Greeter.Greet());
        }

        [Test]
        public void Resolve_UnboundType_Throws()
        {
            var container = new DiContainer();
            Assert.Throws<System.InvalidOperationException>(() => container.Resolve<IGreeter>());
        }
    }
}
