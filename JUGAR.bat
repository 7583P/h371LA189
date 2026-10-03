@echo off
cd /d "%~dp0Builds\Phase2Closure"
start "" "%~dp0Builds\Phase2Closure\HillAdventurerValidation.exe" -force-d3d11 -screen-width 1600 -screen-height 900 -screen-fullscreen 0
