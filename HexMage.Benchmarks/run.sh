#!/bin/sh
mono ./bin/Release/HexMage.Benchmarks.exe --EnableGnuPlot=false | tee /local/darthdeus/`date +%s`
