FROM microsoft/windowsservercore

COPY ShowShutdown/ShowShutdown/bin/Debug/ShowShutdown.exe* c:/
CMD ShowShutdown.exe
