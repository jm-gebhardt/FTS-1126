:: AgvDev Simulation Helper.
::
:: Simple script to support for manual testintg WCS and TAR interaction.
:: Dependencies:
:: 1) https://mosquitto.org/
::    Need to be installed in default location set by %MOSQUITTO%.
:: 2) https://docs.microsoft.com/de-de/sql/tools/mssql-cli
::    https://github.com/dbcli/mssql-cli/
::    Depends on https://www.python.org/downloads/
::       When using latest version 3.10.x you need to force update cli_helpers
::       > pip install --upgrade cli_helpers
::
@echo off
title AgvDev Simulation Helper

if not exist ..\..\Source (
    echo Cannot find project folder "..\..\..\AgvDev". 1>&2
    pause
    exit /b 1
)
if not exist ..\..\.env (
    echo Cannot find environment settings "..\..\.env". 1>&2
    pause
    exit /b 1
)

setlocal

set BASEDIR=%CD%
set MQTTHOST=localhost
set MQTTPORT=1883
set MOSQUITTO=c:\Program Files\mosquitto
:: Well known variable to mssql-cli
set MSSQL_CLI_SERVER=(localdb)\MSSQLLocalDB
set MSSQL_CLI_DATABASE=AgvDev
:: Read environment
for /F "tokens=* eol=#" %%i in ('type %BASEDIR%\..\..\.env') do set %%i

if defined SIM_MOSQUITTO (
    set MOSQUITTO=%SIM_MOSQUITTO%
)

:: Try to find mosquitto
"%MOSQUITTO%\mosquitto.exe" --help >nul 2>&1
if %errorlevel% NEQ 3 (
    echo Mosquitto installation not found. 1>&2
    pause
    exit /b 1
)

:: Try to find mssql-clip
cmd /c mssql-cli -v >nul 2>&1
if %errorlevel% NEQ 0 (
    echo mssql-cli installation not found. 1>&2
    pause
    exit /b 1
)


:MENU
echo.
echo AgvDev Simulation Helper
echo.
echo    WCS send (insert into ToAgv)
echo       o - HandleHostOrder
echo       c - CancelHostOrder
echo    TAR send (publish)
echo       1 - State
echo       2 - AgvData
echo       3 - StationList
echo       4 - Graph
echo       5 - OrderEvent
echo    TAR receive (subscribe)
echo       a - RequestAgvData
echo       l - RequestStationList
echo       g - RequestGraph
echo       r - Order
echo    AgvDev
echo       w - WCSCommunication
echo       t - TransportOrder
echo       d - DeviceCommunication
echo    MISC
echo       s - SQL client
echo       q - Quit
echo.
::        1         1         1         1
choice /c 0123456789abcdefghijklmnopqrstuvwxyz /n /m "Waiting for input [o,c, 1,2,3,4, a,l,g, w,t,d, s,q]? "
:: o,c
if %errorlevel% == 25   call :SQLCMD %BASEDIR%\..\..\Doku\examples\HandleHostOrder.sql
if %errorlevel% == 13   call :SQLCMD %BASEDIR%\..\..\Doku\examples\CancelHostOrder.sql
:: 1,2,3,4,5
if %errorlevel% ==  2   call :PUBLISH agv.status, %BASEDIR%\..\..\Doku\examples\State.json
if %errorlevel% ==  3   call :PUBLISH agv.agvData, %BASEDIR%\..\..\Doku\examples\AgvData.json
if %errorlevel% ==  4   call :PUBLISH agv.stationList, %BASEDIR%\..\..\Doku\examples\StationList.json
if %errorlevel% ==  5   call :PUBLISH agv.graph, %BASEDIR%\..\..\Doku\examples\Graph.json
if %errorlevel% ==  6   call :PUBLISH agv.orderEvent, %BASEDIR%\..\..\Doku\examples\OrderEvent.json
:: a,l,g,r
if %errorlevel% == 11   call :SUBSCRIBE agv.requestData
if %errorlevel% == 22   call :SUBSCRIBE agv.requestStationList
if %errorlevel% == 17   call :SUBSCRIBE agv.requestGraph
if %errorlevel% == 28   call :SUBSCRIBE agv.order
:: w,t,d
if %errorlevel% == 33   call :EXECUTE WCSCommunication
if %errorlevel% == 30   call :EXECUTE TransportOrder
if %errorlevel% == 14   call :EXECUTE DeviceCommunication
:: s,q
if %errorlevel% == 29   call :SQLCLIENT
if %errorlevel% == 27   goto ENDE
goto MENU


:ENDE
endlocal
exit /b 0


:SQLCLIENT
echo.
start cmd /c "title SQL & mssql-cli -E"
echo.
exit /b 0

:SQLCMD
echo.
cmd /c mssql-cli -E -i %~1 || pause
if %errorlevel% == 0 (
    echo Done
) else (
    echo Failed 1>&2
)
echo.
exit /b 0

:PUBLISH
echo.
"%MOSQUITTO%\mosquitto_pub.exe" -h %MQTTHOST% -p %MQTTPORT% -u %RABBITMQ_USER% -P %RABBITMQ_PASSWORD% -t %~1 -f %~2
if %errorlevel% == 0 (
    echo Sent
) else (
    echo Failed 1>&2
)
echo.
exit /b 0

:SUBSCRIBE
echo.
start cmd /c "title SUBSCRIBE %~1 - Press Ctrl-C to quit! & mode con: cols=60 lines=40 & "%MOSQUITTO%\mosquitto_sub.exe" -h %MQTTHOST% -p %MQTTPORT% -u %RABBITMQ_USER% -P %RABBITMQ_PASSWORD% -t %~1"
echo.
exit /b 0

:EXECUTE
echo.
start cmd /c "title %~1 & cd %BASEDIR%\..\..\Source\Agv\%~1\bin\Debug\net6.0 & dotnet run --project %BASEDIR%\..\..\Source\Agv\%~1 || pause"
echo.
exit /b 0
