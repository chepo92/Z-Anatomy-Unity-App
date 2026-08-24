Z-ANATOMY DOCUMENTATION

The root directory contains the Doxygen configuration used to generate the technical documentation for the Z-Anatomy Unity project.

The documentation is generated from the source code and comments contained in the Unity project. The generated HTML documentation should not be edited manually.

REQUIREMENTS

The documentation system requires:

Doxygen
Graphviz

Recommended versions:

Doxygen 1.18.0 or newer
Graphviz with the "dot" executable available in the system PATH

Doxygen uses Graphviz to generate class diagrams, inheritance diagrams, dependency graphs, and other visualizations.

INSTALLATION ON WINDOWS
Doxygen

The easiest way to install Doxygen on Windows is using winget.

Run PowerShell and execute:

winget install DimitriVanHeesch.Doxygen

Verify the installation:

doxygen --version

Expected output will be similar to:

1.18.0
Graphviz

Install Graphviz using winget:

winget search Graphviz

Then install the Graphviz package shown by winget. For example:

winget install Graphviz.Graphviz

After installation, verify that the Graphviz "dot" executable is available:

dot -V

You should see output similar to:

dot - graphviz version ...

You can also verify its location with:

where.exe dot

A typical installation path is:

C:\Program Files\Graphviz\bin\dot.exe

IMPORTANT:

If Doxygen is installed correctly but "dot" is not recognized, Graphviz may be installed but its "bin" directory may not be present in the PATH environment variable.

In that case, add the Graphviz bin directory to the user PATH:

$graphviz = "C:\Program Files\Graphviz\bin"
[Environment]::SetEnvironmentVariable("Path", [Environment]::GetEnvironmentVariable("Path", "User") + ";" + $graphviz, "User")

Close and reopen PowerShell after modifying the PATH.

Then verify again:

dot -V

and:

where.exe dot
INSTALLATION ON OTHER PLATFORMS

Doxygen and Graphviz are available for Linux and macOS as well.

The exact installation method depends on the operating system.

After installation, both commands must be available from the terminal:

doxygen --version
dot -V

The documentation should not depend on a specific local installation path.

PROJECT STRUCTURE

The expected project structure is approximately:

Z-Anatomy-Unity-App/
|
+-- Assets/
|   +-- Scripts/
|   +-- ...
|
+-- Documentation/
|   +-- Doxyfile
|   +-- doxygen/
|       +-- html/
|
+-- Packages/
+-- ProjectSettings/
+-- ...
DOXYGEN CONFIGURATION

The Doxygen configuration is stored in:

Documentation/Doxyfile

The configuration should use relative paths whenever possible.

Do not add machine-specific absolute paths such as:

C:/Users/Username/...
D:/Git/...
/home/username/...

Instead, paths should be relative to the project or documentation directory.

This is important because the documentation must be reproducible on:

Different developer machines
GitHub Actions
Other operating systems
Different local clone locations
GENERATING THE DOCUMENTATION

Open a terminal in the Documentation directory:

cd D:\Git\Z-Anatomy\Z-Anatomy-Unity-App\Documentation

Run Doxygen:

doxygen Doxyfile

Doxygen will process the Unity source code and generate the HTML documentation according to the configuration.

OUTPUT

The generated documentation is expected to be placed under:

Documentation/doxygen/html/

The main entry point is:

Documentation/doxygen/html/index.html

Open index.html in a web browser to inspect the generated documentation locally.

CLEANING THE GENERATED DOCUMENTATION

The generated documentation can be safely deleted because it can be regenerated from the source code.

From the project root:

Remove-Item -Recurse -Force .\Documentation\doxygen

Or, if using a Unix-like shell:

rm -rf Documentation/doxygen

Then regenerate the documentation:

cd Documentation
doxygen Doxyfile
CHECKING THE CONFIGURATION

If Doxygen reports errors, check the following first.

Verify Doxygen:

doxygen --version

Verify Graphviz:

dot -V

Verify that Graphviz can be located:

where.exe dot

Verify that the configured INPUT paths exist.
Verify that the configured output directory can be created.
Check that the Doxyfile does not contain absolute paths referring to another developer's computer.
UPDATING AN OLD DOXYFILE

Doxygen can update an old configuration file using:

doxygen -u Doxyfile

IMPORTANT:

Do not run this command on the original Doxyfile without first creating a backup.

For example:

Copy-Item Doxyfile Doxyfile.backup

Then:

doxygen -u Doxyfile

Review the resulting changes before committing them.

The Z-Anatomy Doxyfile may contain configuration options from older versions of Doxygen. Obsolete options should be reviewed and removed or updated when maintaining the configuration.

TROUBLESHOOTING

"Doxygen is not recognized"

If PowerShell reports:

doxygen : The term 'doxygen' is not recognized...

Doxygen is either not installed or its installation directory is not available in PATH.

Verify the installation with:

winget list Doxygen

Then close and reopen PowerShell or VS Code after installation.

"dot is not recognized"

If PowerShell reports:

dot : The term 'dot' is not recognized...

Graphviz may be installed but its bin directory is not in PATH.

Check:

where.exe dot

If nothing is returned, locate dot.exe and add its directory to PATH.

"Failed to rename ... .dot.png"

Messages such as:

Failed to rename ... inherit_graph_0.dot.png to ... inherit_graph_0.png

usually indicate a problem during the generation or processing of Graphviz images.

Check that:

dot -V

works correctly before investigating the Doxygen configuration.

Also remove the previous generated documentation and run Doxygen again:

Remove-Item -Recurse -Force .\Documentation\doxygen


cd Documentation
doxygen Doxyfile
ABSOLUTE PATHS

The Doxyfile must remain portable.

Never commit paths that reference a developer's local machine.

For example, this should NOT be committed:

INPUT = C:/Users/Lluis/Desktop/Projectes/Unity/Z-Anatomy PC/Z-Anatomy PC/Assets/Scripts

Instead, use a relative path appropriate to the location of the Doxyfile, for example:

INPUT = ../Assets/Scripts

Similarly, output paths should be relative:

OUTPUT_DIRECTORY = doxygen

rather than:

OUTPUT_DIRECTORY = C:/Users/Username/Desktop/Z-Anatomy/Documentation
GITHUB ACTIONS

The long-term goal is to generate the documentation automatically using GitHub Actions.

The intended workflow is:

Z-Anatomy source code
        |
        v
    Doxygen
        |
        v
Documentation/doxygen/html/
        |
        v
   GitHub Actions
        |
        v
Z-Anatomy-Community.github.io
        |
        v
/docs/

This means developers should only need to maintain the source code and Doxyfile.

The generated documentation should be reproducible from a clean checkout using the documented dependencies and commands.

DEVELOPMENT GUIDELINES

When modifying the documentation configuration:

Prefer relative paths.
Do not commit machine-specific paths.
Do not manually edit generated HTML files.
Test Doxygen locally before committing changes.
Verify that Graphviz is available when diagrams are enabled.
Keep the Doxyfile compatible with the version of Doxygen used by the automated documentation workflow.
Regenerate the documentation after significant changes to the source code or Doxyfile.
QUICK START

On a new Windows development machine:

winget install DimitriVanHeesch.Doxygen


winget search Graphviz


winget install Graphviz.Graphviz

Restart PowerShell or VS Code.

Verify:

doxygen --version
dot -V

Go to the Documentation directory:

cd D:\Git\Z-Anatomy\Z-Anatomy-Unity-App\Documentation

Generate the documentation:

doxygen Doxyfile

Open:

Documentation/doxygen/html/index.html

The generated documentation is now available for local inspection.