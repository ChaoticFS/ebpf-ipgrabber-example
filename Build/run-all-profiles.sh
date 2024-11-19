#!/bin/bash
dotnet run --launch-profile "http" &
dotnet run --launch-profile "http1" &
dotnet run --launch-profile "http2" &
dotnet run --launch-profile "http3" &
wait
