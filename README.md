deployment-utilities
====================

Config.Transform 	
Written by Jeff Dunlop   

Within the context of deploying a ClickOnce WPF application, I found that I needed to update the .application file with a custom deploy point, and app.config with a custom service endpoint. The .config.deploy file is published to a versioned folder which makes it difficult to find, and I'm not aware of any utility that can apply xml transforms as easily as msdeploy makes it for web applications. So I wrote one that captures as much of msbuild's behaviour as needed to solve this one problem, and named it Config.Transform.

Here's a dump of the command line options it supports:

Config File Transformation Command Line Utility

Config.Transform [args ...]

-setParam:                    Sets a parameter.

-recursive                    Apply parameters recursively.

-verbose                      Enables more verbose output.

-workingDirectory             Select the working directory.

-?                            Displays options.

Config.Transform expects to find Parameters.xml in the working directory (current directory default), and it follows the conventions of msbuild in applying config changes, so I'll mostly just document the differences.

-workingDirectory is the point where processing starts. Relative paths are supported.

-recursive applies the config changes to all files found starting in the working directory, recursively through all directories.

-verbose is pretty verbose :)

Only kind="XmlFile" in parameterEntry is supported.

You will find online examples of the scope attribute that show web.config$ with and without a leading backslash -- I have no idea  what the distinction is. With Config.Transform, a leading backslash anchors your recursive search to the working directory.

You can download from my repository at https://github.com/jtdunlop/deployment-utilities
