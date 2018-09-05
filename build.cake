///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var tmpDirectory = Argument("tmpdir", MakeAbsolute(Directory("./tmp/" + configuration)));
var nugetDirectory = Argument("nugetdir", tmpDirectory.FullPath + "/nuget" );

///////////////////////////////////////////////////////////////////////////////
// GLobal variables
///////////////////////////////////////////////////////////////////////////////
var asyncLibProj = "RStein.Async.csproj";
var actorsLib = "RStein.Async.Actors.csproj";
var currentPath = MakeAbsolute(Directory("."));
///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
    Information("Rstein.Async build started...");
    Information("Configuration {0}", configuration);
    Information("IsWindows: {0}", IsRunningOnWindows());
    Information("IsUnix: {0}", IsRunningOnUnix());
    Information("Build directory: {0}", tmpDirectory);
    Information("Nuget directory: {0}", nugetDirectory);
});

Teardown(ctx =>
{
    if (ctx.ThrownException != null)
    {
        Error("Build failed.");
        return;
    }

   // Executed AFTER the last task.
   Information("Build succeeded.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////


Task("Default")
.IsDependentOn("Build")
.Does(() => {
   
});

Task("Clean")
.Does(()=>
{
    CleanDirectory(tmpDirectory);
    CleanDirectories(currentPath + "/**/" + configuration);
});

Task("Build")
.IsDependentOn("Clean")
.IsDependentOn("BuildLibs")
.IsDependentOn("BuildCore")
.IsDependentOn("TestCore")
.IsDependentOn("BuildNet")
.IsDependentOn("TestNet")
.Does(()=>
{
    
});

Task("BuildLibs")
.Does(()=>
    {
        var prefix = "[lib]: ";
        var libsProjPath = GetFiles(currentPath + "/**/" + actorsLib).Single();

        var settings = new DotNetCoreBuildSettings
        {
          Configuration = configuration,
          DiagnosticOutput = true  
        };
        
        Information(prefix + $"Building {actorsLib}...");
        Information(prefix + $"Building {asyncLibProj}...");
        Information(prefix + $"Building project {libsProjPath}...");
        DotNetCoreBuild(libsProjPath.FullPath, settings);
    }
);
Task("BuildCore")
.Does(()=>
{
    var prefix = "[CORE]: ";
     var projects = GetFiles(currentPath + "/*Core/*.csproj");
     var settings = new DotNetCoreBuildSettings
     {
          Configuration = configuration,
          DiagnosticOutput = true  
     };

     foreach(var project in projects)
     {
         Information(prefix + $"Building {project}...");
         DotNetCoreBuild(project.FullPath, settings);
     }
});

Task("TestCore")
.Does(()=>
{
    var prefix = "[CORE_TEST]: ";
     var projects = GetFiles(currentPath + "/*Tests.Core/*.csproj");
     var settings = new DotNetCoreTestSettings()
     {
             NoBuild = true,
             DiagnosticOutput = true
     };

     foreach(var project in projects)
     {
         Information(prefix + $"Running tests {project}...");
        
         DotNetCoreTest(project.FullPath, settings);
     }
});

Task("BuildNet")
.Does(()=>
{
     var prefix = "[.NetF]: ";
     var projects = GetFiles(currentPath + "/**/*.csproj")
                    .Where(file => file.GetFilename().FullPath != actorsLib 
                            && file.GetFilename().FullPath != asyncLibProj
                            && !file.FullPath.Contains("Core"));

   
     
     foreach(var project in projects)
     {
         Information(prefix + $"Building {project}...");
         MSBuild(project.FullPath, configurator => configurator.Configuration = configuration);
     }
});

Task("TestNet")
.Does(()=>
{
    var prefix = "[Net_TEST]: ";
     var dlls = GetFiles(currentPath + $"/**/*Tests*/bin/{configuration}/*.dll")
                    .Where(file=>!file.FullPath.Contains("Core") 
                                && file.GetFilenameWithoutExtension().FullPath.StartsWith("RStein") 
                                && file.GetFilenameWithoutExtension().FullPath.EndsWith("Tests"));
                    
     
     foreach(var dll in dlls)
     {
         Information(prefix + $"Running tests {dll}...");
        
         MSTest(dll.FullPath);
     }

});

RunTarget(target);