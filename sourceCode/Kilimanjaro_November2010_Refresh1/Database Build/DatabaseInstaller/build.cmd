xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks 2008R2 Analysis Services Project\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks 2008R2 Analysis Services Project\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks 2008R2 Data Warehouse\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks 2008R2 Data Warehouse\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks 2008R2 LT\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks 2008R2 LT\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks 2008R2 OLTP\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks 2008R2 OLTP\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks Analysis Services Project\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks Analysis Services Project\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks Data Warehouse\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks Data Warehouse\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks LT\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks LT\*.*" /S /R /Y
xcopy "E:\ProdSamples\MSFTDBProdSamples\Kilimanjaro_Trunk\AdventureWorks OLTP\*.*" "C:\b\Distribution\KJ-SR1\%1\src\AdventureWorks OLTP\*.*" /S /R /Y
xcopy "DatabaseManifest*.xml" "C:\b\Distribution\KJ-SR1\%1\src\*.*" /R /Y
xcopy "Images\*.*" "C:\b\Distribution\KJ-SR1\%1\src\Images\*.*" /R /Y
xcopy "bin\Debug\DatabaseInstaller.exe" "C:\b\Distribution\KJ-SR1\%1\src" /R /Y
xcopy "bin\Debug\Interop.SHDocVw.dll" "C:\b\Distribution\KJ-SR1\%1\src" /R /Y

attrib -r C:\b\Distribution\KJ-SR1\%1\src\*.* /S

\vsp\zipit\bin\debug\zipit.exe -Create "C:\b\Distribution\KJ-SR1\%1\AdventureWorks2008R2_SR1.zip" "C:\b\Distribution\KJ-SR1\%1\src"
copy /Y MasterZipAboutBox.txt CurrentZipAboutBox.txt
echo Build %1 >>CurrentZipAboutBox.txt
"C:\program files (x86)\Winzip Self-Extractor\wzipse32.exe" C:\b\Distribution\KJ-SR1\%1\AdventureWorks2008R2_SR1 -setup -t ZipHelp.txt -i ArpIcon.ico -a CurrentZipAboutBox.txt -le -auto -st"AdventureWorks 2008R2 SR1" -runasadmin -c .\DatabaseInstaller.exe
