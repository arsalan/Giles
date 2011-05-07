using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Giles.Core.Runners
{
    public interface ITestRunnerResolver
    {
        IEnumerable<IFrameworkRunner> Resolve(Assembly assembly);
    }

    public class TestRunnerResolver : ITestRunnerResolver
    {
        readonly Func<AssemblyName, bool> mSpecRunnerPredicate =
            assemblyName => assemblyName.Name == "Machine.Specifications";

        List<TestFrameworkRunner> runners;

        public TestRunnerResolver()
        {
            BuildRunnerList();
        }


        void BuildRunnerList()
        {
            runners = new List<TestFrameworkRunner>
                {new TestFrameworkRunner {CheckReference = mSpecRunnerPredicate, GetTheRunner = GetMSpecRunner}};
        }


        public IEnumerable<IFrameworkRunner> Resolve(Assembly assembly)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            var result =
                runners.Where(runner => referencedAssemblies.Count(runner.CheckReference) > 0).Select(
                    runner => runner.GetTheRunner.Invoke());

            return result;
        }


        static IFrameworkRunner GetMSpecRunner()
        {
            var assemblyLocation =
                Path.Combine(Path.GetDirectoryName(typeof (TestRunnerResolver).Assembly.Location),
                             "Giles.Runner.Machine.Specifications.dll");

            var runner = GetRunner(assemblyLocation);

            if (runner == null)
                return null;

            return Activator.CreateInstance(runner) as IFrameworkRunner;
        }


        static Type GetRunner(string assemblyLocation)
        {
            return Assembly.LoadFrom(assemblyLocation).GetTypes()
                .Where(x => typeof (IFrameworkRunner).IsAssignableFrom(x) && x.IsClass)
                .FirstOrDefault();
        }
    }

    internal class TestFrameworkRunner
    {
        internal Func<AssemblyName, bool> CheckReference { get; set; }
        internal Func<IFrameworkRunner> GetTheRunner { get; set; }
    }
}