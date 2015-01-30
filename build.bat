@echo off
cls
rd /q/s packages
.paket\paket.bootstrapper
.paket\paket restore
packages\FAKE\tools\Fake