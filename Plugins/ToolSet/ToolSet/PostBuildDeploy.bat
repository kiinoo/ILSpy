
set TargetPath=%~1
set TargetDir=%~2
set TargetFileName=%~3

::goto :EOF
echo DEPLOY to "%TargetDir%..\..\..\..\..\ILSpy\bin\Debug"
copy "%TargetPath%" "%TargetDir%..\..\..\..\..\ILSpy\bin\Debug" && (
  echo DEPLOY SUCCESS
) || (
  echo DEPLOY ERROR: copying file failed & exit 1
)