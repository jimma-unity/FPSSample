using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace EditorToolsTests
{
    [TestFixture]
    public class BuildToolsContentDirectoriesTests
    {
        const string TempOutputRoot = "Temp/BuildToolsContentDirectoriesTests";

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(TempOutputRoot))
                Directory.Delete(TempOutputRoot, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempOutputRoot))
                Directory.Delete(TempOutputRoot, true);
        }

        [Test]
        public void BuildContentDirectories_MethodSignature_IsExpected()
        {
            var method = typeof(BuildTools).GetMethod("BuildContentDirectories", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(method, "BuildTools.BuildContentDirectories was not found.");

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length, "Unexpected parameter count.");
            Assert.AreEqual(typeof(BuildTarget), parameters[0].ParameterType);
            Assert.AreEqual(typeof(string), parameters[1].ParameterType);
            Assert.AreEqual(typeof(string[]), parameters[2].ParameterType);
            Assert.IsTrue(parameters[2].GetCustomAttributes(typeof(ParamArrayAttribute), false).Any(), "roots parameter is expected to be params string[].");
        }

        [Test]
        [Explicit("Integration test: builds content and writes bundle artifacts. Run manually.")]
        public void BuildContentDirectories_WritesArtifactsTo_OutputPathAssetBundles()
        {
            var outputPath = Path.Combine(TempOutputRoot, "StandaloneWindows64Output");
            Directory.CreateDirectory(outputPath);

            BuildTools.BuildContentDirectories(BuildTarget.StandaloneWindows64, outputPath,
                "Assets/ContentRoots/ClientContentRoot.asset",
                "Assets/ContentRoots/ServerContentRoot.asset",
                "Assets/Resources/Content/SceneListRoot.asset");

            Assert.IsTrue(Directory.Exists(outputPath), "Expected content output path was not created.");
            var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
            Assert.IsTrue(files.Length > 0, "Expected content directory output to contain files.");
        }
    }
}
