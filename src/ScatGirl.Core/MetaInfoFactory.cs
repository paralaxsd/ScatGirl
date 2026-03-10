namespace ScatGirl.Core;

public static class MetaInfoFactory
{
    public static MetaInfo Create(string nugetPackageName) =>
        new(nugetPackageName,
            ThisAssembly.AssemblyInformationalVersion,
            ThisAssembly.AssemblyConfiguration,
            ThisAssembly.GitCommitDate,
            ThisAssembly.IsPublicRelease,
            Environment.OSVersion.ToString(),
            Environment.Version.ToString());
}