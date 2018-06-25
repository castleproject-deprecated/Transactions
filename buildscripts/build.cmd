@ECHO OFF
REM ****************************************************************************
REM Copyright 2004-2013 Castle Project - http://www.castleproject.org/
REM Licensed under the Apache License, Version 2.0 (the "License");
REM you may not use this file except in compliance with the License.
REM You may obtain a copy of the License at
REM 
REM     http://www.apache.org/licenses/LICENSE-2.0
REM 
REM Unless required by applicable law or agreed to in writing, software
REM distributed under the License is distributed on an "AS IS" BASIS,
REM WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
REM See the License for the specific language governing permissions and
REM limitations under the License.
REM ****************************************************************************

if "%1" == "" goto no_config 
if "%1" NEQ "" goto set_config 

:set_config
SET Configuration=%1
GOTO restore_packages

:no_config
SET Configuration=Release
GOTO restore_packages

:restore_packages
dotnet restore Castle.Transactions.sln

GOTO build

:build
dotnet build Castle.Transactions.sln -c %Configuration%
GOTO test

:test

echo -------------
echo Running Tests
echo -------------

dotnet test src\Castle.Facilities.AutoTx.Tests || exit /b 1
dotnet test src\Castle.IO.Tests || exit /b 1
dotnet test src\Castle.Transactions.IO.Tests || exit /b 1
dotnet test src\Castle.Transactions.Tests || exit /b 1
