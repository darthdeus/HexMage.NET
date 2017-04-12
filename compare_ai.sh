#!/bin/bash

echo "Iterations: $1, BenchType: $2, name: $3"

mono ./HexMage.Benchmarks/bin/Release/HexMage.Benchmarks.exe --MctsBenchIterations=$1 --MctsBenchType=$2 > /local/darthdeus/iter/iter-$3-$1
