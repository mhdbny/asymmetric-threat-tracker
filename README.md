# Operating System Compatibility
* All instructions below apply to Windows XP and 7.
* Windows 8 should also work but hasn't been tested.
* Running on Linux via Mono should work (must compile from source -- see below). 

# Prerequisites
* Install [​PostgreSQL](http://www.enterprisedb.com/products-services-training/pgdownload) 9.2 or later. After installing PostgreSQL, use Stack Builder to install PostGIS 2.0 or later. PostGIS is located under the Spatial Extensions tab. Keep note of the file paths to "shp2pgsql.exe" and "pgsql2shp.exe".
* Install [​R](http://www.r-project.org) 2.15.0 or later. Make sure the directory to which you install R packages is writable. Keep note of the file path to "R.exe".
* Install ​[Java](https://www.java.com/en/download) 1.6 or later. Keep note of the file path to "java.exe". 
* Install the most current version of ​[LibLinear](http://www.csie.ntu.edu.tw/~cjlin/liblinear). Keep note of the file paths to "train.exe" and predict.exe".
* Install the most current version of [​SvmRank](http://www.cs.cornell.edu/people/tj/svm_light/svm_rank.html). Keep note of the file paths to "svm_rank_learn.exe" and "svm_rank_classify.exe".

# Installation
There are two installation choices: binary installer and compilation from source. Unless you are interested in modifying the ATT or understanding the nitty gritty of how it works, you will probably want to use the binary installer.

## Binary Installer
Download and run the installer for the version you would like:
* [Beta](https://github.com/MatthewGerber/asymmetric-threat-tracker/raw/master/Installer/Installer/Express/SingleImage/DiskImages/DISK1/setup.exe):  this is the most recent version. It is usually well tested, but might contain bugs.

After you run the installer, edit the configuration files in the Config sub-directory of the installation directory. Use values appropriate for your machine. Then run the ATT. If everything is installed/configured correctly, the system will start.

## Compilation from Source
To compile from source:
* Install a working version of Microsoft Visual Studio that is capable of running C# applications. ​[Visual Studio Express 2013](http://www.visualstudio.com/en-US/products/visual-studio-express-vs) is sufficient and free.
* Optional:  Go to Tools -> Extensions and Updates, then search for and install the License Header Manager. This is used to apply license text to each source code file. Also install the [InstallShield Limited Edition for Visual Studio](http://learn.flexerasoftware.com/content/IS-EVAL-InstallShield-Limited-Edition-Visual-Studio) add-on if you want to build the installer package.
* Clone the [ATT GitHub repository](https://github.com/MatthewGerber/asymmetric-threat-tracker).
* Open the ATT solution in Visual Studio by double-clicking the "ATT.sln" file. 
* Along the top menu in Visual Studio, change the configuration of the solution from "Debug" to "Release". 
* Right-click the ATT project and select "Build".
* Copy "att_config.xml" from the ATT project into the Config folder within the GUI project. After copying, select the copied file and, within the properties window, set "Copy to Output Directory" to "Copy Always".
* Copy "gui_config.xml" from the GUI project into the Config folder within the GUI project. After copying, select the copied file and, within the properties window, set "Copy to Output Directory" to "Copy Always".
* Edit the "att_config.xml" and "gui_config.xml" files in Config appropriately for your machine. Consult the documentation within the config files for detailed instructions.
* Right-click the GUI project and select "Build".
* Right-click the GUI project and select "Set as Start Up Project".
* Run the ATT solution. If everything goes okay, it will initialize all of the database tables and present the GUI interface.
