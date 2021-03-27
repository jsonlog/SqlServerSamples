xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks 2008 Analysis Services Project\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks 2008 Analysis Services Project\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks 2008 Data Warehouse\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks 2008 Data Warehouse\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks 2008 LT\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks 2008 LT\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks 2008 OLTP\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks 2008 OLTP\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks Analysis Services Project\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks Analysis Services Project\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks Data Warehouse\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks Data Warehouse\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks LT\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks LT\*.*" /S /R /Y
xcopy "C:\ProdSamples\MSFTDBProdSamples\Katmai_Trunk\AdventureWorks OLTP\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\AdventureWorks OLTP\*.*" /S /R /Y
xcopy "DatabaseManifest*.xml" "C:\b\Distribution\Katmai-SR4\%1\src\*.*" /R /Y
xcopy "Images\*.*" "C:\b\Distribution\Katmai-SR4\%1\src\Images\*.*" /R /Y
xcopy "bin\Debug\DatabaseInstaller.exe" "C:\b\Distribution\Katmai-SR4\%1\src" /R /Y
xcopy "bin\Debug\Interop.SHDocVw.dll" "C:\b\Distribution\Katmai-SR4\%1\src" /R /Y

attrib -r C:\b\Distribution\Katmai-SR4\%1\src\*.* /S

\vsp\zipit\bin\debug\zipit.exe -Create "C:\b\Distribution\Katmai-SR4\%1\AdventureWorks2008_SR4.zip" "C:\b\Distribution\Katmai-SR4\%1\src"
copy /Y MasterZipAboutBox.txt CurrentZipAboutBox.txt
echo Build %1 >>CurrentZipAboutBox.txt
"c:\program files (x86)\Winzip Self-Extractor\wzipse32.exe" C:\b\Distribution\Katmai-SR4\%1\AdventureWorks2008_SR4 -setup -t ZipHelp.txt -i ArpIcon.ico -a CurrentZipAboutBox.txt -le -auto -st"AdventureWorks 2008 SR4" -runasadmin -c .\DatabaseInstaller.exe
