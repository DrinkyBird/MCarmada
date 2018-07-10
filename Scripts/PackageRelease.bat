SET OUTDIR=%CD%\output

if NOT EXIST %OUTDIR%\ (
	mkdir %OUTDIR%
)

pushd MCarmada\bin\Release

copy /Y *.dll %OUTDIR%\
copy /Y *.exe %OUTDIR%\
xcopy /I /E /Y %CD%\Settings %OUTDIR%\Settings

if NOT EXIST %OUTDIR%\Plugins (
	mkdir %OUTDIR%\Plugins
)

popd