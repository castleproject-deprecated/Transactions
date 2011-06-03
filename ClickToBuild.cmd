@ECHO OFF
echo Checking ruby version
ruby --version

IF %ERRORLEVEL% NEQ 0 GOTO install

:build
echo Performing build
rake
goto done

:done
exit /B 0

:install
tools\wget "http://rubyforge.org/frs/download.php/74298/rubyinstaller-1.9.2-p180.exe"
echo "installing ruby!"
rubyinstaller-1.9.2-p180.exe
SET ERRORLEVEL=0
GOTO build