@echo off
setlocal

REM Default subject for testing CA.
set OPENSSL_SUBJ_CA=/CN=gebhardt-foerdertechnik.de

REM Default subject for server certificate
set OPENSSL_SUBJ_SERVER=/C=DE/ST=BW/L=Sinsheim/O=Gebhardt Foerdertechnik GmbH/OU=StoreWare/CN=gebhardt-foerdertechnik.de

REM Default subject for client certificate
set OPENSSL_SUBJ_CLIENT=/O=Client Company/CN=client-domain.de

REM Default number of days for certificate validity
set OPENSSL_CERT_DAYS=365

REM Key size
set OPENSSL_CERT_KEYSIZE=4096

REM Target folder
set OPENSSL_CERTS_DIR=.\Certs

REM Fallback (when neither OpenSSL is found nor AGV_OPENSSL_PATH is set).
set OPENSSL_PATH=C:\Program Files\Git\usr\bin\

REM Temporary folder for building SSL certs.
set TEMP_FOLDER=%TEMP%\agvdev_certs


goto start
:usage
echo Create TLS certificates for RabbitMQ/MQTT transport security.
echo Usage: create_cert.bat
echo The following environment variables are supported:
echo    AGV_SUBJ_SERVER=  Subject for server certificate.
echo                      Using "%OPENSSL_SUBJ_SERVER%" when not set.
echo    AGV_SUBJ_CLIENT=  Subject for client certificate.
echo                      Using "%OPENSSL_SUBJ_CLIENT%" when not set.
echo    AGV_CERT_DAYS=    Number of days for certificate validity.
echo                      Using %OPENSSL_CERT_DAYS% days when not set.
echo    AGV_SUBJ_CA=      Subject for testing certification authority.
echo                      Using "%OPENSSL_SUBJ_CA%" when not set.
echo    AGV_CERTS_DIR=    Target folder for certificate generation.
echo                      Using "%OPENSSL_CERTS_DIR%" when not set.
echo    AGV_OPENSSL_PATH= OpenSSL installation folder.
echo                      When not set try to find OpenSSL from Git for Windows.
echo.
exit /b 1


:start

if not [%~1] == [] goto usage

if defined AGV_SUBJ_SERVER (
	set OPENSSL_SUBJ_SERVER=%AGV_SUBJ_SERVER%
)

if defined AGV_SUBJ_CLIENT (
	set OPENSSL_SUBJ_CLIENT=%AGV_SUBJ_CLIENT%
)

if defined AGV_CERT_DAYS (
	set OPENSSL_CERT_DAYS=%AGV_CERT_DAYS%
)

if defined AGV_SUBJ_CA (
	set OPENSSL_SUBJ_CA=%AGV_SUBJ_CA%
)

if defined AGV_CERTS_DIR (
	set OPENSSL_CERTS_DIR=%AGV_CERTS_DIR%
)

if defined AGV_OPENSSL_PATH (
	REM Use settings from environment.
	set OPENSSL_PATH=%AGV_OPENSSL_PATH%\
) else (
	REM Try find OpenSSL by PATH environment.
	openssl.exe version >nul 2>&1
	if not errorlevel 1 (
		set OPENSSL_PATH=
	)
)

REM Try to find OpenSSL (maybe using default),
"%OPENSSL_PATH%openssl.exe" version >nul 2>&1
if errorlevel 1 (
	echo OpenSSL not found. 1>&2
	goto usage
)



REM Cleanup leftover from previous run ...
if exist "%TEMP_FOLDER%" (
	rd /s /q "%TEMP_FOLDER%"
	echo Failed to cleanup folder %TEMP_FOLDER%. 1>&2
	if errorlevel 1 (
		echo Failed to cleanup folder %TEMP_FOLDER%. 1>&2
		exit /b 1
	)
)
REM ... and prepare temporary folders for certificate creation.
mkdir "%TEMP_FOLDER%" && ^
mkdir "%TEMP_FOLDER%\server" && ^
mkdir "%TEMP_FOLDER%\client" && ^
mkdir "%TEMP_FOLDER%\ca" && ^
mkdir "%TEMP_FOLDER%\ca\certs" && ^
mkdir "%TEMP_FOLDER%\ca\private" && ^
echo 01>"%TEMP_FOLDER%\ca\serial"&&^
echo. 2>"%TEMP_FOLDER%\ca\index.txt" >nul
if errorlevel 1 (
	echo Failed to create folders in %TEMP_FOLDER%. 1>&2
	exit /b 1
)

REM Prepare replacements for configuration.
set "OPENSSL_CA_FOLDER=%TEMP_FOLDER:\=/%/ca"

REM Generate ".\openssl.cnf" from template.
goto build_openssl_cnf
:eval_openssl_cnf
echo %~1 >> "%TEMP_FOLDER%\ca\openssl.cnf"
goto:eof
:build_openssl_cnf
echo. 2>"%TEMP_FOLDER%\ca\openssl.cnf" >nul
for /f "delims=" %%i in (.\Config\openssl.cnf.template) do call:eval_openssl_cnf "%%i"
if not exist "%TEMP_FOLDER%\ca\openssl.cnf" (
	echo Failed to build OpenSSL configuration. 1>&2
	exit /b 1
)

REM Prepare replacement for configuration (convert to absolute path)
pushd %OPENSSL_CERTS_DIR%
set OPENSSL_CERTS_SERVER_DIR=%CD%\server
popd
set "OPENSSL_CERTS_SERVER_DIR=%OPENSSL_CERTS_SERVER_DIR:\=/%"

REM Generate "rabbitmq.conf" from template.
del .\Config\rabbitmq\rabbitmq.conf
goto build_rabbitmq_cnf
:eval_rabbitmq_cnf
echo %~1 >> .\Config\rabbitmq\rabbitmq.conf
goto:eof
:build_rabbitmq_cnf
echo # Generated from rabbitmq.conf.template by create_cert.bat > .\Config\rabbitmq\rabbitmq.docker.conf
for /f "delims=" %%i in (.\Config\rabbitmq\rabbitmq.conf.template) do call:eval_rabbitmq_cnf "%%i"
if not exist .\Config\rabbitmq\rabbitmq.conf (
	echo Failed to build RabbitMQ configuration. 1>&2
	exit /b 1
)

REM Prepare replacement for configuration (unix path within RabbitMQ container)
set OPENSSL_CERTS_SERVER_DIR=/etc/rabbitmq/certs

REM Generate "rabbitmq.docker.conf" from template.
del .\Config\rabbitmq\rabbitmq.docker.conf
goto build_rabbitmq_dockercnf
:eval_rabbitmq_dockercnf
echo %~1 >> .\Config\rabbitmq\rabbitmq.docker.conf
goto:eof
:build_rabbitmq_dockercnf
echo # Generated from rabbitmq.conf.template by create_cert.bat > .\Config\rabbitmq\rabbitmq.docker.conf
for /f "delims=" %%i in (.\Config\rabbitmq\rabbitmq.conf.template) do call:eval_rabbitmq_dockercnf "%%i"
if not exist .\Config\rabbitmq\rabbitmq.docker.conf (
	echo Failed to build RabbitMQ configuration for docker. 1>&2
	exit /b 1
)


set ERRCNT=0


echo.
echo.
echo Creating Test CA =========================================================
"%OPENSSL_PATH%openssl.exe" req -x509 -config "%TEMP_FOLDER%\ca\openssl.cnf" -newkey rsa:%OPENSSL_CERT_KEYSIZE% -days %OPENSSL_CERT_DAYS% -out "%TEMP_FOLDER%\ca\ca_certificate.pem" -outform PEM -subj "%OPENSSL_SUBJ_CA%" -nodes >nul
set /a ERRCNT+=%errorlevel%
"%OPENSSL_PATH%openssl.exe" x509 -in "%TEMP_FOLDER%\ca\ca_certificate.pem" -out "%TEMP_FOLDER%\ca\ca_certificate.cer" -outform DER >nul
set /a ERRCNT+=%errorlevel%


echo.
echo.
echo Creating Server Certificate ==============================================
"%OPENSSL_PATH%openssl.exe" genrsa -out "%TEMP_FOLDER%\server\private_key.pem" %OPENSSL_CERT_KEYSIZE% >nul
set /a ERRCNT+=%errorlevel%
"%OPENSSL_PATH%openssl.exe" req -new -key "%TEMP_FOLDER%\server\private_key.pem" -out "%TEMP_FOLDER%\server\req.pem" -outform PEM -subj "%OPENSSL_SUBJ_SERVER%" -nodes >nul
set /a ERRCNT+=%errorlevel%
"%OPENSSL_PATH%openssl.exe" ca -config "%TEMP_FOLDER%\ca\openssl.cnf" -in "%TEMP_FOLDER%\server\req.pem" -out "%TEMP_FOLDER%\server\server_certificate.pem" -notext -batch -extensions server_ca_extensions >nul
set /a ERRCNT+=%errorlevel%


echo.
echo.
echo Creating Client Certificate ==============================================
"%OPENSSL_PATH%openssl.exe" genrsa -out "%TEMP_FOLDER%\client\private_key.pem" %OPENSSL_CERT_KEYSIZE% >nul
set /a ERRCNT+=%errorlevel%
"%OPENSSL_PATH%openssl.exe" req -new -key "%TEMP_FOLDER%\client\private_key.pem" -out "%TEMP_FOLDER%\client\req.pem" -outform PEM -subj "%OPENSSL_SUBJ_CLIENT%" -nodes >nul
set /a ERRCNT+=%errorlevel%
"%OPENSSL_PATH%openssl.exe" ca -config "%TEMP_FOLDER%\ca\openssl.cnf" -in "%TEMP_FOLDER%\client\req.pem" -out "%TEMP_FOLDER%\client\client_certificate.pem" -notext -batch -extensions client_ca_extensions >nul
set /a ERRCNT+=%errorlevel%


if %ERRCNT% EQU 0 (
	echo.
	echo.
	echo Copy certificates to target folder ==========================================

	if not exist "%OPENSSL_CERTS_DIR%" (
		mkdir "%OPENSSL_CERTS_DIR%"
		set /a ERRCNT+=%errorlevel%
	)
	if %ERRCNT% EQU 0 (
		if not exist "%OPENSSL_CERTS_DIR%\server" (
			mkdir "%OPENSSL_CERTS_DIR%\server"
			set /a ERRCNT+=%errorlevel%
		)
	)
	if %ERRCNT% EQU 0 (
		if not exist "%OPENSSL_CERTS_DIR%\client" (
			mkdir "%OPENSSL_CERTS_DIR%\client"
			set /a ERRCNT+=%errorlevel%
		)
	)

	if %ERRCNT% EQU 0 (
		copy /y "%TEMP_FOLDER%\ca\ca_certificate.pem"         "%OPENSSL_CERTS_DIR%\server\ca_certificate.pem" >nul
		set /a ERRCNT+=%errorlevel%
		copy /y "%TEMP_FOLDER%\server\server_certificate.pem" "%OPENSSL_CERTS_DIR%\server\server_certificate.pem" >nul
		set /a ERRCNT+=%errorlevel%
		copy /y "%TEMP_FOLDER%\server\private_key.pem"        "%OPENSSL_CERTS_DIR%\server\private_key.pem" >nul
		set /a ERRCNT+=%errorlevel%
		copy /y "%TEMP_FOLDER%\ca\ca_certificate.pem"         "%OPENSSL_CERTS_DIR%\client\ca_certificate.pem" >nul
		set /a ERRCNT+=%errorlevel%
		copy /y "%TEMP_FOLDER%\client\client_certificate.pem" "%OPENSSL_CERTS_DIR%\client\client_certificate.pem" >nul
		set /a ERRCNT+=%errorlevel%
		copy /y "%TEMP_FOLDER%\client\private_key.pem"        "%OPENSSL_CERTS_DIR%\client\private_key.pem" >nul
		set /a ERRCNT+=%errorlevel%
	)
	if %ERRCNT% NEQ 0 (
		rd /s /q "%OPENSSL_CERTS_DIR%\server"
		rd /s /q "%OPENSSL_CERTS_DIR%\client"
	)
) else (
	echo Failed to install certificates. 1>&2
)


if %ERRCNT% EQU 0 (
	echo.
	echo.
	echo RabbitMQ configuration ======================================================
	echo A configuration template has been generated at .\Config\rabbitmq\rabbitmq.conf
	echo CN of the following DN is used for authorization:
	"%OPENSSL_PATH%openssl.exe" x509 -in "%OPENSSL_CERTS_DIR%\client\client_certificate.pem" -nameopt RFC2253 -subject -noout
	echo For more information see https://dev.azure.com/GebhardtGroup/StoreWare/_wiki/wikis/StoreWare.wiki/167/Transportsicherheit-(TLS-Transport-Layer-Security)
)


REM Clean up temp files (on success).
if %ERRCNT% EQU 0 (
	rd /s /q "%TEMP_FOLDER%"
)

echo.
exit /b %ERRCNT%
