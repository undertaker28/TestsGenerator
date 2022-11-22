using System.Threading.Tasks.Dataflow;

using TestsGeneratorLibrary;

namespace TestsGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            TestsGeneratorScript generator = new TestsGeneratorScript();
            ExecutionDataflowBlockOptions blockOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 3 };
            TransformBlock<string, string> getCode = new TransformBlock<string, string>
            (
                async filePath => await File.ReadAllTextAsync(filePath),
                blockOptions
            );

            TransformManyBlock<string, TestFile> createTests = new TransformManyBlock<string, TestFile>
            (
                async sourceCode => await Task.Run(() => generator.CreateTests(sourceCode).ToArray()),
                blockOptions
            );

            ActionBlock<TestFile> saveTests = new ActionBlock<TestFile>
            (
                async testsFile => await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + "\\Tests\\" + testsFile.Name, testsFile.Code),
                blockOptions
            );

            DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            getCode.LinkTo(createTests, linkOptions);
            createTests.LinkTo(saveTests, linkOptions);

            string[] filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Files\\");

            foreach (string filePath in filePaths)
            {
                if (filePath.EndsWith(".cs"))
                    getCode.Post(filePath);
            }
            getCode.Complete();
            saveTests.Completion.Wait();
        }
    }
}
