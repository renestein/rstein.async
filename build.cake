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
   // Executed AFTER the last task.
   Information("Finished running tasks.");
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
//.IsDependentOn("BuildNet")
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

RunTarget(target);