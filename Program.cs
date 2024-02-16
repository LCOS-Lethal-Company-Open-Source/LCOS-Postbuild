using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;

// CONSTANTS //
const string LETHAL_LOCATION = @"C:\Program Files (x86)\Steam\steamapps\common\Lethal Company";
const string PLUGINS_LOCATION = LETHAL_LOCATION + @"\BepInEx\plugins";
const string THUNDERSTORE_FOLDER = "thunderstore";

// SANITY CHECKS //
if(!Directory.Exists(LETHAL_LOCATION)) {
    Console.Error.WriteLine(":: ERROR ::");
    Console.Error.WriteLine("Please download Lethal Company before attempting to mod it!");
    Environment.Exit(1);
}

if(!Directory.Exists(PLUGINS_LOCATION)) {
    Console.Error.WriteLine(":: ERROR ::");
    Console.Error.WriteLine("Please install BepInEx before building your mods!");
    Environment.Exit(1);
}

// --DEV //
if(args[0] == "--dev") {
    // Copy compilation output to lethal company //
    Console.WriteLine($":: LOG :: - Running copy from {args[1]}");

    // TODO: better error handling
    var projLoc = args[2];
    var depsLoc = Path.Combine(projLoc, "deps");

    var filesToCopy = Directory.GetFiles(args[1], "*.dll", SearchOption.AllDirectories)
        .Where(x => File.Exists(Path.ChangeExtension(x, ".pdb")));

    // Add dependencies to copy
    if(Path.Exists(depsLoc)) {
        filesToCopy = filesToCopy.Concat(Directory.GetFiles(depsLoc));
    }

    var buildLoc = Directory.CreateDirectory(Path.Combine(projLoc, "build"));

    foreach(var buildFile in buildLoc.GetFiles()) {
        Console.WriteLine($":: LOG :: - Removing old version of {buildFile.FullName}");
        buildFile.Delete();
    }
    
    var outputLocs = new[] {
        Directory.CreateDirectory(PLUGINS_LOCATION),
        buildLoc
    };

    foreach(var fileToCopy in filesToCopy) {
        foreach(var outputLoc in outputLocs) {
            var newfile = Path.Combine(outputLoc.FullName, Path.GetFileName(fileToCopy));

            if(Path.Exists(newfile)) {
                File.Delete(newfile);
            }

            Console.WriteLine($":: LOG :: - Copying to {newfile}");
            File.Copy(fileToCopy, newfile);
        }
    }

    Console.WriteLine($":: LOG :: - Completed copying to plugin location");

    // Build thunderstore output
    var thunderstoreLoc = Path.Combine(projLoc, THUNDERSTORE_FOLDER);
    var thunderstoreFiles = new List<FileInfo>();
    thunderstoreFiles.AddRange(Directory.CreateDirectory(thunderstoreLoc).GetFiles());
    thunderstoreFiles.AddRange(buildLoc.GetFiles());
    
    var zip = Path.Combine(buildLoc.FullName, "thunderstore.zip");
    if(Path.Exists(zip)) {
        File.Delete(zip);
    }
    
    Console.WriteLine($":: LOG :: - Creating zip file for thunderstore release");

    using(var zipfile = ZipFile.Open(zip, ZipArchiveMode.Create)) {
        foreach(var file in thunderstoreFiles) {
            zipfile.CreateEntryFromFile(file.FullName, file.Name);
        }
    }

    // Run lethal company if requested //
    if(args.Length > 3 && args[3] == "--run") {
        new Process {
            StartInfo = new ProcessStartInfo {
                FileName = Path.Combine(LETHAL_LOCATION, "Lethal Company.exe"),
                UseShellExecute = true
            }
        }.Start();
    }
}