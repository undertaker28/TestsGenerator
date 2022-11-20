namespace TestsGeneratorLibrary
{
    public class TestFile
    {
        public string Name { get; }
        public string Code { get; }
        public TestFile(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
