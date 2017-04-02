#!/bin/sh
mono ./bin/Release/HexMage.Benchmarks.exe --GnuPlot=false | tee /local/darthdeus/`date +%s`
